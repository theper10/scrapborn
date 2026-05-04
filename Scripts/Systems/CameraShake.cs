using Godot;

public partial class CameraShake : Camera2D
{
	private const string TestCameraShakeAction = "TestCameraShake";

	[Export]
	private float maxOffset = 14f;

	[Export]
	private bool shakeEnabled = true;

	private readonly RandomNumberGenerator random = new();
	private SettingsManager settingsManager;
	private Vector2 baseOffset;
	private float shakeStrength;
	private float shakeDuration;
	private float shakeRemaining;

	public override void _Ready()
	{
		EnsureTestCameraShakeInputAction();
		Enabled = true;
		MakeCurrent();
		settingsManager = GetNodeOrNull<SettingsManager>("/root/SettingsManager");
		baseOffset = Offset;
		random.Randomize();
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed(TestCameraShakeAction))
		{
			Shake(12f, 0.28f);
		}

		if (!CanShake())
		{
			shakeRemaining = 0f;
			shakeStrength = 0f;
			Offset = baseOffset;
			return;
		}

		if (shakeRemaining <= 0f)
		{
			shakeStrength = 0f;
			Offset = Offset.Lerp(baseOffset, 14f * (float)delta);
			return;
		}

		shakeRemaining = Mathf.Max(0f, shakeRemaining - (float)delta);
		float progress = shakeDuration <= 0f ? 0f : shakeRemaining / shakeDuration;
		float currentStrength = Mathf.Min(shakeStrength, maxOffset) * progress;
		Offset = baseOffset + new Vector2(
			random.RandfRange(-currentStrength, currentStrength),
			random.RandfRange(-currentStrength, currentStrength));
	}

	public override void _ExitTree()
	{
		Offset = baseOffset;
	}

	public void Shake(float strength, float duration)
	{
		if (!CanShake())
		{
			return;
		}

		float effectiveStrength = strength * (settingsManager?.CameraShakeStrengthMultiplier ?? 1f);
		shakeStrength = Mathf.Min(maxOffset, Mathf.Max(shakeStrength, effectiveStrength));
		shakeDuration = Mathf.Max(0.01f, duration);
		shakeRemaining = Mathf.Max(shakeRemaining, duration);
	}

	private bool CanShake()
	{
		return shakeEnabled && (settingsManager?.CameraShakeEnabled ?? true);
	}

	private static void EnsureTestCameraShakeInputAction()
	{
		if (!InputMap.HasAction(TestCameraShakeAction))
		{
			InputMap.AddAction(TestCameraShakeAction);
		}

		foreach (InputEvent inputEvent in InputMap.ActionGetEvents(TestCameraShakeAction))
		{
			if (inputEvent is InputEventKey keyEvent &&
			    (keyEvent.PhysicalKeycode == Key.F8 || keyEvent.Keycode == Key.F8))
			{
				InputMap.ActionEraseEvent(TestCameraShakeAction, inputEvent);
			}
		}

		foreach (InputEvent inputEvent in InputMap.ActionGetEvents(TestCameraShakeAction))
		{
			if (inputEvent is InputEventKey keyEvent &&
			    (keyEvent.PhysicalKeycode == Key.F6 || keyEvent.Keycode == Key.F6))
			{
				return;
			}
		}

		InputMap.ActionAddEvent(TestCameraShakeAction, new InputEventKey
		{
			PhysicalKeycode = Key.F6
		});
	}
}
