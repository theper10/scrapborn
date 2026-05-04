using Godot;

public partial class UpgradeScreenController : CanvasLayer
{
	private const string SelectUpgrade1Action = "SelectUpgradeChoice1";
	private const string SelectUpgrade2Action = "SelectUpgradeChoice2";
	private const string SelectUpgrade3Action = "SelectUpgradeChoice3";

	private UpgradeManager upgradeManager;
	private Control root;
	private Button[] choiceButtons;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		EnsureUpgradeInputActions();
		CacheNodes();
		ConnectButtons();
		ConnectUpgradeManager();
		SetScreenVisible(false);
	}

	public override void _ExitTree()
	{
		if (upgradeManager != null)
		{
			upgradeManager.UpgradeChoicesChanged -= OnUpgradeChoicesChanged;
		}
	}

	public override void _Process(double delta)
	{
		if (!root.Visible || upgradeManager == null)
		{
			return;
		}

		if (Input.IsActionJustPressed(SelectUpgrade1Action))
		{
			SelectChoice(0);
		}
		else if (Input.IsActionJustPressed(SelectUpgrade2Action))
		{
			SelectChoice(1);
		}
		else if (Input.IsActionJustPressed(SelectUpgrade3Action))
		{
			SelectChoice(2);
		}
	}

	private void CacheNodes()
	{
		root = GetNode<Control>("Root");
		choiceButtons = new[]
		{
			GetNode<Button>("Root/Panel/VBox/Choice1Button"),
			GetNode<Button>("Root/Panel/VBox/Choice2Button"),
			GetNode<Button>("Root/Panel/VBox/Choice3Button")
		};
	}

	private void ConnectButtons()
	{
		choiceButtons[0].Pressed += () => SelectChoice(0);
		choiceButtons[1].Pressed += () => SelectChoice(1);
		choiceButtons[2].Pressed += () => SelectChoice(2);
	}

	private void ConnectUpgradeManager()
	{
		upgradeManager = GetNodeOrNull<UpgradeManager>("/root/UpgradeManager");
		if (upgradeManager == null)
		{
			GD.PushWarning("UpgradeScreen could not find the UpgradeManager autoload.");
			return;
		}

		upgradeManager.UpgradeChoicesChanged += OnUpgradeChoicesChanged;
	}

	private void OnUpgradeChoicesChanged(bool isVisible)
	{
		SetScreenVisible(isVisible);
		if (isVisible)
		{
			RefreshChoices();
		}
	}

	private void RefreshChoices()
	{
		for (int index = 0; index < choiceButtons.Length; index++)
		{
			if (index >= upgradeManager.CurrentChoices.Count)
			{
				choiceButtons[index].Visible = false;
				continue;
			}

			UpgradeDefinition upgrade = upgradeManager.CurrentChoices[index];
			choiceButtons[index].Visible = true;
			choiceButtons[index].Text =
				$"{index + 1}. {upgrade.DisplayName}\n" +
				$"{upgrade.Rarity} | {upgrade.Category}\n" +
				upgrade.Description;
		}
	}

	private void SelectChoice(int index)
	{
		if (upgradeManager == null || index >= upgradeManager.CurrentChoices.Count)
		{
			return;
		}

		upgradeManager.SelectUpgrade(index);
	}

	private void SetScreenVisible(bool isVisible)
	{
		root.Visible = isVisible;
	}

	private static void EnsureUpgradeInputActions()
	{
		EnsureActionHasKey(SelectUpgrade1Action, Key.Key1);
		EnsureActionHasKey(SelectUpgrade2Action, Key.Key2);
		EnsureActionHasKey(SelectUpgrade3Action, Key.Key3);
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
