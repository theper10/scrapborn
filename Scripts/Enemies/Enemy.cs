using Godot;
using System;

public partial class Enemy : CharacterBody2D
{
	[Signal]
	public delegate void DiedEventHandler(Enemy enemy);

	[Export]
	private EnemyType enemyType = EnemyType.Crawler;

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
	private PlayerController player;
	private ProgressBar healthBar;
	private double attackTimer;
	private double hitFlashTimer;
	private int currentHealth;
	private bool isDying;

	public int CurrentHealth => currentHealth;
	public int MaxHealth => maxHealth;
	public EnemyType EnemyType => enemyType;

	public override void _Ready()
	{
		AddToGroup("Enemies");
		currentHealth = maxHealth;
		core = GetNodeOrNull<Core>("../Core") ?? GetTree().Root.FindChild("Core", true, false) as Core;
		player = GetTree().Root.FindChild("Player", true, false) as PlayerController;
		healthBar = GetNodeOrNull<ProgressBar>("HealthBar");
		UpdateHealthBar();
	}

	public override void _PhysicsProcess(double delta)
	{
		UpdateHitFlash(delta);

		if (currentHealth <= 0 || isDying)
		{
			Velocity = Vector2.Zero;
			return;
		}

		Node2D attackTarget = FindAttackTarget();
		if (attackTarget != null)
		{
			Velocity = Vector2.Zero;
			AttackTarget(attackTarget, delta);
			return;
		}

		if (core == null || core.CurrentHealth <= 0)
		{
			Velocity = Vector2.Zero;
			return;
		}

		Vector2 direction = GlobalPosition.DirectionTo(core.GlobalPosition);
		Velocity = direction * moveSpeed;
		MoveAndSlide();
	}

	public void TakeDamage(int amount)
	{
		if (amount <= 0 || currentHealth <= 0 || isDying)
		{
			return;
		}

		int previousHealth = currentHealth;
		currentHealth = Math.Clamp(currentHealth - amount, 0, maxHealth);
		int damageTaken = previousHealth - currentHealth;
		hitFlashTimer = 0.08;
		Modulate = new Color(1f, 0.35f, 0.35f, 1f);
		FeedbackEffects.PlaySfx(this, "enemy_hit");
		FeedbackEffects.SpawnText(
			this,
			GlobalPosition,
			$"-{damageTaken}",
			FeedbackEffects.DamageColor,
			FeedbackCategory.CombatDamage,
			0.05f,
			$"{GetInstanceId()}:hit");
		UpdateHealthBar();

		if (currentHealth <= 0)
		{
			Die();
		}
	}

	private void Die()
	{
		isDying = true;
		Velocity = Vector2.Zero;
		CollisionLayer = 0;
		CollisionMask = 0;
		if (healthBar != null)
		{
			healthBar.Visible = false;
		}

		EmitSignal(SignalName.Died, this);
		FeedbackEffects.PlaySfx(this, "enemy_death");
		FeedbackEffects.SpawnText(
			this,
			GlobalPosition,
			"Destroyed",
			FeedbackEffects.AmmoGainColor,
			FeedbackCategory.CombatDamage,
			0.05f,
			$"{GetInstanceId()}:dead");

		Tween tween = CreateTween();
		tween.TweenProperty(this, "scale", Scale * 1.35f, 0.14);
		tween.Parallel().TweenProperty(this, "modulate", new Color(1f, 0.9f, 0.45f, 0f), 0.14);
		tween.TweenCallback(Callable.From(QueueFree));
	}

	private Node2D FindAttackTarget()
	{
		if (IsPlayerAttackable())
		{
			return player;
		}

		Building building = FindClosestAttackableBuilding();
		if (building != null)
		{
			return building;
		}

		if (core != null && core.CurrentHealth > 0 && GlobalPosition.DistanceTo(core.GlobalPosition) <= attackRange)
		{
			return core;
		}

		return null;
	}

	private bool IsPlayerAttackable()
	{
		return player != null &&
		       IsInstanceValid(player) &&
		       player.CurrentHealth > 0 &&
		       GlobalPosition.DistanceTo(player.GlobalPosition) <= attackRange;
	}

	private Building FindClosestAttackableBuilding()
	{
		Building closestBuilding = null;
		float closestDistanceSquared = attackRange * attackRange;

		foreach (Node node in GetTree().GetNodesInGroup("Buildings"))
		{
			if (node is not Building building ||
			    !IsInstanceValid(building) ||
			    building.IsDestroyed)
			{
				continue;
			}

			float distanceSquared = GlobalPosition.DistanceSquaredTo(building.GlobalPosition);
			if (distanceSquared <= closestDistanceSquared)
			{
				closestDistanceSquared = distanceSquared;
				closestBuilding = building;
			}
		}

		return closestBuilding;
	}

	private void AttackTarget(Node2D target, double delta)
	{
		attackTimer -= delta;
		if (attackTimer > 0.0)
		{
			return;
		}

		attackTimer = attackInterval;
		if (target is PlayerController playerTarget)
		{
			playerTarget.TakeDamage(contactDamage);
		}
		else if (target is Building buildingTarget)
		{
			buildingTarget.TakeDamage(contactDamage);
		}
		else if (target is Core coreTarget)
		{
			coreTarget.TakeDamage(contactDamage);
		}
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
