using Godot;

public static class FeedbackEffects
{
	public static readonly Color ScrapGainColor = new(0.95f, 0.82f, 0.42f, 1f);
	public static readonly Color EnergyGainColor = new(0.42f, 0.9f, 1f, 1f);
	public static readonly Color AmmoGainColor = new(0.8f, 0.95f, 0.45f, 1f);
	public static readonly Color SpendColor = new(1f, 0.62f, 0.42f, 1f);
	public static readonly Color DamageColor = new(1f, 0.36f, 0.32f, 1f);
	public static readonly Color RepairColor = new(0.48f, 1f, 0.62f, 1f);
	public static readonly Color WarningColor = new(1f, 0.88f, 0.42f, 1f);

	public static FeedbackIntensity CurrentIntensity { get; private set; } = FeedbackIntensity.Medium;

	public static void SpawnText(
		Node source,
		Vector2 worldPosition,
		string message,
		Color color,
		float cooldownSeconds = 0f,
		string throttleKey = "",
		Vector2? screenOffset = null)
	{
		SpawnText(source, worldPosition, message, color, FeedbackCategory.Status, cooldownSeconds, throttleKey, screenOffset);
	}

	public static void SpawnText(
		Node source,
		Vector2 worldPosition,
		string message,
		Color color,
		FeedbackCategory category,
		float cooldownSeconds = 0f,
		string throttleKey = "",
		Vector2? screenOffset = null)
	{
		FeedbackIntensity intensity = GetFeedbackIntensity(source);
		if (!ShouldShowFloatingText(category, intensity))
		{
			return;
		}

		GetFloatingTextSpawner(source)?.SpawnWorldText(
			worldPosition,
			message,
			color,
			GetEffectiveCooldown(category, cooldownSeconds, intensity),
			throttleKey,
			screenOffset,
			GetTextScale(category));
	}

	public static void ShakeCamera(Node source, float strength, float duration)
	{
		if (strength <= 0f || duration <= 0f)
		{
			return;
		}

		GetCameraShake(source)?.Shake(strength, duration);
	}

	public static void PlaySfx(Node source, string sfxName)
	{
		source?.GetNodeOrNull<AudioManager>("/root/AudioManager")?.PlaySfx(sfxName);
	}

	public static void CycleIntensity()
	{
		CurrentIntensity = CurrentIntensity switch
		{
			FeedbackIntensity.High => FeedbackIntensity.Medium,
			FeedbackIntensity.Medium => FeedbackIntensity.Low,
			FeedbackIntensity.Low => FeedbackIntensity.Off,
			_ => FeedbackIntensity.High
		};
	}

	public static string GetIntensityLabel()
	{
		return $"Feedback: {CurrentIntensity}";
	}

	private static FeedbackIntensity GetFeedbackIntensity(Node source)
	{
		SettingsManager settingsManager = GetSettingsManager(source);
		return settingsManager?.FeedbackIntensity ?? CurrentIntensity;
	}

	private static bool ShouldShowFloatingText(FeedbackCategory category, FeedbackIntensity intensity)
	{
		return intensity switch
		{
			FeedbackIntensity.High => true,
			FeedbackIntensity.Medium => true,
			FeedbackIntensity.Low => category is
				FeedbackCategory.CombatDamage or
				FeedbackCategory.Repair or
				FeedbackCategory.Error or
				FeedbackCategory.Critical,
			FeedbackIntensity.Off => false,
			_ => true
		};
	}

	private static float GetEffectiveCooldown(FeedbackCategory category, float requestedCooldown, FeedbackIntensity intensity)
	{
		if (category != FeedbackCategory.Production)
		{
			return requestedCooldown;
		}

		return intensity switch
		{
			FeedbackIntensity.High => Mathf.Max(requestedCooldown, 1f),
			FeedbackIntensity.Medium => Mathf.Max(requestedCooldown, 3f),
			_ => requestedCooldown
		};
	}

	private static float GetTextScale(FeedbackCategory category)
	{
		return category switch
		{
			FeedbackCategory.Production => 0.78f,
			FeedbackCategory.Gathering => 0.86f,
			FeedbackCategory.Status => 0.9f,
			FeedbackCategory.Error => 0.95f,
			_ => 1f
		};
	}

	private static FloatingTextSpawner GetFloatingTextSpawner(Node source)
	{
		return source?.GetTree()?.Root.FindChild("FloatingTextSpawner", true, false) as FloatingTextSpawner;
	}

	private static SettingsManager GetSettingsManager(Node source)
	{
		return source?.GetNodeOrNull<SettingsManager>("/root/SettingsManager");
	}

	private static CameraShake GetCameraShake(Node source)
	{
		return source?.GetTree()?.Root.FindChild("Camera2D", true, false) as CameraShake;
	}
}
