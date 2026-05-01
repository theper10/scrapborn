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
	private double damageFlashTimer;

	public int CurrentHealth { get; private set; }
	public int MaxHealth => maxHealth;
	public bool IsDestroyed => CurrentHealth <= 0;
	public bool NeedsRepair => !IsDestroyed && CurrentHealth < maxHealth;

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

	public override void _Process(double delta)
	{
		UpdateDamageFlash(delta);
	}

	public void TakeDamage(int amount)
	{
		if (amount <= 0 || CurrentHealth <= 0)
		{
			return;
		}

		CurrentHealth = Math.Clamp(CurrentHealth - amount, 0, maxHealth);
		damageFlashTimer = 0.12;
		Modulate = new Color(1f, 0.35f, 0.35f, 1f);
		EmitSignal(SignalName.HealthChanged, CurrentHealth, maxHealth);
	}

	public int Repair(int amount)
	{
		if (amount <= 0 || CurrentHealth <= 0 || CurrentHealth >= maxHealth)
		{
			return 0;
		}

		int previousHealth = CurrentHealth;
		CurrentHealth = Math.Clamp(CurrentHealth + amount, 0, maxHealth);
		EmitSignal(SignalName.HealthChanged, CurrentHealth, maxHealth);
		return CurrentHealth - previousHealth;
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

	private void UpdateDamageFlash(double delta)
	{
		if (damageFlashTimer <= 0.0)
		{
			return;
		}

		damageFlashTimer -= delta;
		if (damageFlashTimer <= 0.0)
		{
			Modulate = Colors.White;
		}
	}
}
