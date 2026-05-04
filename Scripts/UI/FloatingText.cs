using Godot;

public partial class FloatingText : Label
{
	[Export]
	private float lifetime = 0.8f;

	[Export]
	private float riseDistance = 30f;

	private Color startColor = Colors.White;
	private Vector2 startPosition;
	private double elapsed;

	public override void _Ready()
	{
		startPosition = Position;
		startColor = Modulate;
	}

	public override void _Process(double delta)
	{
		elapsed += delta;
		float progress = lifetime <= 0f ? 1f : Mathf.Clamp((float)(elapsed / lifetime), 0f, 1f);
		Position = startPosition + new Vector2(0f, -riseDistance * progress);
		Modulate = new Color(startColor.R, startColor.G, startColor.B, 1f - progress);

		if (progress >= 1f)
		{
			QueueFree();
		}
	}

	public void Initialize(string message, Color color, float duration = 0.8f, float textScale = 1f)
	{
		Text = message;
		startColor = color;
		Modulate = color;
		lifetime = duration;
		Scale = Vector2.One * textScale;
		startPosition = Position;
		elapsed = 0.0;
	}
}
