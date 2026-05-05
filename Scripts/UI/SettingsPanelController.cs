using Godot;

public partial class SettingsPanelController : Control
{
	[Signal]
	public delegate void ClosedEventHandler();

	private SettingsManager settingsManager;
	private AudioManager audioManager;
	private Button feedbackButton;
	private Button cameraShakeButton;
	private Button cameraShakeStrengthButton;
	private Button debugStatsButton;
	private Button fullscreenButton;
	private Button vsyncButton;
	private Button masterVolumeButton;
	private Button sfxVolumeButton;
	private Button musicVolumeButton;
	private Button muteAudioButton;
	private Button closeButton;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		settingsManager = GetNodeOrNull<SettingsManager>("/root/SettingsManager");
		audioManager = GetNodeOrNull<AudioManager>("/root/AudioManager");
		feedbackButton = GetNode<Button>("Dim/Panel/Margin/VBox/FeedbackButton");
		cameraShakeButton = GetNode<Button>("Dim/Panel/Margin/VBox/CameraShakeButton");
		cameraShakeStrengthButton = GetNode<Button>("Dim/Panel/Margin/VBox/CameraShakeStrengthButton");
		debugStatsButton = GetNode<Button>("Dim/Panel/Margin/VBox/DebugStatsButton");
		fullscreenButton = GetNode<Button>("Dim/Panel/Margin/VBox/FullscreenButton");
		vsyncButton = GetNode<Button>("Dim/Panel/Margin/VBox/VSyncButton");
		masterVolumeButton = GetNode<Button>("Dim/Panel/Margin/VBox/MasterVolumeButton");
		sfxVolumeButton = GetNode<Button>("Dim/Panel/Margin/VBox/SfxVolumeButton");
		musicVolumeButton = GetNode<Button>("Dim/Panel/Margin/VBox/MusicVolumeButton");
		muteAudioButton = GetNode<Button>("Dim/Panel/Margin/VBox/MuteAudioButton");
		closeButton = GetNode<Button>("Dim/Panel/Margin/VBox/CloseButton");

		feedbackButton.Pressed += CycleFeedbackIntensity;
		cameraShakeButton.Pressed += ToggleCameraShake;
		cameraShakeStrengthButton.Pressed += CycleCameraShakeStrength;
		debugStatsButton.Pressed += ToggleDebugStats;
		fullscreenButton.Pressed += ToggleFullscreen;
		vsyncButton.Pressed += ToggleVSync;
		masterVolumeButton.Pressed += CycleMasterVolume;
		sfxVolumeButton.Pressed += CycleSfxVolume;
		musicVolumeButton.Pressed += CycleMusicVolume;
		muteAudioButton.Pressed += ToggleMuteAudio;
		closeButton.Pressed += Close;

		if (settingsManager != null)
		{
			settingsManager.SettingsChanged += RefreshLabels;
		}
		else
		{
			GD.PushWarning("SettingsPanel could not find the SettingsManager autoload.");
		}

		if (audioManager != null)
		{
			audioManager.AudioSettingsChanged += RefreshLabels;
		}
		else
		{
			GD.PushWarning("SettingsPanel could not find the AudioManager autoload.");
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

		if (masterVolumeButton != null)
		{
			masterVolumeButton.Pressed -= CycleMasterVolume;
		}

		if (sfxVolumeButton != null)
		{
			sfxVolumeButton.Pressed -= CycleSfxVolume;
		}

		if (musicVolumeButton != null)
		{
			musicVolumeButton.Pressed -= CycleMusicVolume;
		}

		if (muteAudioButton != null)
		{
			muteAudioButton.Pressed -= ToggleMuteAudio;
		}

		if (closeButton != null)
		{
			closeButton.Pressed -= Close;
		}

		if (settingsManager != null)
		{
			settingsManager.SettingsChanged -= RefreshLabels;
		}

		if (audioManager != null)
		{
			audioManager.AudioSettingsChanged -= RefreshLabels;
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

	private void CycleMasterVolume()
	{
		audioManager?.CycleMasterVolume();
	}

	private void CycleSfxVolume()
	{
		audioManager?.CycleSfxVolume();
	}

	private void CycleMusicVolume()
	{
		audioManager?.CycleMusicVolume();
	}

	private void ToggleMuteAudio()
	{
		audioManager?.ToggleMute();
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
			masterVolumeButton.Text = "Master Volume: 75%";
			sfxVolumeButton.Text = "SFX Volume: 75%";
			musicVolumeButton.Text = "Music Volume: 50%";
			muteAudioButton.Text = "Mute Audio: Off";
			return;
		}

		feedbackButton.Text = settingsManager.GetFeedbackIntensityLabel();
		cameraShakeButton.Text = settingsManager.GetCameraShakeLabel();
		cameraShakeStrengthButton.Text = settingsManager.GetCameraShakeStrengthLabel();
		debugStatsButton.Text = settingsManager.GetDebugStatsLabel();
		fullscreenButton.Text = settingsManager.GetFullscreenLabel();
		vsyncButton.Text = settingsManager.GetVSyncLabel();
		masterVolumeButton.Text = audioManager?.GetMasterVolumeLabel() ?? "Master Volume: 75%";
		sfxVolumeButton.Text = audioManager?.GetSfxVolumeLabel() ?? "SFX Volume: 75%";
		musicVolumeButton.Text = audioManager?.GetMusicVolumeLabel() ?? "Music Volume: 50%";
		muteAudioButton.Text = audioManager?.GetMuteLabel() ?? "Mute Audio: Off";
	}
}
