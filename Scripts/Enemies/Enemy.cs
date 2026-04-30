using Godot;
using System;

public partial class Enemy : CharacterBody2D
{
	[Signal]
	public delegate void DiedEventHandler(Enemy enemy);

	[Export]
	private int maxHealth = 30;

	[Export]
	private float moveSpeed = 70f;

	[Export]
	private int contactDamage = 5;

	[Export]
	private float attackInterval = 1f;

	[Export]
	private float attackRange = 34f;

	private Core core;
	private ProgressBar healthBar;
	private double attackTimer;
	private double hitFlashTimer;
	private int currentHealth;

	public int CurrentHealth => currentHealth;
	public int MaxHealth => maxHealth;

	public override void _Ready()
	{
		AddToGroup("Enemies");
		currentHealth = maxHealth;
		core = GetNodeOrNull<Core>("../Core") ?? GetTree().Root.FindChild("Core", true, false) as Core;
		healthBar = GetNodeOrNull<ProgressBar>("HealthBar");
		UpdateHealthBar();
	}

	public override void _PhysicsProcess(double delta)
	{
		UpdateHitFlash(delta);

		if (core == null || core.CurrentHealth <= 0 || currentHealth <= 0)
		{
			Velocity = Vector2.Zero;
			return;
		}

		float distanceToCore = GlobalPosition.DistanceTo(core.GlobalPosition);
		if (distanceToCore <= attackRange)
		{
			Velocity = Vector2.Zero;
			AttackCore(delta);
			return;
		}

		Vector2 direction = GlobalPosition.DirectionTo(core.GlobalPosition);
		Velocity = direction * moveSpeed;
		MoveAndSlide();
	}

	public void TakeDamage(int amount)
	{
		if (amount <= 0 || currentHealth <= 0)
		{
			return;
		}

		currentHealth = Math.Clamp(currentHealth - amount, 0, maxHealth);
		hitFlashTimer = 0.08;
		Modulate = new Color(1f, 0.35f, 0.35f, 1f);
		UpdateHealthBar();

		if (currentHealth <= 0)
		{
			EmitSignal(SignalName.Died, this);
			QueueFree();
		}
	}

	private void AttackCore(double delta)
	{
		attackTimer -= delta;
		if (attackTimer > 0.0)
		{
			return;
		}

		attackTimer = attackInterval;
		core.TakeDamage(contactDamage);
	}

	private void UpdateHitFlash(double delta)
	{
		if (hitFlashTimer <= 0.0)
		{
			return;
		}

		hitFlashTimer -= delta;
		if (hitFlashTimer <= 0.0)
		{
			Modulate = Colors.White;
		}
	}

	private void UpdateHealthBar()
	{
		if (healthBar == null)
		{
			return;
		}

		healthBar.MaxValue = maxHealth;
		healthBar.Value = currentHealth;
	}
}
