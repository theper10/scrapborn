using Godot;

public partial class Main : Node2D
{
	public override void _Ready()
	{
		GD.Print("Scrapborn foundation loaded.");
		GetNodeOrNull<RunManager>("/root/RunManager")?.BindCurrentScene(this);
	}
}
