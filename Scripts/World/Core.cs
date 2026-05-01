using Godot;
using System;

public partial class Core : Node2D, IInspectable
{
	[Signal]
	public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);

	[Export]
	private int maxHealth = 250;

	private UpgradeManager upgradeManager;
	private Line2D selectionHighlight;
	private int baseMaxHealth;
	private double damageFlashTimer;

	public string InspectableName => "Core";
	public int CurrentHealth { get; private set; }
	public int MaxHealth => maxHealth;
	public bool IsDestroyed => CurrentHealth <= 0;
	public bool NeedsRepair => !IsDestroyed && CurrentHealth < maxHealth;
	public bool IsSelectable => Visible;

	public override void _Ready()
	{
		CreateSelectionHighlight();
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

	public void SetSelected(bool selected)
	{
		if (selectionHighlight != null)
		{
			selectionHighlight.Visible = selected && IsSelectable;
		}
	}

	public string GetInspectionText()
	{
		string repairHint = NeedsRepair ? "\nRepair: Hold F nearby to repair" : string.Empty;
		return
			$"HP: {CurrentHealth} / {maxHealth}\n" +
			$"Status: {GetCoreStatus()}" +
			repairHint;
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

	private void CreateSelectionHighlight()
	{
		selectionHighlight = new Line2D
		{
			Name = "SelectionHighlight",
			ZIndex = 80,
			Width = 3f,
			DefaultColor = new Color(1f, 0.92f, 0.25f, 1f),
			Visible = false,
			Closed = true,
			Points = new[]
			{
				new Vector2(-42, -42),
				new Vector2(42, -42),
				new Vector2(42, 42),
				new Vector2(-42, 42)
			}
		};

		AddChild(selectionHighlight);
	}

	private string GetCoreStatus()
	{
		if (IsDestroyed)
		{
			return "Destroyed";
		}

		return NeedsRepair ? "Damaged" : "Stable";
	}
}
