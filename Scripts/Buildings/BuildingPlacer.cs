using Godot;
using System.Collections.Generic;

public partial class BuildingPlacer : Node2D
{
	[Signal]
	public delegate void BuildStateChangedEventHandler(
		bool isBuildModeActive,
		string selectedBuildingName,
		string selectedBuildingCost,
		string statusText);

	private const string ToggleBuildModeAction = "ToggleBuildMode";
	private const string SelectBuilding1Action = "SelectBuilding1";
	private const string SelectBuilding2Action = "SelectBuilding2";
	private const string SelectBuilding3Action = "SelectBuilding3";
	private const string SelectBuilding4Action = "SelectBuilding4";
	private const string SelectBuilding5Action = "SelectBuilding5";
	private const string PlaceBuildingAction = "PlaceBuilding";
	private const string CancelBuildModeAction = "CancelBuildMode";
	private const string SellBuildingAction = "SellBuilding";

	private readonly Dictionary<Vector2I, Building> placedBuildings = new();
	private readonly Dictionary<Vector2I, string> blockedCellReasons = new();
	private readonly Dictionary<BuildingType, PackedScene> buildingScenes = new();

	private ResourceManager resourceManager;
	private UpgradeManager upgradeManager;
	private RunManager runManager;
	private SelectionController selectionController;
	private Node2D buildingsRoot;
	private Polygon2D preview;
	private BuildingType selectedBuildingType = BuildingType.Drill;
	private bool isBuildModeActive;
	private string statusText = string.Empty;
	private string placementTooltipText = string.Empty;

	[Export]
	private int gridSize = 64;

	public bool IsBuildModeActive => isBuildModeActive;
	public BuildingType SelectedBuildingType => selectedBuildingType;
	public string SelectedBuildingName => BuildingDefinitions.Get(selectedBuildingType).DisplayName;
	public string SelectedBuildingCost => GetSelectedBuildingCostText();
	public string SelectedBuildingPurpose => BuildingDefinitions.Get(selectedBuildingType).Purpose;
	public string StatusText => statusText;
	public string BuildMenuText => GetBuildMenuText();
	public string PlacementTooltipText => placementTooltipText;

	public override void _Ready()
	{
		EnsureBuildInputActions();
		resourceManager = GetNodeOrNull<ResourceManager>("/root/ResourceManager");
		if (resourceManager != null)
		{
			resourceManager.ResourcesChanged += OnResourcesChanged;
		}

		upgradeManager = GetNodeOrNull<UpgradeManager>("/root/UpgradeManager");
		runManager = GetNodeOrNull<RunManager>("/root/RunManager");
		if (upgradeManager != null)
		{
			upgradeManager.UpgradeApplied += OnUpgradeApplied;
		}

		selectionController = GetNodeOrNull<SelectionController>("../SelectionController");
		buildingsRoot = GetNodeOrNull<Node2D>("../TestWorld/Buildings");
		CacheBuildingScenes();
		CacheBlockedCells();
		CreatePreview();
		UpdatePreview();
		EmitBuildState();
	}

	public override void _Process(double delta)
	{
		HandleBuildInput();
		UpdatePreview();
	}

	public override void _ExitTree()
	{
		if (resourceManager != null)
		{
			resourceManager.ResourcesChanged -= OnResourcesChanged;
		}

		if (upgradeManager != null)
		{
			upgradeManager.UpgradeApplied -= OnUpgradeApplied;
		}
	}

	private void HandleBuildInput()
	{
		if (Input.IsActionJustPressed(ToggleBuildModeAction))
		{
			SetBuildModeActive(!isBuildModeActive);
		}

		if (Input.IsActionJustPressed(CancelBuildModeAction))
		{
			SetBuildModeActive(false);
		}

		if (Input.IsActionJustPressed(SelectBuilding1Action))
		{
			SelectBuilding(BuildingType.Drill);
		}
		else if (Input.IsActionJustPressed(SelectBuilding2Action))
		{
			SelectBuilding(BuildingType.Generator);
		}
		else if (Input.IsActionJustPressed(SelectBuilding3Action))
		{
			SelectBuilding(BuildingType.Assembler);
		}
		else if (Input.IsActionJustPressed(SelectBuilding4Action))
		{
			SelectBuilding(BuildingType.Turret);
		}
		else if (Input.IsActionJustPressed(SelectBuilding5Action))
		{
			SelectBuilding(BuildingType.Storage);
		}

		if (isBuildModeActive && Input.IsActionJustPressed(PlaceBuildingAction))
		{
			TryPlaceSelectedBuilding();
		}

		if (!isBuildModeActive && Input.IsActionJustPressed(SellBuildingAction))
		{
			TrySellSelectedBuilding();
		}
	}

	private void SelectBuilding(BuildingType buildingType)
	{
		selectedBuildingType = buildingType;
		SetBuildModeActive(true);
		SetStatus(string.Empty);
	}

	private void TryPlaceSelectedBuilding()
	{
		Vector2I targetCell = GetMouseCell();
		if (!CanPlaceAt(targetCell, out string placementError))
		{
			FeedbackEffects.PlaySfx(this, "error");
			SetStatus(placementError);
			return;
		}

		BuildingDefinition definition = BuildingDefinitions.Get(selectedBuildingType);
		Dictionary<ResourceType, int> cost = GetSelectedBuildingCost();
		if (!buildingScenes.TryGetValue(selectedBuildingType, out PackedScene scene) || scene == null)
		{
			FeedbackEffects.PlaySfx(this, "error");
			SetStatus("Invalid placement");
			return;
		}

		if (resourceManager == null || !resourceManager.Spend(cost))
		{
			FeedbackEffects.PlaySfx(this, "error");
			SetStatus("Not enough resources");
			return;
		}

		Building building = scene.Instantiate<Building>();
		building.InitializePlacement(targetCell, cost);
		building.Destroyed += OnBuildingDestroyed;
		building.GlobalPosition = CellToWorldPosition(targetCell);
		buildingsRoot.AddChild(building);
		placedBuildings[targetCell] = building;
		runManager?.RecordBuildingPlaced();
		FeedbackEffects.PlaySfx(this, "building_placed");
		SetStatus($"{definition.DisplayName} placed");
	}

	private void TrySellSelectedBuilding()
	{
		if (GetTree().Paused ||
		    runManager?.CurrentPhase is not (RunPhase.Day or RunPhase.Night) ||
		    selectionController?.SelectedNode is not Building building ||
		    !IsInstanceValid(building) ||
		    building.IsDestroyed)
		{
			return;
		}

		SellBuilding(building);
	}

	private void SellBuilding(Building building)
	{
		Dictionary<ResourceType, int> refund = building.GetRefundCost();
		Dictionary<ResourceType, int> actualRefund = AddRefund(refund, out bool lostRefund);
		string buildingName = building.DisplayName;
		Vector2 feedbackPosition = building.GlobalPosition;

		building.Destroyed -= OnBuildingDestroyed;
		if (placedBuildings.TryGetValue(building.GridCell, out Building placedBuilding) && placedBuilding == building)
		{
			placedBuildings.Remove(building.GridCell);
		}

		selectionController?.ClearSelection();
		building.Sell();
		runManager?.RecordBuildingSold(actualRefund);
		FeedbackEffects.PlaySfx(this, lostRefund ? "error" : "building_sold");

		string refundText = actualRefund.Count > 0
			? $"+{BuildingDefinitions.FormatCost(actualRefund)}"
			: "refund storage full";
		FeedbackEffects.SpawnText(
			this,
			feedbackPosition,
			$"Sold {buildingName}: {refundText}",
			FeedbackEffects.ScrapGainColor,
			FeedbackCategory.Status,
			0.1f,
			$"{GetInstanceId()}:sold");

		if (lostRefund)
		{
			FeedbackEffects.SpawnText(
				this,
				feedbackPosition,
				"Refund partly lost: storage full",
				FeedbackEffects.WarningColor,
				FeedbackCategory.Error,
				0.1f,
				$"{GetInstanceId()}:refund-lost",
				new Vector2(0f, -18f));
		}

		SetStatus(lostRefund ? "Refund partly lost: storage full" : $"{buildingName} sold");
		UpdatePreview();
	}

	private bool CanPlaceAt(Vector2I cell, out string placementError)
	{
		if (buildingsRoot == null)
		{
			placementError = "Invalid location";
			return false;
		}

		if (placedBuildings.ContainsKey(cell))
		{
			placementError = "Cell occupied";
			return false;
		}

		if (blockedCellReasons.TryGetValue(cell, out string blockedReason))
		{
			placementError = blockedReason;
			return false;
		}

		if (TryGetBlockingDeposit(cell, out string depositBlockReason))
		{
			placementError = depositBlockReason;
			return false;
		}

		if (resourceManager == null || !resourceManager.CanSpend(GetSelectedBuildingCost()))
		{
			placementError = "Not enough resources";
			return false;
		}

		placementError = string.Empty;
		return true;
	}

	private void SetBuildModeActive(bool active)
	{
		if (isBuildModeActive == active)
		{
			return;
		}

		isBuildModeActive = active;
		if (!isBuildModeActive)
		{
			SetStatus(string.Empty);
		}

		UpdatePreview();
		EmitBuildState();
	}

	private void SetStatus(string message)
	{
		statusText = message;
		EmitBuildState();
	}

	private void EmitBuildState()
	{
		EmitSignal(
			SignalName.BuildStateChanged,
			isBuildModeActive,
			SelectedBuildingName,
			SelectedBuildingCost,
			statusText);
	}

	private Dictionary<ResourceType, int> GetSelectedBuildingCost()
	{
		return GetBuildingCost(selectedBuildingType);
	}

	public bool CanAfford(BuildingType buildingType)
	{
		return resourceManager != null && resourceManager.CanSpend(GetBuildingCost(buildingType));
	}

	public string GetBuildingCostText(BuildingType buildingType)
	{
		return BuildingDefinitions.FormatCost(GetBuildingCost(buildingType));
	}

	public string GetBuildingCompactCostText(BuildingType buildingType)
	{
		return BuildingDefinitions.FormatCompactCost(GetBuildingCost(buildingType));
	}

	private string GetSelectedBuildingCostText()
	{
		Dictionary<ResourceType, int> baseCost = BuildingDefinitions.Get(selectedBuildingType).Cost;
		return upgradeManager?.FormatDiscountedCost(baseCost) ?? BuildingDefinitions.FormatCost(baseCost);
	}

	private void OnUpgradeApplied(int upgradeType)
	{
		if ((UpgradeType)upgradeType is UpgradeType.BuildingCostDiscount or UpgradeType.BuildingCostDiscountSmall)
		{
			EmitBuildState();
			UpdatePreview();
		}
	}

	private void OnResourcesChanged()
	{
		EmitBuildState();
		UpdatePreview();
	}

	private void OnBuildingDestroyed(Building building)
	{
		if (building == null)
		{
			return;
		}

		building.Destroyed -= OnBuildingDestroyed;
		if (placedBuildings.TryGetValue(building.GridCell, out Building placedBuilding) && placedBuilding == building)
		{
			placedBuildings.Remove(building.GridCell);
		}

		GetNodeOrNull<RunManager>("/root/RunManager")?.RecordBuildingDestroyed();
		UpdatePreview();
	}

	private void UpdatePreview()
	{
		if (preview == null)
		{
			return;
		}

		preview.Visible = isBuildModeActive;
		if (!isBuildModeActive)
		{
			SetPlacementTooltip(string.Empty);
			return;
		}

		Vector2I targetCell = GetMouseCell();
		preview.GlobalPosition = CellToWorldPosition(targetCell);
		bool isValid = CanPlaceAt(targetCell, out string placementError);
		preview.Color = isValid
			? new Color(0.25f, 0.95f, 0.45f, 0.45f)
			: new Color(0.95f, 0.2f, 0.18f, 0.45f);
		SetPlacementTooltip(BuildPlacementTooltip(targetCell, isValid, placementError));
	}

	private void CreatePreview()
	{
		preview = new Polygon2D
		{
			Name = "BuildingPreview",
			ZIndex = 100,
			Visible = false,
			Polygon = new Vector2[]
			{
				new(-24, -24),
				new(24, -24),
				new(24, 24),
				new(-24, 24)
			}
		};

		AddChild(preview);
	}

	private void CacheBuildingScenes()
	{
		foreach (BuildingType buildingType in System.Enum.GetValues<BuildingType>())
		{
			buildingScenes[buildingType] = BuildingDefinitions.LoadScene(buildingType);
		}
	}

	private void CacheBlockedCells()
	{
		Node testWorld = GetNodeOrNull("../TestWorld");
		if (testWorld == null)
		{
			return;
		}

		foreach (Node child in testWorld.GetChildren())
		{
			if (child is Core core)
			{
				blockedCellReasons[WorldPositionToCell(core.GlobalPosition)] = "Blocked by Core";
			}
		}
	}

	private string GetBuildMenuText()
	{
		List<string> lines = new()
		{
			"Build Menu"
		};

		foreach (BuildingType buildingType in System.Enum.GetValues<BuildingType>())
		{
			BuildingDefinition definition = BuildingDefinitions.Get(buildingType);
			Dictionary<ResourceType, int> cost = GetBuildingCost(buildingType);
			string affordableText = resourceManager != null && resourceManager.CanSpend(cost) ? "CAN" : "NEED";
			int hotkey = (int)buildingType + 1;
			lines.Add($"{hotkey} {definition.DisplayName} | {BuildingDefinitions.FormatCost(cost)} | {definition.Purpose} [{affordableText}]");
		}

		return string.Join("\n", lines);
	}

	private string BuildPlacementTooltip(Vector2I targetCell, bool isValid, string placementError)
	{
		BuildingDefinition definition = BuildingDefinitions.Get(selectedBuildingType);
		List<string> lines = new()
		{
			$"{definition.DisplayName}",
			$"Cost: {GetSelectedBuildingCostText()}",
			definition.Purpose
		};

		lines.Add(isValid ? "Placement: Valid" : $"Placement: Invalid - {placementError}");

		string warning = GetPlacementWarning(targetCell);
		if (!string.IsNullOrEmpty(warning))
		{
			lines.Add($"Warning: {warning}");
		}

		return string.Join("\n", lines);
	}

	private string GetPlacementWarning(Vector2I targetCell)
	{
		if (selectedBuildingType == BuildingType.Drill && !IsCellNearScrapDeposit(targetCell))
		{
			return "Needs nearby Scrap Deposit";
		}

		return string.Empty;
	}

	private bool IsCellNearScrapDeposit(Vector2I cell)
	{
		const float depositWorkRange = 96f;
		float rangeSquared = depositWorkRange * depositWorkRange;
		Vector2 worldPosition = CellToWorldPosition(cell);
		foreach (Node node in GetTree().GetNodesInGroup("ScrapDeposits"))
		{
			if (node is ScrapDeposit deposit &&
			    IsInstanceValid(deposit) &&
			    !deposit.IsEmpty &&
			    worldPosition.DistanceSquaredTo(deposit.GlobalPosition) <= rangeSquared)
			{
				return true;
			}
		}

		return false;
	}

	private void SetPlacementTooltip(string tooltipText)
	{
		if (placementTooltipText == tooltipText)
		{
			return;
		}

		placementTooltipText = tooltipText;
		EmitBuildState();
	}

	private Dictionary<ResourceType, int> GetBuildingCost(BuildingType buildingType)
	{
		Dictionary<ResourceType, int> baseCost = BuildingDefinitions.Get(buildingType).Cost;
		return upgradeManager?.GetDiscountedCost(baseCost) ?? new Dictionary<ResourceType, int>(baseCost);
	}

	private Dictionary<ResourceType, int> AddRefund(Dictionary<ResourceType, int> refund, out bool lostRefund)
	{
		Dictionary<ResourceType, int> actualRefund = new();
		lostRefund = false;
		if (resourceManager == null || refund == null)
		{
			return actualRefund;
		}

		foreach (KeyValuePair<ResourceType, int> entry in refund)
		{
			int added = resourceManager.AddResource(entry.Key, entry.Value);
			if (added > 0)
			{
				actualRefund[entry.Key] = added;
			}

			if (added < entry.Value)
			{
				lostRefund = true;
			}
		}

		return actualRefund;
	}

	private bool TryGetBlockingDeposit(Vector2I cell, out string blockReason)
	{
		foreach (Node node in GetTree().GetNodesInGroup("ScrapDeposits"))
		{
			if (node is not ScrapDeposit deposit ||
			    !IsInstanceValid(deposit) ||
			    deposit.IsEmpty)
			{
				continue;
			}

			if (WorldPositionToCell(deposit.GlobalPosition) == cell)
			{
				blockReason = "Blocked by ScrapDeposit";
				return true;
			}
		}

		blockReason = string.Empty;
		return false;
	}

	private Vector2I GetMouseCell()
	{
		return WorldPositionToCell(GetGlobalMousePosition());
	}

	private Vector2I WorldPositionToCell(Vector2 worldPosition)
	{
		return new Vector2I(
			Mathf.RoundToInt(worldPosition.X / gridSize),
			Mathf.RoundToInt(worldPosition.Y / gridSize));
	}

	private Vector2 CellToWorldPosition(Vector2I cell)
	{
		return new Vector2(cell.X * gridSize, cell.Y * gridSize);
	}

	private static void EnsureBuildInputActions()
	{
		EnsureActionHasKey(ToggleBuildModeAction, Key.B);
		EnsureActionHasKey(SelectBuilding1Action, Key.Key1);
		EnsureActionHasKey(SelectBuilding2Action, Key.Key2);
		EnsureActionHasKey(SelectBuilding3Action, Key.Key3);
		EnsureActionHasKey(SelectBuilding4Action, Key.Key4);
		EnsureActionHasKey(SelectBuilding5Action, Key.Key5);
		EnsureActionHasMouseButton(PlaceBuildingAction, MouseButton.Left);
		EnsureActionHasMouseButton(CancelBuildModeAction, MouseButton.Right);
		EnsureActionHasKey(SellBuildingAction, Key.X);
	}

	private static void EnsureActionHasKey(string action, Key key)
	{
		if (!InputMap.HasAction(action))
		{
			InputMap.AddAction(action);
		}

		foreach (InputEvent inputEvent in InputMap.ActionGetEvents(action))
		{
			if (inputEvent is InputEventKey keyEvent &&
			    (keyEvent.PhysicalKeycode == key || keyEvent.Keycode == key))
			{
				return;
			}
		}

		InputMap.ActionAddEvent(action, new InputEventKey
		{
			PhysicalKeycode = key
		});
	}

	private static void EnsureActionHasMouseButton(string action, MouseButton button)
	{
		if (!InputMap.HasAction(action))
		{
			InputMap.AddAction(action);
		}

		foreach (InputEvent inputEvent in InputMap.ActionGetEvents(action))
		{
			if (inputEvent is InputEventMouseButton mouseButtonEvent &&
			    mouseButtonEvent.ButtonIndex == button)
			{
				return;
			}
		}

		InputMap.ActionAddEvent(action, new InputEventMouseButton
		{
			ButtonIndex = button
		});
	}
}
