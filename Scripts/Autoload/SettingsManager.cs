using Godot;

public partial class SettingsManager : Node
{
	[Signal]
	public delegate void SettingsChangedEventHandler();

	private readonly float[] cameraShakeStrengthOptions = { 0.5f, 1f, 1.5f };
	private int cameraShakeStrengthIndex = 1;

	public FeedbackIntensity FeedbackIntensity { get; private set; } = FeedbackIntensity.Medium;
	public bool CameraShakeEnabled { get; private set; } = true;
	public float CameraShakeStrengthMultiplier => cameraShakeStrengthOptions[cameraShakeStrengthIndex];
	public bool DebugStatsVisible { get; private set; }
	public bool Fullscreen { get; private set; }
	public bool VSyncEnabled { get; private set; }

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		Fullscreen = IsFullscreenMode(DisplayServer.WindowGetMode());
		VSyncEnabled = DisplayServer.WindowGetVsyncMode() != DisplayServer.VSyncMode.Disabled;
		EmitSignal(SignalName.SettingsChanged);
	}

	public void CycleFeedbackIntensity()
	{
		FeedbackIntensity = FeedbackIntensity switch
		{
			FeedbackIntensity.High => FeedbackIntensity.Medium,
			FeedbackIntensity.Medium => FeedbackIntensity.Low,
			FeedbackIntensity.Low => FeedbackIntensity.Off,
			_ => FeedbackIntensity.High
		};
		EmitSignal(SignalName.SettingsChanged);
	}

	public void ToggleCameraShake()
	{
		CameraShakeEnabled = !CameraShakeEnabled;
		EmitSignal(SignalName.SettingsChanged);
	}

	public void CycleCameraShakeStrength()
	{
		cameraShakeStrengthIndex = (cameraShakeStrengthIndex + 1) % cameraShakeStrengthOptions.Length;
		EmitSignal(SignalName.SettingsChanged);
	}

	public void ToggleDebugStats()
	{
		SetDebugStatsVisible(!DebugStatsVisible);
	}

	public void SetDebugStatsVisible(bool visible)
	{
		if (DebugStatsVisible == visible)
		{
			return;
		}

		DebugStatsVisible = visible;
		EmitSignal(SignalName.SettingsChanged);
	}

	public void ToggleFullscreen()
	{
		Fullscreen = !Fullscreen;
		DisplayServer.WindowSetMode(Fullscreen
			? DisplayServer.WindowMode.Fullscreen
			: DisplayServer.WindowMode.Windowed);
		EmitSignal(SignalName.SettingsChanged);
	}

	public void ToggleVSync()
	{
		VSyncEnabled = !VSyncEnabled;
		DisplayServer.WindowSetVsyncMode(VSyncEnabled
			? DisplayServer.VSyncMode.Enabled
			: DisplayServer.VSyncMode.Disabled);
		EmitSignal(SignalName.SettingsChanged);
	}

	public string GetFeedbackIntensityLabel()
	{
		return $"Feedback Intensity: {FeedbackIntensity}";
	}

	public string GetCameraShakeLabel()
	{
		return $"Camera Shake: {(CameraShakeEnabled ? "On" : "Off")}";
	}

	public string GetCameraShakeStrengthLabel()
	{
		return $"Camera Shake Strength: {Mathf.RoundToInt(CameraShakeStrengthMultiplier * 100f)}%";
	}

	public string GetDebugStatsLabel()
	{
		return $"Debug Stats: {(DebugStatsVisible ? "On" : "Off")}";
	}

	public string GetFullscreenLabel()
	{
		return $"Display: {(Fullscreen ? "Fullscreen" : "Windowed")}";
	}

	public string GetVSyncLabel()
	{
		return $"VSync: {(VSyncEnabled ? "On" : "Off")}";
	}

	private static bool IsFullscreenMode(DisplayServer.WindowMode mode)
	{
		return mode is DisplayServer.WindowMode.Fullscreen or DisplayServer.WindowMode.ExclusiveFullscreen;
	}
}
