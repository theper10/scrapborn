using Godot;
using System.Collections.Generic;

public partial class FloatingTextSpawner : CanvasLayer
{
	private const string FloatingTextScenePath = "res://Scenes/UI/FloatingText.tscn";

	private readonly Dictionary<string, double> lastSpawnTimes = new();
	private PackedScene floatingTextScene;
	private int staggerIndex;

	[Export]
	private int maxActiveTexts = 34;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		Layer = 30;
		floatingTextScene = ResourceLoader.Load<PackedScene>(FloatingTextScenePath);
	}

	public void SpawnWorldText(
		Vector2 worldPosition,
		string message,
		Color color,
		float cooldownSeconds = 0f,
		string throttleKey = "",
		Vector2? screenOffset = null,
		float textScale = 1f)
	{
		Vector2 screenPosition = GetViewport().GetCanvasTransform() * worldPosition;
		SpawnUiText(screenPosition, message, color, cooldownSeconds, throttleKey, screenOffset, textScale);
	}

	public void SpawnUiText(
		Vector2 uiPosition,
		string message,
		Color color,
		float cooldownSeconds = 0f,
		string throttleKey = "",
		Vector2? screenOffset = null,
		float textScale = 1f)
	{
		if (floatingTextScene == null ||
		    string.IsNullOrWhiteSpace(message) ||
		    IsThrottled(throttleKey, cooldownSeconds))
		{
			return;
		}

		if (GetChildCount() >= maxActiveTexts)
		{
			GetChild(0).QueueFree();
		}

		FloatingText floatingText = floatingTextScene.Instantiate<FloatingText>();
		AddChild(floatingText);
		floatingText.Position = uiPosition + GetStaggerOffset() + (screenOffset ?? Vector2.Zero);
		floatingText.Initialize(message, color, 0.8f, textScale);
	}

	private bool IsThrottled(string throttleKey, float cooldownSeconds)
	{
		if (string.IsNullOrEmpty(throttleKey) || cooldownSeconds <= 0f)
		{
			return false;
		}

		double now = Time.GetTicksMsec() / 1000.0;
		if (lastSpawnTimes.TryGetValue(throttleKey, out double lastTime) &&
		    now - lastTime < cooldownSeconds)
		{
			return true;
		}

		lastSpawnTimes[throttleKey] = now;
		return false;
	}

	private Vector2 GetStaggerOffset()
	{
		int column = staggerIndex % 5;
		int row = (staggerIndex / 5) % 3;
		staggerIndex = (staggerIndex + 1) % 15;
		return new Vector2((column - 2) * 10f, -row * 8f);
	}
}
