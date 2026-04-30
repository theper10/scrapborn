using Godot;
using System;

public partial class ScrapDeposit : Area2D
{
	[Export]
	public int StartingAmount { get; set; } = 100;

	public int CurrentAmount { get; private set; }
	public bool IsEmpty => CurrentAmount <= 0;

	public override void _Ready()
	{
		CurrentAmount = Math.Max(0, StartingAmount);
		UpdateVisualState();
	}

	public int Gather(int amount)
	{
		if (amount <= 0 || IsEmpty)
		{
			return 0;
		}

		int gatheredAmount = Math.Min(amount, CurrentAmount);
		CurrentAmount -= gatheredAmount;
		UpdateVisualState();
		return gatheredAmount;
	}

	private void UpdateVisualState()
	{
		Visible = !IsEmpty;
		Monitorable = !IsEmpty;
	}
}
