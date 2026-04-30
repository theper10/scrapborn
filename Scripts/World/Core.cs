using Godot;
using System;

public partial class Core : Node2D
{
	[Signal]
	public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);

	[Export]
	private int maxHealth = 250;

	private UpgradeManager upgradeManager;
	private int baseMaxHealth;

	public int CurrentHealth { get; private set; }
	public int MaxHealth => maxHealth;
	public bool IsDestroyed => CurrentHealth <= 0;

	public override void _Ready()
	{
		baseMaxHealth = maxHealth;
		upgradeManager = GetNodeOrNull<UpgradeManager>("/root/UpgradeManager");
		if (upgradeManager != null)
		{
			upgradeManager.UpgradeApplied += OnUpgradeApplied;
			maxHealth = baseMaxHealth + upgradeManager.CoreMaxHealthBonus;
		}

		CurrentHealth = maxHealth;
		EmitSignal(SignalName.HealthChanged, CurrentHealth, maxHealth);
	}

	public override void _ExitTree()
	{
		if (upgradeManager != null)
		{
			upgradeManager.UpgradeApplied -= OnUpgradeApplied;
		}
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

	private void OnUpgradeApplied(int upgradeType)
	{
		if ((UpgradeType)upgradeType != UpgradeType.CoreMaxHealth)
		{
			return;
		}

		int previousMaxHealth = maxHealth;
		maxHealth = baseMaxHealth + (upgradeManager?.CoreMaxHealthBonus ?? 0);
		CurrentHealth = Math.Clamp(CurrentHealth + maxHealth - previousMaxHealth, 0, maxHealth);
		EmitSignal(SignalName.HealthChanged, CurrentHealth, maxHealth);
	}
}
