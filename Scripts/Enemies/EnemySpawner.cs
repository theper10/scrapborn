using Godot;
using System.Collections.Generic;

public partial class EnemySpawner : Node2D
{
	[Signal]
	public delegate void EnemySpawnedEventHandler(Enemy enemy);

	private const string SpawnCrawlerTestAction = "SpawnCrawlerTest";
	private const string SpawnFastTestAction = "SpawnFastTest";
	private const string SpawnTankTestAction = "SpawnTankTest";

	private readonly Dictionary<EnemyType, PackedScene> enemyScenes = new();
	private Node2D enemiesRoot;

	[Export]
	private float spawnRadius = 560f;

	public override void _Ready()
	{
		EnsureTestSpawnInputActions();
		enemiesRoot = GetNodeOrNull<Node2D>("../Enemies");
		CacheEnemyScenes();
	}

	public override void _Process(double delta)
	{
		RunManager runManager = GetNodeOrNull<RunManager>("/root/RunManager");
		if (runManager != null && runManager.IsRunOver)
		{
			return;
		}

		if (Input.IsActionJustPressed(SpawnCrawlerTestAction))
		{
			SpawnEnemy(EnemyType.Crawler);
		}
		else if (Input.IsActionJustPressed(SpawnFastTestAction))
		{
			SpawnEnemy(EnemyType.Fast);
		}
		else if (Input.IsActionJustPressed(SpawnTankTestAction))
		{
			SpawnEnemy(EnemyType.Tank);
		}
	}

	public Enemy SpawnEnemy(EnemyType enemyType)
	{
		if (enemiesRoot == null ||
		    !enemyScenes.TryGetValue(enemyType, out PackedScene scene) ||
		    scene == null)
		{
			GD.PushWarning($"Could not spawn {EnemyDefinitions.Get(enemyType).DisplayName}.");
			return null;
		}

		Enemy enemy = scene.Instantiate<Enemy>();
		enemy.GlobalPosition = GetSpawnPosition();
		enemiesRoot.AddChild(enemy);
		EmitSignal(SignalName.EnemySpawned, enemy);
		return enemy;
	}

	private Vector2 GetSpawnPosition()
	{
		float angle = (float)GD.RandRange(0.0, Mathf.Tau);
		return GlobalPosition + Vector2.FromAngle(angle) * spawnRadius;
	}

	private void CacheEnemyScenes()
	{
		foreach (EnemyType enemyType in System.Enum.GetValues<EnemyType>())
		{
			enemyScenes[enemyType] = EnemyDefinitions.LoadScene(enemyType);
		}
	}

	private static void EnsureTestSpawnInputActions()
	{
		EnsureActionHasKey(SpawnCrawlerTestAction, Key.F1);
		EnsureActionHasKey(SpawnFastTestAction, Key.F2);
		EnsureActionHasKey(SpawnTankTestAction, Key.F3);
	}

	private static void EnsureActionHasKey(string action, Key key)
	{
		if (!InputMap.HasAction(action))
		{
			InputMap.AddAction(action);
		}

		foreach (InputEvent inputEvent in InputMap.ActionGetEvents(action))
		{
			if (inputEvent is InputEventKey keyEvent &&
			    (keyEvent.PhysicalKeycode == key || keyEvent.Keycode == key))
			{
				return;
			}
		}

		InputMap.ActionAddEvent(action, new InputEventKey
		{
			PhysicalKeycode = key
		});
	}
}
