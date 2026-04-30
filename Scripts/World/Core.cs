using Godot;
using System;

public partial class Core : Node2D
{
	[Signal]
	public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);

	[Export]
	private int maxHealth = 250;

	public int CurrentHealth { get; private set; }
	public int MaxHealth => maxHealth;
	public bool IsDestroyed => CurrentHealth <= 0;

	public override void _Ready()
	{
		CurrentHealth = maxHealth;
		EmitSignal(SignalName.HealthChanged, CurrentHealth, maxHealth);
	}

	public void TakeDamage(int amount)
	{
		if (amount <= 0 || CurrentHealth <= 0)
		{
			return;
		}

		CurrentHealth = Math.Clamp(CurrentHealth - amount, 0, maxHealth);
		EmitSignal(SignalName.HealthChanged, CurrentHealth, maxHealth);
	}
}
