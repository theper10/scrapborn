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

	private readonly Dictionary<Vector2I, Building> placedBuildings = new();
	private readonly HashSet<Vector2I> blockedCells = new();
	private readonly Dictionary<BuildingType, PackedScene> buildingScenes = new();

	private ResourceManager resourceManager;
	private Node2D buildingsRoot;
	private Polygon2D preview;
	private BuildingType selectedBuildingType = BuildingType.Drill;
	private bool isBuildModeActive;
	private string statusText = string.Empty;

	[Export]
	private int gridSize = 64;

	public bool IsBuildModeActive => isBuildModeActive;
	public string SelectedBuildingName => BuildingDefinitions.Get(selectedBuildingType).DisplayName;
	public string SelectedBuildingCost => BuildingDefinitions.FormatCost(selectedBuildingType);
	public string StatusText => statusText;

	public override void _Ready()
	{
		EnsureBuildInputActions();
		resourceManager = GetNodeOrNull<ResourceManager>("/root/ResourceManager");
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
			SetStatus(placementError);
			return;
		}

		BuildingDefinition definition = BuildingDefinitions.Get(selectedBuildingType);
		if (!buildingScenes.TryGetValue(selectedBuildingType, out PackedScene scene) || scene == null)
		{
			SetStatus("Invalid placement");
			return;
		}

		if (resourceManager == null || !resourceManager.Spend(definition.Cost))
		{
			SetStatus("Not enough resources");
			return;
		}

		Building building = scene.Instantiate<Building>();
		building.GlobalPosition = CellToWorldPosition(targetCell);
		buildingsRoot.AddChild(building);
		placedBuildings[targetCell] = building;
		SetStatus($"{definition.DisplayName} placed");
	}

	private bool CanPlaceAt(Vector2I cell, out string placementError)
	{
		if (buildingsRoot == null)
		{
			placementError = "Invalid placement";
			return false;
		}

		if (placedBuildings.ContainsKey(cell) || blockedCells.Contains(cell))
		{
			placementError = "Cell occupied";
			return false;
		}

		BuildingDefinition definition = BuildingDefinitions.Get(selectedBuildingType);
		if (resourceManager == null || !resourceManager.CanSpend(definition.Cost))
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

	private void UpdatePreview()
	{
		if (preview == null)
		{
			return;
		}

		preview.Visible = isBuildModeActive;
		if (!isBuildModeActive)
		{
			return;
		}

		Vector2I targetCell = GetMouseCell();
		preview.GlobalPosition = CellToWorldPosition(targetCell);
		bool isValid = CanPlaceAt(targetCell, out _);
		preview.Color = isValid
			? new Color(0.25f, 0.95f, 0.45f, 0.45f)
			: new Color(0.95f, 0.2f, 0.18f, 0.45f);
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
			if (child is Core or ScrapDeposit)
			{
				blockedCells.Add(WorldPositionToCell(((Node2D)child).GlobalPosition));
			}
		}
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
