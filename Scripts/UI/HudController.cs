using Godot;

public partial class HudController : CanvasLayer
{
	private ResourceManager resourceManager;
	private RunManager runManager;
	private PlayerController player;
	private PlayerInteraction playerInteraction;
	private BuildingPlacer buildingPlacer;
	private Core core;

	private Label scrapLabel;
	private Label energyLabel;
	private Label ammoLabel;
	private Label runStatusLabel;
	private Label interactionHintLabel;
	private Label buildStatusLabel;
	private Label globalMessageLabel;
	private Label playerHealthLabel;
	private Label coreHealthLabel;
	private ProgressBar playerHealthBar;
	private ProgressBar coreHealthBar;

	public override void _Ready()
	{
		CacheNodes();
		ConnectResourceManager();
		ConnectHealthSources();
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

		if (core != null)
		{
			core.HealthChanged -= UpdateCoreHealth;
		}
	}

	private void CacheNodes()
	{
		scrapLabel = GetNode<Label>("Root/VBox/Resources/ScrapLabel");
		energyLabel = GetNode<Label>("Root/VBox/Resources/EnergyLabel");
		ammoLabel = GetNode<Label>("Root/VBox/Resources/AmmoLabel");
		runStatusLabel = GetNode<Label>("Root/VBox/RunStatusLabel");
		interactionHintLabel = GetNode<Label>("Root/VBox/InteractionHintLabel");
		buildStatusLabel = GetNode<Label>("Root/VBox/BuildStatusLabel");
		globalMessageLabel = GetNode<Label>("Root/VBox/GlobalMessageLabel");
		playerHealthLabel = GetNode<Label>("Root/VBox/PlayerHealthLabel");
		coreHealthLabel = GetNode<Label>("Root/VBox/CoreHealthLabel");
		playerHealthBar = GetNode<ProgressBar>("Root/VBox/PlayerHealthBar");
		coreHealthBar = GetNode<ProgressBar>("Root/VBox/CoreHealthBar");
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
	}

	private void UpdateResourceLabel(Label label, string displayName, ResourceType type)
	{
		label.Text = $"{displayName}: {resourceManager.GetAmount(type)} / {resourceManager.GetMax(type)}";
	}

	private void UpdatePlayerHealth(int currentHealth, int maxHealth)
	{
		playerHealthLabel.Text = $"Player: {currentHealth} / {maxHealth}";
		playerHealthBar.MaxValue = maxHealth;
		playerHealthBar.Value = currentHealth;
	}

	private void UpdateCoreHealth(int currentHealth, int maxHealth)
	{
		coreHealthLabel.Text = $"Core: {currentHealth} / {maxHealth}";
		coreHealthBar.MaxValue = maxHealth;
		coreHealthBar.Value = currentHealth;

		if (runManager == null)
		{
			globalMessageLabel.Visible = currentHealth <= 0;
			globalMessageLabel.Text = currentHealth <= 0 ? "Core destroyed" : string.Empty;
		}
	}

	private void UpdateInteractionHint(string hintText, bool isVisible)
	{
		interactionHintLabel.Text = hintText;
		interactionHintLabel.Visible = isVisible;
	}

	private void UpdateBuildStatus(
		bool isBuildModeActive,
		string selectedBuildingName,
		string selectedBuildingCost,
		string statusText)
	{
		string modeText = isBuildModeActive ? "On" : "Off";
		buildStatusLabel.Text = $"Build: {modeText}\nSelected: {selectedBuildingName}\nCost: {selectedBuildingCost}";

		if (!string.IsNullOrEmpty(statusText))
		{
			buildStatusLabel.Text += $"\n{statusText}";
		}
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

		globalMessageLabel.Visible = isRunOver;
		globalMessageLabel.Text = messageText;
	}
}
