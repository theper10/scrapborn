using Godot;
using System.Collections.Generic;

public partial class HudController : CanvasLayer
{
	private const string DismissTutorialAction = "DismissTutorialHint";
	private const string ToggleDebugStatsAction = "ToggleDebugStats";

	private readonly Dictionary<BuildingType, PanelContainer> buildSlotPanels = new();
	private readonly Dictionary<BuildingType, Label> buildSlotNameLabels = new();
	private readonly Dictionary<BuildingType, Label> buildSlotCostLabels = new();
	private readonly Dictionary<BuildingType, Label> buildSlotStateLabels = new();

	private ResourceManager resourceManager;
	private RunManager runManager;
	private PlayerController player;
	private PlayerInteraction playerInteraction;
	private BuildingPlacer buildingPlacer;
	private SelectionController selectionController;
	private ObjectiveTracker objectiveTracker;
	private Core core;
	private IInspectable hoveredInspectable;

	private Label scrapLabel;
	private Label energyLabel;
	private Label ammoLabel;
	private Label runStatusLabel;
	private Label runStatsLabel;
	private Label tutorialHintLabel;
	private Label objectiveLabel;
	private Label interactionHintLabel;
	private Label hoverPreviewLabel;
	private Label globalMessageLabel;
	private Label feedbackLabel;
	private Label announcementTitleLabel;
	private Label announcementDetailLabel;
	private Label playerHealthLabel;
	private Label coreHealthLabel;
	private Label buildTooltipTitleLabel;
	private Label buildTooltipBodyLabel;
	private ProgressBar playerHealthBar;
	private ProgressBar coreHealthBar;
	private PanelContainer debugPanel;
	private PanelContainer hintPanel;
	private PanelContainer hoverPreviewPanel;
	private PanelContainer announcementPanel;
	private PanelContainer buildHintPanel;
	private PanelContainer buildTooltipPanel;
	private HBoxContainer buildHotbar;

	private const double FeedbackDuration = 2.2;
	private const double FeedbackRepeatCooldown = 1.2;
	private const double AnnouncementDuration = 3.0;

	private int tutorialStep;
	private bool isTutorialHidden;
	private bool isDebugStatsVisible;
	private bool currentInteractionHintVisible;
	private bool currentObjectiveVisible;
	private string currentInteractionHintText = string.Empty;
	private string currentObjectiveText = string.Empty;
	private string latestBuildStatusText = string.Empty;
	private string lastFeedbackText = string.Empty;
	private double tutorialElapsed;
	private double feedbackTimer;
	private double feedbackRepeatCooldown;
	private double announcementTimer;
	private Vector2 initialPlayerPosition;
	private bool hasInitialPlayerPosition;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		EnsureInputActions();
		CacheNodes();
		ConnectResourceManager();
		ConnectHealthSources();
		RefreshBuildUi();
		RefreshHintPanel();
		UpdateDebugPanelVisibility();
	}

	public override void _Process(double delta)
	{
		tutorialElapsed += delta;
		UpdateTemporaryUi(delta);

		if (Input.IsActionJustPressed(DismissTutorialAction))
		{
			isTutorialHidden = true;
		}

		if (Input.IsActionJustPressed(ToggleDebugStatsAction))
		{
			isDebugStatsVisible = !isDebugStatsVisible;
			UpdateDebugPanelVisibility();
		}

		RefreshTutorialHint();
		RefreshHoverPreview();
		RefreshBuildUi();
		if (isDebugStatsVisible)
		{
			UpdateDebugStats();
		}
	}

	public override void _ExitTree()
	{
		if (resourceManager != null)
		{
			resourceManager.ResourcesChanged -= RefreshResources;
		}

		if (runManager != null)
		{
			runManager.RunStateChanged -= UpdateRunStatus;
			runManager.RunAnnouncement -= ShowRunAnnouncement;
		}

		if (player != null)
		{
			player.HealthChanged -= UpdatePlayerHealth;
		}

		if (playerInteraction != null)
		{
			playerInteraction.InteractionHintChanged -= UpdateInteractionHint;
		}

		if (buildingPlacer != null)
		{
			buildingPlacer.BuildStateChanged -= UpdateBuildStatus;
		}

		if (selectionController != null)
		{
			selectionController.HoverChanged -= UpdateHoverPreview;
		}

		if (objectiveTracker != null)
		{
			objectiveTracker.ObjectiveChanged -= UpdateObjective;
		}

		if (core != null)
		{
			core.HealthChanged -= UpdateCoreHealth;
		}
	}

	private void CacheNodes()
	{
		scrapLabel = GetNode<Label>("Root/TopLeft/ResourcePanel/Margin/VBox/ScrapLabel");
		energyLabel = GetNode<Label>("Root/TopLeft/ResourcePanel/Margin/VBox/EnergyLabel");
		ammoLabel = GetNode<Label>("Root/TopLeft/ResourcePanel/Margin/VBox/AmmoLabel");
		playerHealthLabel = GetNode<Label>("Root/TopLeft/ResourcePanel/Margin/VBox/PlayerHealthLabel");
		playerHealthBar = GetNode<ProgressBar>("Root/TopLeft/ResourcePanel/Margin/VBox/PlayerHealthBar");
		coreHealthLabel = GetNode<Label>("Root/TopLeft/ResourcePanel/Margin/VBox/CoreHealthLabel");
		coreHealthBar = GetNode<ProgressBar>("Root/TopLeft/ResourcePanel/Margin/VBox/CoreHealthBar");

		runStatusLabel = GetNode<Label>("Root/TopCenter/PhasePanel/Margin/VBox/RunStatusLabel");
		globalMessageLabel = GetNode<Label>("Root/TopCenter/PhasePanel/Margin/VBox/GlobalMessageLabel");
		feedbackLabel = GetNode<Label>("Root/TopCenter/PhasePanel/Margin/VBox/FeedbackLabel");
		announcementPanel = GetNode<PanelContainer>("Root/AnnouncementPanel");
		announcementTitleLabel = GetNode<Label>("Root/AnnouncementPanel/Margin/VBox/AnnouncementTitleLabel");
		announcementDetailLabel = GetNode<Label>("Root/AnnouncementPanel/Margin/VBox/AnnouncementDetailLabel");

		debugPanel = GetNode<PanelContainer>("Root/TopRight/DebugPanel");
		runStatsLabel = GetNode<Label>("Root/TopRight/DebugPanel/Margin/VBox/RunStatsLabel");

		hoverPreviewPanel = GetNode<PanelContainer>("Root/BottomLeft/VBox/HoverPreviewPanel");
		hoverPreviewLabel = GetNode<Label>("Root/BottomLeft/VBox/HoverPreviewPanel/Margin/HoverPreviewLabel");
		hintPanel = GetNode<PanelContainer>("Root/BottomLeft/VBox/HintPanel");
		objectiveLabel = GetNode<Label>("Root/BottomLeft/VBox/HintPanel/Margin/VBox/ObjectiveLabel");
		tutorialHintLabel = GetNode<Label>("Root/BottomLeft/VBox/HintPanel/Margin/VBox/TutorialHintLabel");
		interactionHintLabel = GetNode<Label>("Root/BottomLeft/VBox/HintPanel/Margin/VBox/InteractionHintLabel");

		buildTooltipPanel = GetNode<PanelContainer>("Root/BottomCenter/VBox/BuildTooltipPanel");
		buildTooltipTitleLabel = GetNode<Label>("Root/BottomCenter/VBox/BuildTooltipPanel/Margin/VBox/BuildTooltipTitleLabel");
		buildTooltipBodyLabel = GetNode<Label>("Root/BottomCenter/VBox/BuildTooltipPanel/Margin/VBox/BuildTooltipBodyLabel");
		buildHotbar = GetNode<HBoxContainer>("Root/BottomCenter/VBox/BuildHotbar");
		buildHintPanel = GetNode<PanelContainer>("Root/BottomCenter/VBox/BuildHintPanel");
		CacheBuildSlot(BuildingType.Drill, "Slot1");
		CacheBuildSlot(BuildingType.Generator, "Slot2");
		CacheBuildSlot(BuildingType.Assembler, "Slot3");
		CacheBuildSlot(BuildingType.Turret, "Slot4");
		CacheBuildSlot(BuildingType.Storage, "Slot5");
	}

	private void CacheBuildSlot(BuildingType buildingType, string slotName)
	{
		string basePath = $"Root/BottomCenter/VBox/BuildHotbar/{slotName}/Margin/VBox";
		buildSlotPanels[buildingType] = GetNode<PanelContainer>($"Root/BottomCenter/VBox/BuildHotbar/{slotName}");
		buildSlotNameLabels[buildingType] = GetNode<Label>($"{basePath}/NameLabel");
		buildSlotCostLabels[buildingType] = GetNode<Label>($"{basePath}/CostLabel");
		buildSlotStateLabels[buildingType] = GetNode<Label>($"{basePath}/StateLabel");
	}

	private void ConnectResourceManager()
	{
		resourceManager = GetNodeOrNull<ResourceManager>("/root/ResourceManager");

		if (resourceManager == null)
		{
			GD.PushWarning("Hud could not find the ResourceManager autoload.");
			return;
		}

		resourceManager.ResourcesChanged += RefreshResources;
		RefreshResources();

		runManager = GetNodeOrNull<RunManager>("/root/RunManager");
		if (runManager != null)
		{
			runManager.RunStateChanged += UpdateRunStatus;
			runManager.RunAnnouncement += ShowRunAnnouncement;
		}
		else
		{
			GD.PushWarning("Hud could not find the RunManager autoload.");
		}
	}

	private void ConnectHealthSources()
	{
		Node parent = GetParent();

		player = parent?.GetNodeOrNull<PlayerController>("Player");
		if (player != null)
		{
			player.HealthChanged += UpdatePlayerHealth;
			UpdatePlayerHealth(player.CurrentHealth, player.MaxHealth);
			initialPlayerPosition = player.GlobalPosition;
			hasInitialPlayerPosition = true;

			playerInteraction = player.GetNodeOrNull<PlayerInteraction>("InteractionRange");
			if (playerInteraction != null)
			{
				playerInteraction.InteractionHintChanged += UpdateInteractionHint;
				UpdateInteractionHint(playerInteraction.CurrentHintText, playerInteraction.IsHintVisible);
			}
		}
		else
		{
			GD.PushWarning("Hud could not find the Player node.");
		}

		core = parent?.GetNodeOrNull<Core>("TestWorld/Core");
		if (core != null)
		{
			core.HealthChanged += UpdateCoreHealth;
			UpdateCoreHealth(core.CurrentHealth, core.MaxHealth);
		}
		else
		{
			GD.PushWarning("Hud could not find the Core node.");
		}

		buildingPlacer = parent?.GetNodeOrNull<BuildingPlacer>("BuildingPlacer");
		if (buildingPlacer != null)
		{
			buildingPlacer.BuildStateChanged += UpdateBuildStatus;
			UpdateBuildStatus(
				buildingPlacer.IsBuildModeActive,
				buildingPlacer.SelectedBuildingName,
				buildingPlacer.SelectedBuildingCost,
				buildingPlacer.StatusText);
		}
		else
		{
			GD.PushWarning("Hud could not find the BuildingPlacer node.");
		}

		selectionController = parent?.GetNodeOrNull<SelectionController>("SelectionController");
		if (selectionController != null)
		{
			selectionController.HoverChanged += UpdateHoverPreview;
			UpdateHoverPreview(selectionController.HoveredNode);
		}
		else
		{
			GD.PushWarning("Hud could not find the SelectionController node.");
		}

		objectiveTracker = parent?.GetNodeOrNull<ObjectiveTracker>("ObjectiveTracker");
		if (objectiveTracker != null)
		{
			objectiveTracker.ObjectiveChanged += UpdateObjective;
			UpdateObjective(objectiveTracker.CurrentObjectiveText, objectiveTracker.IsObjectiveVisible);
		}
		else
		{
			GD.PushWarning("Hud could not find the ObjectiveTracker node.");
		}
	}

	private void RefreshResources()
	{
		if (resourceManager == null)
		{
			return;
		}

		UpdateResourceLabel(scrapLabel, "Scrap", ResourceType.Scrap);
		UpdateResourceLabel(energyLabel, "Energy", ResourceType.Energy);
		UpdateResourceLabel(ammoLabel, "Ammo", ResourceType.Ammo);
		RefreshBuildUi();
	}

	private void UpdateResourceLabel(Label label, string displayName, ResourceType type)
	{
		label.Text = $"{displayName} {resourceManager.GetAmount(type)} / {resourceManager.GetMax(type)}";
	}

	private void UpdatePlayerHealth(int currentHealth, int maxHealth)
	{
		playerHealthLabel.Text = $"Player HP {currentHealth} / {maxHealth}";
		playerHealthBar.MaxValue = maxHealth;
		playerHealthBar.Value = currentHealth;
	}

	private void UpdateCoreHealth(int currentHealth, int maxHealth)
	{
		coreHealthLabel.Text = $"Core HP {currentHealth} / {maxHealth}";
		coreHealthBar.MaxValue = maxHealth;
		coreHealthBar.Value = currentHealth;

		if (currentHealth > 0 && maxHealth > 0 && currentHealth <= Mathf.CeilToInt(maxHealth * 0.3f))
		{
			ShowTemporaryMessage("Warning: Core integrity low");
		}

		if (runManager == null)
		{
			globalMessageLabel.Visible = currentHealth <= 0;
			globalMessageLabel.Text = currentHealth <= 0 ? "Core destroyed" : string.Empty;
		}
	}

	private void UpdateInteractionHint(string hintText, bool isVisible)
	{
		currentInteractionHintText = hintText;
		currentInteractionHintVisible = isVisible;
		if (isVisible && (hintText == "Need Scrap to repair" || hintText == "Scrap storage full"))
		{
			ShowTemporaryMessage(hintText);
		}

		RefreshHintPanel();
	}

	private void UpdateBuildStatus(
		bool isBuildModeActive,
		string selectedBuildingName,
		string selectedBuildingCost,
		string statusText)
	{
		latestBuildStatusText = statusText;
		if (isBuildModeActive && IsActionableBuildStatus(statusText))
		{
			ShowTemporaryMessage(statusText);
		}

		RefreshBuildUi();
	}

	private void UpdateObjective(string objectiveText, bool isVisible)
	{
		currentObjectiveText = objectiveText;
		currentObjectiveVisible = isVisible;
		RefreshHintPanel();
	}

	private void UpdateRunStatus(
		string phaseText,
		string detailText,
		string messageText,
		bool isRunOver)
	{
		runStatusLabel.Text = string.IsNullOrEmpty(detailText)
			? phaseText
			: $"{phaseText} - {detailText}";

		UpdateDebugStats();
		globalMessageLabel.Visible = isRunOver;
		globalMessageLabel.Text = messageText;
	}

	private void ShowRunAnnouncement(string titleText, string detailText)
	{
		announcementTitleLabel.Text = titleText;
		announcementDetailLabel.Text = detailText;
		announcementPanel.Visible = true;
		announcementTimer = AnnouncementDuration;
	}

	private void RefreshBuildUi()
	{
		if (!CanShowGameplayControls())
		{
			buildHintPanel.Visible = false;
			buildTooltipPanel.Visible = false;
			buildHotbar.Visible = false;
			return;
		}

		bool isBuildModeActive = buildingPlacer?.IsBuildModeActive == true;
		buildHintPanel.Visible = !isBuildModeActive;
		buildTooltipPanel.Visible = isBuildModeActive;
		buildHotbar.Visible = isBuildModeActive;

		if (!isBuildModeActive || buildingPlacer == null)
		{
			return;
		}

		RefreshBuildHotbar();
		RefreshPlacementTooltip();
	}

	private void RefreshBuildHotbar()
	{
		foreach (BuildingType buildingType in System.Enum.GetValues<BuildingType>())
		{
			BuildingDefinition definition = BuildingDefinitions.Get(buildingType);
			bool isSelected = buildingPlacer.SelectedBuildingType == buildingType;
			bool canAfford = buildingPlacer.CanAfford(buildingType);
			int hotkey = (int)buildingType + 1;

			buildSlotNameLabels[buildingType].Text = isSelected
				? $"> {hotkey} {definition.DisplayName} <"
				: $"{hotkey} {definition.DisplayName}";
			buildSlotCostLabels[buildingType].Text = buildingPlacer.GetBuildingCompactCostText(buildingType);
			buildSlotStateLabels[buildingType].Text = canAfford ? "CAN" : "NEED";

			buildSlotPanels[buildingType].Modulate = isSelected
				? new Color(1f, 0.92f, 0.42f, 1f)
				: canAfford
					? Colors.White
					: new Color(0.72f, 0.48f, 0.48f, 0.82f);
		}
	}

	private bool CanShowGameplayControls()
	{
		return !GetTree().Paused &&
		       (runManager == null || runManager.CurrentPhase is RunPhase.Day or RunPhase.Night);
	}

	private void RefreshPlacementTooltip()
	{
		buildTooltipTitleLabel.Text = buildingPlacer.SelectedBuildingName;
		string bodyText = buildingPlacer.PlacementTooltipText;
		string selectedName = buildingPlacer.SelectedBuildingName;
		if (bodyText.StartsWith($"{selectedName}\n"))
		{
			bodyText = bodyText[(selectedName.Length + 1)..];
		}

		if (!string.IsNullOrEmpty(latestBuildStatusText))
		{
			bodyText += $"\n{latestBuildStatusText}";
		}

		buildTooltipBodyLabel.Text = bodyText;
	}

	private void UpdateHoverPreview(Node hoveredNode)
	{
		hoveredInspectable = hoveredNode as IInspectable;
		RefreshHoverPreview();
	}

	private void RefreshHoverPreview()
	{
		if (hoveredInspectable == null ||
		    !hoveredInspectable.IsSelectable ||
		    GetTree().Paused ||
		    buildingPlacer?.IsBuildModeActive == true)
		{
			hoverPreviewPanel.Visible = false;
			return;
		}

		hoverPreviewLabel.Text = hoveredInspectable.GetHoverText();
		hoverPreviewPanel.Visible = true;
	}

	private void RefreshTutorialHint()
	{
		if (currentObjectiveVisible ||
		    isTutorialHidden ||
		    runManager == null ||
		    GetTree().Paused ||
		    runManager.CurrentPhase is not (RunPhase.Day or RunPhase.Night) ||
		    runManager.IsRunOver)
		{
			tutorialHintLabel.Visible = false;
			RefreshHintPanel();
			return;
		}

		AdvanceTutorialStep();
		string hintText = GetTutorialHintText();
		tutorialHintLabel.Text = string.IsNullOrEmpty(hintText)
			? string.Empty
			: $"{hintText}\nH: hide hint";
		tutorialHintLabel.Visible = !string.IsNullOrEmpty(hintText);
		RefreshHintPanel();
	}

	private void RefreshHintPanel()
	{
		objectiveLabel.Text = string.IsNullOrEmpty(currentObjectiveText)
			? string.Empty
			: $"Objective: {currentObjectiveText}";
		objectiveLabel.Visible = currentObjectiveVisible;
		interactionHintLabel.Text = currentInteractionHintText;
		interactionHintLabel.Visible = currentInteractionHintVisible;
		hintPanel.Visible = currentObjectiveVisible || tutorialHintLabel.Visible || currentInteractionHintVisible;
	}

	private void UpdateDebugPanelVisibility()
	{
		debugPanel.Visible = isDebugStatsVisible;
		if (isDebugStatsVisible)
		{
			UpdateDebugStats();
		}
	}

	private void UpdateDebugStats()
	{
		if (runManager == null)
		{
			runStatsLabel.Text = "No run data";
			return;
		}

		RunStats stats = runManager.Stats;
		runStatsLabel.Text =
			$"Kills: {stats.EnemiesKilled}\n" +
			$"Buildings: {stats.BuildingsPlaced} | Lost: {stats.BuildingsDestroyed}\n" +
			$"Manual Scrap: {stats.ScrapGatheredManually}\n" +
			$"Drill Scrap: {stats.ScrapProducedByDrills}\n" +
			$"Energy: {stats.EnergyProduced} | Ammo: {stats.AmmoProduced}\n" +
			$"Repairs: {stats.HealthRepaired} HP / {stats.ScrapSpentOnRepairs} Scrap\n" +
			$"Upgrades: {stats.UpgradesChosen} | Nights: {stats.NightsSurvived}";
	}

	private void AdvanceTutorialStep()
	{
		if (tutorialStep == 0 && (tutorialElapsed >= 6.0 || HasPlayerMoved()))
		{
			tutorialStep = 1;
		}

		if (runManager.Stats.ScrapGatheredManually > 0)
		{
			tutorialStep = Mathf.Max(tutorialStep, 2);
		}

		if (buildingPlacer?.IsBuildModeActive == true || runManager.Stats.BuildingsPlaced > 0)
		{
			tutorialStep = Mathf.Max(tutorialStep, 3);
		}

		if (HasPlacedBuilding(BuildingType.Generator) || runManager.Stats.EnergyProduced > 0)
		{
			tutorialStep = Mathf.Max(tutorialStep, 4);
		}

		if (HasPlacedBuilding(BuildingType.Assembler) || runManager.Stats.AmmoProduced > 0)
		{
			tutorialStep = Mathf.Max(tutorialStep, 5);
		}

		if (HasPlacedBuilding(BuildingType.Turret))
		{
			tutorialStep = Mathf.Max(tutorialStep, 6);
		}

		if (runManager.Stats.NightsSurvived > 0)
		{
			tutorialStep = Mathf.Max(tutorialStep, 7);
		}

		if (runManager.Stats.UpgradesChosen > 0)
		{
			isTutorialHidden = true;
		}
	}

	private bool HasPlayerMoved()
	{
		return hasInitialPlayerPosition &&
		       player != null &&
		       player.GlobalPosition.DistanceSquaredTo(initialPlayerPosition) > 24f * 24f;
	}

	private bool HasPlacedBuilding(BuildingType buildingType)
	{
		foreach (Node node in GetTree().GetNodesInGroup("Buildings"))
		{
			if (node is Building building &&
			    building.BuildingType == buildingType &&
			    !building.IsDestroyed)
			{
				return true;
			}
		}

		return false;
	}

	private string GetTutorialHintText()
	{
		return tutorialStep switch
		{
			0 => "WASD or Arrow Keys to move.",
			1 => "Walk to Scrap and press E to gather.",
			2 => "Press B to enter Build Mode.",
			3 => "Build a Generator to make Energy.",
			4 => "Build an Assembler to make Ammo.",
			5 => "Build a Turret before Night begins.",
			6 => "Survive the night. Repair with F.",
			7 => "Choose an upgrade after each night.",
			_ => string.Empty
		};
	}

	private void UpdateTemporaryUi(double delta)
	{
		if (feedbackRepeatCooldown > 0.0)
		{
			feedbackRepeatCooldown -= delta;
		}

		if (feedbackTimer > 0.0)
		{
			feedbackTimer -= delta;
			if (feedbackTimer <= 0.0)
			{
				feedbackLabel.Visible = false;
			}
		}

		if (announcementTimer > 0.0)
		{
			announcementTimer -= delta;
			if (announcementTimer <= 0.0)
			{
				announcementPanel.Visible = false;
			}
		}
	}

	private void ShowTemporaryMessage(string message)
	{
		if (string.IsNullOrWhiteSpace(message))
		{
			return;
		}

		if (message == lastFeedbackText && feedbackRepeatCooldown > 0.0)
		{
			return;
		}

		lastFeedbackText = message;
		feedbackRepeatCooldown = FeedbackRepeatCooldown;
		feedbackTimer = FeedbackDuration;
		feedbackLabel.Text = message;
		feedbackLabel.Visible = true;
	}

	private static bool IsActionableBuildStatus(string statusText)
	{
		return statusText is
			"Not enough resources" or
			"Cell occupied" or
			"Blocked by Core" or
			"Blocked by ScrapDeposit" or
			"Invalid location" or
			"Invalid placement";
	}

	private static void EnsureInputActions()
	{
		EnsureActionHasKey(DismissTutorialAction, Key.H);
		EnsureActionHasKey(ToggleDebugStatsAction, Key.F9);
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
}
