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

	public int CurrentHealth { get; private set; }
	public int MaxHealth => maxHealth;

	public override void _Ready()
	{
		EnsureMovementActions();
		CurrentHealth = maxHealth;
		EmitSignal(SignalName.HealthChanged, CurrentHealth, maxHealth);
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 inputDirection = Input.GetVector(MoveLeftAction, MoveRightAction, MoveUpAction, MoveDownAction);
		Velocity = inputDirection * moveSpeed;
		MoveAndSlide();
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
