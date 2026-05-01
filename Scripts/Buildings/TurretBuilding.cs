using Godot;
using System.Collections.Generic;

public partial class TurretBuilding : Building
{
	[Export]
	private float range = 220f;

	[Export]
	private int damage = 10;

	[Export]
	private float fireInterval = 0.5f;

	[Export]
	private int ammoCost = 1;

	private Line2D shotLine;
	private double fireTimer;
	private double shotFlashTimer;

	public override void _Ready()
	{
		base._Ready();
		shotLine = GetNodeOrNull<Line2D>("ShotLine");
		if (shotLine != null)
		{
			shotLine.Visible = false;
		}
	}

	public override void _Process(double delta)
	{
		if (IsDestroyed)
		{
			if (shotLine != null)
			{
				shotLine.Visible = false;
			}

			return;
		}

		UpdateShotFlash(delta);

		if (fireTimer > 0.0)
		{
			fireTimer -= delta;
		}

		Enemy target = FindNearestEnemyInRange();
		if (target == null)
		{
			SetStatus(BuildingStatus.Idle);
			return;
		}

		if (ResourceManager == null || ResourceManager.GetAmount(ResourceType.Ammo) < ammoCost)
		{
			SetStatus(BuildingStatus.MissingInput);
			return;
		}

		if (fireTimer > 0.0)
		{
			return;
		}

		Dictionary<ResourceType, int> shotCost = new()
		{
			{ ResourceType.Ammo, ammoCost }
		};

		if (!ResourceManager.Spend(shotCost))
		{
			SetStatus(BuildingStatus.MissingInput);
			return;
		}

		target.TakeDamage(GetEffectiveDamage());
		ShowShot(target.GlobalPosition);
		fireTimer = GetEffectiveFireInterval();
		SetStatus(BuildingStatus.Working);
	}

	private Enemy FindNearestEnemyInRange()
	{
		Enemy nearestEnemy = null;
		float nearestDistanceSquared = range * range;

		foreach (Node node in GetTree().GetNodesInGroup("Enemies"))
		{
			if (node is not Enemy enemy || !IsInstanceValid(enemy) || enemy.CurrentHealth <= 0)
			{
				continue;
			}

			float distanceSquared = GlobalPosition.DistanceSquaredTo(enemy.GlobalPosition);
			if (distanceSquared <= nearestDistanceSquared)
			{
				nearestDistanceSquared = distanceSquared;
				nearestEnemy = enemy;
			}
		}

		return nearestEnemy;
	}

	private void ShowShot(Vector2 targetPosition)
	{
		if (shotLine == null)
		{
			return;
		}

		shotLine.Points = new[]
		{
			Vector2.Zero,
			ToLocal(targetPosition)
		};
		shotLine.Visible = true;
		shotFlashTimer = 0.08;
	}

	private void UpdateShotFlash(double delta)
	{
		if (shotLine == null || shotFlashTimer <= 0.0)
		{
			return;
		}

		shotFlashTimer -= delta;
		if (shotFlashTimer <= 0.0)
		{
			shotLine.Visible = false;
		}
	}

	protected override string GetInspectionDetails()
	{
		return
			$"Damage: {GetEffectiveDamage()}\n" +
			$"Fire interval: {GetEffectiveFireInterval():0.##}s\n" +
			$"Range: {range:0} px\n" +
			$"Ammo cost: {ammoCost}";
	}

	private int GetEffectiveDamage()
	{
		float damageMultiplier = UpgradeManager?.TurretDamageMultiplier ?? 1f;
		return Mathf.Max(1, Mathf.RoundToInt(damage * damageMultiplier));
	}

	private float GetEffectiveFireInterval()
	{
		float fireRateMultiplier = UpgradeManager?.TurretFireRateMultiplier ?? 1f;
		return fireInterval / fireRateMultiplier;
	}
}
