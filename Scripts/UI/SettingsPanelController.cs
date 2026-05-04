using Godot;

public partial class SettingsPanelController : Control
{
	[Signal]
	public delegate void ClosedEventHandler();

	private SettingsManager settingsManager;
	private Button feedbackButton;
	private Button cameraShakeButton;
	private Button cameraShakeStrengthButton;
	private Button debugStatsButton;
	private Button fullscreenButton;
	private Button vsyncButton;
	private Button closeButton;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		settingsManager = GetNodeOrNull<SettingsManager>("/root/SettingsManager");
		feedbackButton = GetNode<Button>("Dim/Panel/Margin/VBox/FeedbackButton");
		cameraShakeButton = GetNode<Button>("Dim/Panel/Margin/VBox/CameraShakeButton");
		cameraShakeStrengthButton = GetNode<Button>("Dim/Panel/Margin/VBox/CameraShakeStrengthButton");
		debugStatsButton = GetNode<Button>("Dim/Panel/Margin/VBox/DebugStatsButton");
		fullscreenButton = GetNode<Button>("Dim/Panel/Margin/VBox/FullscreenButton");
		vsyncButton = GetNode<Button>("Dim/Panel/Margin/VBox/VSyncButton");
		closeButton = GetNode<Button>("Dim/Panel/Margin/VBox/CloseButton");

		feedbackButton.Pressed += CycleFeedbackIntensity;
		cameraShakeButton.Pressed += ToggleCameraShake;
		cameraShakeStrengthButton.Pressed += CycleCameraShakeStrength;
		debugStatsButton.Pressed += ToggleDebugStats;
		fullscreenButton.Pressed += ToggleFullscreen;
		vsyncButton.Pressed += ToggleVSync;
		closeButton.Pressed += Close;

		if (settingsManager != null)
		{
			settingsManager.SettingsChanged += RefreshLabels;
		}
		else
		{
			GD.PushWarning("SettingsPanel could not find the SettingsManager autoload.");
		}

		Visible = false;
		RefreshLabels();
	}

	public override void _ExitTree()
	{
		if (feedbackButton != null)
		{
			feedbackButton.Pressed -= CycleFeedbackIntensity;
		}

		if (cameraShakeButton != null)
		{
			cameraShakeButton.Pressed -= ToggleCameraShake;
		}

		if (cameraShakeStrengthButton != null)
		{
			cameraShakeStrengthButton.Pressed -= CycleCameraShakeStrength;
		}

		if (debugStatsButton != null)
		{
			debugStatsButton.Pressed -= ToggleDebugStats;
		}

		if (fullscreenButton != null)
		{
			fullscreenButton.Pressed -= ToggleFullscreen;
		}

		if (vsyncButton != null)
		{
			vsyncButton.Pressed -= ToggleVSync;
		}

		if (closeButton != null)
		{
			closeButton.Pressed -= Close;
		}

		if (settingsManager != null)
		{
			settingsManager.SettingsChanged -= RefreshLabels;
		}
	}

	public void Open()
	{
		RefreshLabels();
		Visible = true;
		feedbackButton.GrabFocus();
	}

	public void Close()
	{
		Visible = false;
		EmitSignal(SignalName.Closed);
	}

	private void CycleFeedbackIntensity()
	{
		settingsManager?.CycleFeedbackIntensity();
	}

	private void ToggleCameraShake()
	{
		settingsManager?.ToggleCameraShake();
	}

	private void CycleCameraShakeStrength()
	{
		settingsManager?.CycleCameraShakeStrength();
	}

	private void ToggleDebugStats()
	{
		settingsManager?.ToggleDebugStats();
	}

	private void ToggleFullscreen()
	{
		settingsManager?.ToggleFullscreen();
	}

	private void ToggleVSync()
	{
		settingsManager?.ToggleVSync();
	}

	private void RefreshLabels()
	{
		if (settingsManager == null)
		{
			feedbackButton.Text = "Feedback Intensity: Medium";
			cameraShakeButton.Text = "Camera Shake: On";
			cameraShakeStrengthButton.Text = "Camera Shake Strength: 100%";
			debugStatsButton.Text = "Debug Stats: Off";
			fullscreenButton.Text = "Display: Windowed";
			vsyncButton.Text = "VSync: Project Default";
			return;
		}

		feedbackButton.Text = settingsManager.GetFeedbackIntensityLabel();
		cameraShakeButton.Text = settingsManager.GetCameraShakeLabel();
		cameraShakeStrengthButton.Text = settingsManager.GetCameraShakeStrengthLabel();
		debugStatsButton.Text = settingsManager.GetDebugStatsLabel();
		fullscreenButton.Text = settingsManager.GetFullscreenLabel();
		vsyncButton.Text = settingsManager.GetVSyncLabel();
	}
}
