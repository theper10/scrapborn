using Godot;
using System;

public partial class PlayerController : CharacterBody2D
{
	[Signal]
	public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);

	private const string MoveUpAction = "MoveUp";
	private const string MoveDownAction = "MoveDown";
	private const string MoveLeftAction = "MoveLeft";
	private const string MoveRightAction = "MoveRight";

	[Export]
	private float moveSpeed = 220f;

	[Export]
	private int maxHealth = 100;

	private UpgradeManager upgradeManager;
	private RunManager runManager;
	private float baseMoveSpeed;
	private int baseMaxHealth;
	private double damageFlashTimer;

	public int CurrentHealth { get; private set; }
	public int MaxHealth => maxHealth;

	public override void _Ready()
	{
		EnsureMovementActions();
		baseMoveSpeed = moveSpeed;
		baseMaxHealth = maxHealth;
		upgradeManager = GetNodeOrNull<UpgradeManager>("/root/UpgradeManager");
		runManager = GetNodeOrNull<RunManager>("/root/RunManager");
		if (upgradeManager != null)
		{
			upgradeManager.UpgradeApplied += OnUpgradeApplied;
			maxHealth = baseMaxHealth + upgradeManager.PlayerMaxHealthBonus;
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

	public override void _PhysicsProcess(double delta)
	{
		if (CurrentHealth <= 0 || runManager?.IsRunOver == true)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		Vector2 inputDirection = Input.GetVector(MoveLeftAction, MoveRightAction, MoveUpAction, MoveDownAction);
		Velocity = inputDirection * GetEffectiveMoveSpeed();
		MoveAndSlide();
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

		int previousHealth = CurrentHealth;
		CurrentHealth = Math.Clamp(CurrentHealth - amount, 0, maxHealth);
		int damageTaken = previousHealth - CurrentHealth;
		damageFlashTimer = 0.1;
		Modulate = new Color(1f, 0.35f, 0.35f, 1f);
		FeedbackEffects.SpawnText(
			this,
			GlobalPosition,
			$"-{damageTaken} HP",
			FeedbackEffects.DamageColor,
			FeedbackCategory.Critical,
			0.08f,
			$"{GetInstanceId()}:damage");
		FeedbackEffects.ShakeCamera(this, 5f, 0.18f);
		EmitSignal(SignalName.HealthChanged, CurrentHealth, maxHealth);

		if (CurrentHealth <= 0)
		{
			runManager?.EnterDefeat("Player destroyed.");
		}
	}

	private float GetEffectiveMoveSpeed()
	{
		return baseMoveSpeed * (upgradeManager?.PlayerMoveSpeedMultiplier ?? 1f);
	}

	private void OnUpgradeApplied(int upgradeType)
	{
		if ((UpgradeType)upgradeType != UpgradeType.PlayerMaxHealth)
		{
			return;
		}

		int previousMaxHealth = maxHealth;
		maxHealth = baseMaxHealth + (upgradeManager?.PlayerMaxHealthBonus ?? 0);
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

	private static void EnsureMovementActions()
	{
		EnsureActionHasKeys(MoveUpAction, Key.W, Key.Up);
		EnsureActionHasKeys(MoveDownAction, Key.S, Key.Down);
		EnsureActionHasKeys(MoveLeftAction, Key.A, Key.Left);
		EnsureActionHasKeys(MoveRightAction, Key.D, Key.Right);
	}

	private static void EnsureActionHasKeys(string action, params Key[] keys)
	{
		if (!InputMap.HasAction(action))
		{
			InputMap.AddAction(action);
		}

		foreach (Key key in keys)
		{
			if (ActionHasKey(action, key))
			{
				continue;
			}

			InputMap.ActionAddEvent(action, new InputEventKey
			{
				PhysicalKeycode = key
			});
		}
	}

	private static bool ActionHasKey(string action, Key key)
	{
		foreach (InputEvent inputEvent in InputMap.ActionGetEvents(action))
		{
			if (inputEvent is InputEventKey keyEvent &&
			    (keyEvent.PhysicalKeycode == key || keyEvent.Keycode == key))
			{
				return true;
			}
		}

		return false;
	}
}
