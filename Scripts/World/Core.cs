using Godot;
using System;

public partial class Core : Node2D, IInspectable
{
	[Signal]
	public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);

	[Export]
	private int maxHealth = 250;

	private UpgradeManager upgradeManager;
	private RunManager runManager;
	private Line2D selectionHighlight;
	private int baseMaxHealth;
	private double damageFlashTimer;
	private double regenCarry;

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
		runManager = GetNodeOrNull<RunManager>("/root/RunManager");
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
		UpdateDayRegen(delta);
		UpdateDamageFlash(delta);
	}

	public void TakeDamage(int amount)
	{
		if (amount <= 0 || CurrentHealth <= 0)
		{
			return;
		}

		int previousHealth = CurrentHealth;
		int adjustedAmount = Mathf.Max(1, Mathf.CeilToInt(amount * (upgradeManager?.CoreDamageTakenMultiplier ?? 1f)));
		CurrentHealth = Math.Clamp(CurrentHealth - adjustedAmount, 0, maxHealth);
		int damageTaken = previousHealth - CurrentHealth;
		damageFlashTimer = 0.12;
		Modulate = new Color(1f, 0.35f, 0.35f, 1f);
		FeedbackEffects.PlaySfx(this, "core_damage");
		FeedbackEffects.SpawnText(
			this,
			GlobalPosition,
			$"-{damageTaken} HP",
			FeedbackEffects.DamageColor,
			FeedbackCategory.Critical,
			0.08f,
			$"{GetInstanceId()}:damage");
		FeedbackEffects.ShakeCamera(this, 8f, 0.25f);
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
		int repaired = CurrentHealth - previousHealth;
		if (repaired > 0)
		{
			PulseRepairVisual();
			FeedbackEffects.SpawnText(
				this,
				GlobalPosition,
				$"Repaired +{repaired}",
				FeedbackEffects.RepairColor,
				FeedbackCategory.Repair,
				0.08f,
				$"{GetInstanceId()}:repair");
		}

		return repaired;
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

	public string GetHoverText()
	{
		return $"Core\nHP: {CurrentHealth} / {maxHealth}\nStatus: {GetCoreStatus()}";
	}

	private void OnUpgradeApplied(int upgradeType)
	{
		UpgradeType appliedUpgrade = (UpgradeType)upgradeType;
		if (appliedUpgrade is not (UpgradeType.CoreMaxHealth or UpgradeType.CoreMaxHealthLarge))
		{
			return;
		}

		int previousMaxHealth = maxHealth;
		maxHealth = baseMaxHealth + (upgradeManager?.CoreMaxHealthBonus ?? 0);
		CurrentHealth = Math.Clamp(CurrentHealth + maxHealth - previousMaxHealth, 0, maxHealth);
		EmitSignal(SignalName.HealthChanged, CurrentHealth, maxHealth);
	}

	private void UpdateDayRegen(double delta)
	{
		float regenPerSecond = upgradeManager?.CoreDayRegenPerSecond ?? 0f;
		if (regenPerSecond <= 0f ||
		    runManager?.CurrentPhase != RunPhase.Day ||
		    CurrentHealth <= 0 ||
		    CurrentHealth >= maxHealth)
		{
			regenCarry = 0.0;
			return;
		}

		regenCarry += delta * regenPerSecond;
		int healAmount = Mathf.FloorToInt((float)regenCarry);
		if (healAmount <= 0)
		{
			return;
		}

		regenCarry -= healAmount;
		CurrentHealth = Math.Clamp(CurrentHealth + healAmount, 0, maxHealth);
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

	private void PulseRepairVisual()
	{
		damageFlashTimer = 0.0;
		Modulate = new Color(0.55f, 1f, 0.65f, 1f);
		Tween tween = CreateTween();
		tween.TweenProperty(this, "modulate", Colors.White, 0.18);
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
