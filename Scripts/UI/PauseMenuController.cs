using Godot;

public partial class PauseMenuController : CanvasLayer
{
	private const string PauseAction = "PauseMenu";

	private RunManager runManager;
	private Control root;
	private Button resumeButton;
	private Button restartButton;
	private Button mainMenuButton;
	private Button feedbackButton;
	private Button quitButton;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		EnsurePauseInputAction();
		runManager = GetNodeOrNull<RunManager>("/root/RunManager");
		root = GetNode<Control>("Root");
		resumeButton = GetNode<Button>("Root/Panel/VBox/ResumeButton");
		restartButton = GetNode<Button>("Root/Panel/VBox/RestartButton");
		mainMenuButton = GetNode<Button>("Root/Panel/VBox/MainMenuButton");
		feedbackButton = GetNode<Button>("Root/Panel/VBox/FeedbackButton");
		quitButton = GetNode<Button>("Root/Panel/VBox/QuitButton");

		resumeButton.Pressed += Resume;
		restartButton.Pressed += RestartRun;
		mainMenuButton.Pressed += ReturnToMainMenu;
		feedbackButton.Pressed += CycleFeedbackIntensity;
		quitButton.Pressed += QuitToDesktop;
		UpdateFeedbackButtonLabel();
		root.Visible = false;
	}

	public override void _ExitTree()
	{
		if (resumeButton != null)
		{
			resumeButton.Pressed -= Resume;
		}

		if (restartButton != null)
		{
			restartButton.Pressed -= RestartRun;
		}

		if (mainMenuButton != null)
		{
			mainMenuButton.Pressed -= ReturnToMainMenu;
		}

		if (feedbackButton != null)
		{
			feedbackButton.Pressed -= CycleFeedbackIntensity;
		}

		if (quitButton != null)
		{
			quitButton.Pressed -= QuitToDesktop;
		}
	}

	public override void _Process(double delta)
	{
		if (!Input.IsActionJustPressed(PauseAction))
		{
			return;
		}

		if (!CanTogglePause())
		{
			return;
		}

		SetPaused(!GetTree().Paused);
	}

	private bool CanTogglePause()
	{
		if (runManager == null)
		{
			return true;
		}

		return runManager.CurrentPhase is RunPhase.Day or RunPhase.Night;
	}

	private void Resume()
	{
		SetPaused(false);
	}

	private void RestartRun()
	{
		SetPaused(false);
		runManager?.RestartRun();
	}

	private void ReturnToMainMenu()
	{
		SetPaused(false);
		runManager?.ReturnToMainMenu();
	}

	private void QuitToDesktop()
	{
		GetTree().Quit();
	}

	private void CycleFeedbackIntensity()
	{
		FeedbackEffects.CycleIntensity();
		UpdateFeedbackButtonLabel();
	}

	private void UpdateFeedbackButtonLabel()
	{
		if (feedbackButton != null)
		{
			feedbackButton.Text = FeedbackEffects.GetIntensityLabel();
		}
	}

	private void SetPaused(bool paused)
	{
		GetTree().Paused = paused;
		root.Visible = paused;
		if (paused)
		{
			UpdateFeedbackButtonLabel();
			resumeButton.GrabFocus();
		}
	}

	private static void EnsurePauseInputAction()
	{
		if (!InputMap.HasAction(PauseAction))
		{
			InputMap.AddAction(PauseAction);
		}

		foreach (InputEvent inputEvent in InputMap.ActionGetEvents(PauseAction))
		{
			if (inputEvent is InputEventKey keyEvent &&
			    (keyEvent.PhysicalKeycode == Key.Escape || keyEvent.Keycode == Key.Escape))
			{
				return;
			}
		}

		InputMap.ActionAddEvent(PauseAction, new InputEventKey
		{
			PhysicalKeycode = Key.Escape
		});
	}
}
