using Godot;
using System.Collections.Generic;

public partial class ScrapDepositSpawner : Node2D
{
	private const string ScrapDepositScenePath = "res://Scenes/Resources/ScrapDeposit.tscn";

	[Export]
	private int maxActiveDeposits = 8;

	[Export]
	private int minActiveDeposits = 5;

	[Export]
	private int depositStartingAmount = 1000;

	[Export]
	private float minRespawnDelay = 10f;

	[Export]
	private float maxRespawnDelay = 20f;

	[Export]
	private float emergencyRespawnDelay = 3f;

	[Export]
	private float minSpawnDistance = 128f;

	private readonly RandomNumberGenerator random = new();
	private PackedScene scrapDepositScene;
	private double respawnTimer = -1.0;

	public override void _Ready()
	{
		random.Randomize();
		scrapDepositScene = ResourceLoader.Load<PackedScene>(ScrapDepositScenePath);
	}

	public override void _Process(double delta)
	{
		int activeDepositCount = GetActiveDepositCount();
		if (activeDepositCount >= minActiveDeposits || activeDepositCount >= maxActiveDeposits)
		{
			respawnTimer = -1.0;
			return;
		}

		if (respawnTimer < 0.0)
		{
			respawnTimer = activeDepositCount <= 0
				? emergencyRespawnDelay
				: random.RandfRange(minRespawnDelay, maxRespawnDelay);
		}

		respawnTimer -= delta;
		if (respawnTimer > 0.0)
		{
			return;
		}

		TrySpawnDeposit();
		respawnTimer = GetActiveDepositCount() <= 0
			? emergencyRespawnDelay
			: random.RandfRange(minRespawnDelay, maxRespawnDelay);
	}

	private bool TrySpawnDeposit()
	{
		if (scrapDepositScene == null || GetActiveDepositCount() >= maxActiveDeposits)
		{
			return false;
		}

		List<Marker2D> spawnPoints = GetSpawnPoints();
		while (spawnPoints.Count > 0)
		{
			int index = random.RandiRange(0, spawnPoints.Count - 1);
			Marker2D spawnPoint = spawnPoints[index];
			spawnPoints.RemoveAt(index);

			if (!IsSpawnPositionClear(spawnPoint.GlobalPosition))
			{
				continue;
			}

			ScrapDeposit deposit = scrapDepositScene.Instantiate<ScrapDeposit>();
			deposit.StartingAmount = depositStartingAmount;
			Node parent = GetParent();
			if (parent == null)
			{
				deposit.QueueFree();
				return false;
			}

			parent.AddChild(deposit);
			deposit.GlobalPosition = spawnPoint.GlobalPosition;
			FeedbackEffects.SpawnText(
				this,
				deposit.GlobalPosition,
				"New Scrap Deposit",
				FeedbackEffects.WarningColor,
				FeedbackCategory.Status,
				1.0f,
				$"{GetInstanceId()}:spawn");
			return true;
		}

		return false;
	}

	private int GetActiveDepositCount()
	{
		int count = 0;
		foreach (Node node in GetTree().GetNodesInGroup("ScrapDeposits"))
		{
			if (node is ScrapDeposit deposit &&
			    IsInstanceValid(deposit) &&
			    !deposit.IsEmpty)
			{
				count++;
			}
		}

		return count;
	}

	private List<Marker2D> GetSpawnPoints()
	{
		List<Marker2D> spawnPoints = new();
		foreach (Node child in GetChildren())
		{
			if (child is Marker2D marker)
			{
				spawnPoints.Add(marker);
			}
		}

		return spawnPoints;
	}

	private bool IsSpawnPositionClear(Vector2 position)
	{
		float minDistanceSquared = minSpawnDistance * minSpawnDistance;
		foreach (Node node in GetTree().GetNodesInGroup("ScrapDeposits"))
		{
			if (node is ScrapDeposit deposit &&
			    IsInstanceValid(deposit) &&
			    !deposit.IsEmpty &&
			    deposit.GlobalPosition.DistanceSquaredTo(position) < minDistanceSquared)
			{
				return false;
			}
		}

		foreach (Node node in GetTree().GetNodesInGroup("Buildings"))
		{
			if (node is Building building &&
			    IsInstanceValid(building) &&
			    !building.IsDestroyed &&
			    building.GlobalPosition.DistanceSquaredTo(position) < minDistanceSquared)
			{
				return false;
			}
		}

		Core core = GetTree().Root.FindChild("Core", true, false) as Core;
		return core == null || core.GlobalPosition.DistanceSquaredTo(position) >= minDistanceSquared;
	}
}
