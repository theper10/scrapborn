using Godot;

public partial class MainMenuController : CanvasLayer
{
	private const string GameplayScenePath = "res://Scenes/Main.tscn";

	private Button newRunButton;
	private Button settingsButton;
	private Button controlsButton;
	private Button quitButton;
	private Control controlsPanel;
	private SettingsPanelController settingsPanel;

	public override void _Ready()
	{
		GetTree().Paused = false;
		newRunButton = GetNode<Button>("Root/Panel/Margin/VBox/NewRunButton");
		settingsButton = GetNode<Button>("Root/Panel/Margin/VBox/SettingsButton");
		controlsButton = GetNode<Button>("Root/Panel/Margin/VBox/ControlsButton");
		quitButton = GetNode<Button>("Root/Panel/Margin/VBox/QuitButton");
		controlsPanel = GetNode<Control>("Root/Panel/Margin/VBox/ControlsPanel");
		settingsPanel = GetNode<SettingsPanelController>("SettingsPanel");

		newRunButton.Pressed += StartNewRun;
		settingsButton.Pressed += OpenSettings;
		controlsButton.Pressed += ToggleControlsPanel;
		quitButton.Pressed += QuitToDesktop;
		controlsPanel.Visible = false;
		newRunButton.GrabFocus();
	}

	public override void _ExitTree()
	{
		if (newRunButton != null)
		{
			newRunButton.Pressed -= StartNewRun;
		}

		if (settingsButton != null)
		{
			settingsButton.Pressed -= OpenSettings;
		}

		if (controlsButton != null)
		{
			controlsButton.Pressed -= ToggleControlsPanel;
		}

		if (quitButton != null)
		{
			quitButton.Pressed -= QuitToDesktop;
		}
	}

	private void StartNewRun()
	{
		GetNodeOrNull<RunManager>("/root/RunManager")?.PrepareNewRun();
		GetTree().ChangeSceneToFile(GameplayScenePath);
	}

	private void ToggleControlsPanel()
	{
		controlsPanel.Visible = !controlsPanel.Visible;
	}

	private void OpenSettings()
	{
		controlsPanel.Visible = false;
		settingsPanel.Open();
	}

	private void QuitToDesktop()
	{
		GetTree().Quit();
	}
}
