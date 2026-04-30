using Godot;

public partial class HudController : CanvasLayer
{
	private ResourceManager resourceManager;
	private PlayerController player;
	private PlayerInteraction playerInteraction;
	private Core core;

	private Label scrapLabel;
	private Label energyLabel;
	private Label ammoLabel;
	private Label interactionHintLabel;
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

		if (player != null)
		{
			player.HealthChanged -= UpdatePlayerHealth;
		}

		if (playerInteraction != null)
		{
			playerInteraction.InteractionHintChanged -= UpdateInteractionHint;
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
		interactionHintLabel = GetNode<Label>("Root/VBox/InteractionHintLabel");
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
	}

	private void UpdateInteractionHint(string hintText, bool isVisible)
	{
		interactionHintLabel.Text = hintText;
		interactionHintLabel.Visible = isVisible;
	}
}
