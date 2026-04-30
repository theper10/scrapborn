using Godot;
using System;

public partial class ScrapDeposit : Node2D
{
	[Export]
	private int startingScrap = 100;

	public int AvailableScrap { get; private set; }
	public int StartingScrap => startingScrap;

	public override void _Ready()
	{
		AvailableScrap = Math.Max(0, startingScrap);
	}
}
