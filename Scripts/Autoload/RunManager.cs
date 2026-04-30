using Godot;
using System;
using System.Collections.Generic;

public partial class RunManager : Node
{
	[Signal]
	public delegate void RunStateChangedEventHandler(
		string phaseText,
		string detailText,
		string messageText,
		bool isRunOver);

	private const string RestartRunAction = "RestartRun";
	private const int FinalNight = 5;
	private const float SpawnInterval = 1f;

	private readonly Dictionary<int, EnemyType[]> waveDefinitions = new()
	{
		{ 1, BuildWave(6, 0, 0) },
		{ 2, BuildWave(10, 2, 0) },
		{ 3, BuildWave(14, 5, 0) },
		{ 4, BuildWave(12, 6, 3) },
		{ 5, BuildWave(20, 8, 5) }
	};

	private EnemySpawner enemySpawner;
	private Core core;
	private RunPhase currentPhase = RunPhase.Day;
	private int currentDay = 1;
	private int currentNight;
	private float dayTimer;
	private float spawnTimer;
	private EnemyType[] currentWave = Array.Empty<EnemyType>();
	private int spawnedThisNight;
	private int activeEnemies;

	[Export]
	private float dayDuration = 90f;

	public RunPhase CurrentPhase => currentPhase;
	public int CurrentDay => currentDay;
	public int CurrentNight => currentNight;
	public int ActiveEnemies => activeEnemies;
	public int SpawnedThisNight => spawnedThisNight;
	public int TotalEnemiesThisNight => currentWave.Length;
	public bool IsRunOver => IsRunOverInternal();

	public override void _Ready()
	{
		EnsureRestartInputAction();
		ResetRunState();
	}

	public override void _Process(double delta)
	{
		if (IsRunOverInternal())
		{
			if (Input.IsActionJustPressed(RestartRunAction))
			{
				RestartRun();
			}

			return;
		}

		if (currentPhase == RunPhase.Day)
		{
			UpdateDay(delta);
		}
		else if (currentPhase == RunPhase.Night)
		{
			UpdateNight(delta);
		}
	}

	public void BindCurrentScene(Node sceneRoot)
	{
		if (enemySpawner != null && IsInstanceValid(enemySpawner))
		{
			enemySpawner.EnemySpawned -= OnEnemySpawned;
		}

		if (core != null && IsInstanceValid(core))
		{
			core.HealthChanged -= OnCoreHealthChanged;
		}

		enemySpawner = sceneRoot.GetNodeOrNull<EnemySpawner>("TestWorld/EnemySpawner");
		core = sceneRoot.GetNodeOrNull<Core>("TestWorld/Core");

		if (enemySpawner != null)
		{
			enemySpawner.EnemySpawned += OnEnemySpawned;
		}
		else
		{
			GD.PushWarning("RunManager could not find TestWorld/EnemySpawner.");
		}

		if (core != null)
		{
			core.HealthChanged += OnCoreHealthChanged;
			OnCoreHealthChanged(core.CurrentHealth, core.MaxHealth);
		}
		else
		{
			GD.PushWarning("RunManager could not find TestWorld/Core.");
		}

		EmitRunState();
	}

	private void UpdateDay(double delta)
	{
		dayTimer = Math.Max(0f, dayTimer - (float)delta);
		if (dayTimer <= 0f)
		{
			StartNight(currentDay);
			return;
		}

		EmitRunState();
	}

	private void UpdateNight(double delta)
	{
		if (spawnedThisNight < currentWave.Length)
		{
			spawnTimer -= (float)delta;
			if (spawnTimer <= 0f)
			{
				SpawnNextEnemy();
				spawnTimer = SpawnInterval;
			}
		}

		if (spawnedThisNight >= currentWave.Length && activeEnemies <= 0)
		{
			if (currentNight >= FinalNight)
			{
				StartVictory();
			}
			else
			{
				StartDay(currentNight + 1);
			}

			return;
		}

		EmitRunState();
	}

	private void StartDay(int day)
	{
		currentPhase = RunPhase.Day;
		currentDay = day;
		currentNight = 0;
		dayTimer = dayDuration;
		currentWave = Array.Empty<EnemyType>();
		spawnedThisNight = 0;
		spawnTimer = 0f;
		EmitRunState();
	}

	private void StartNight(int night)
	{
		currentPhase = RunPhase.Night;
		currentNight = night;
		currentWave = waveDefinitions[night];
		spawnedThisNight = 0;
		spawnTimer = 0f;
		EmitRunState();
	}

	private void SpawnNextEnemy()
	{
		if (enemySpawner == null || spawnedThisNight >= currentWave.Length)
		{
			return;
		}

		enemySpawner.SpawnEnemy(currentWave[spawnedThisNight]);
		spawnedThisNight++;
	}

	private void OnEnemySpawned(Enemy enemy)
	{
		if (enemy == null)
		{
			return;
		}

		activeEnemies++;
		enemy.Died += OnEnemyDied;
		EmitRunState();
	}

	private void OnEnemyDied(Enemy enemy)
	{
		if (enemy != null)
		{
			enemy.Died -= OnEnemyDied;
		}

		activeEnemies = Math.Max(0, activeEnemies - 1);
		EmitRunState();
	}

	private void OnCoreHealthChanged(int currentHealth, int maxHealth)
	{
		if (currentHealth <= 0 && !IsRunOverInternal())
		{
			StartDefeat();
		}
	}

	private void StartVictory()
	{
		currentPhase = RunPhase.Victory;
		ClearEnemies();
		EmitRunState();
	}

	private void StartDefeat()
	{
		currentPhase = RunPhase.Defeat;
		ClearEnemies();
		EmitRunState();
	}

	private void ClearEnemies()
	{
		foreach (Node node in GetTree().GetNodesInGroup("Enemies"))
		{
			if (node is Enemy enemy)
			{
				enemy.Died -= OnEnemyDied;
				enemy.QueueFree();
			}
		}

		activeEnemies = 0;
	}

	private void RestartRun()
	{
		GetNodeOrNull<ResourceManager>("/root/ResourceManager")?.ResetResources();
		ResetRunState();
		GetTree().ReloadCurrentScene();
	}

	private void ResetRunState()
	{
		currentPhase = RunPhase.Day;
		currentDay = 1;
		currentNight = 0;
		dayTimer = dayDuration;
		spawnTimer = 0f;
		currentWave = Array.Empty<EnemyType>();
		spawnedThisNight = 0;
		activeEnemies = 0;
		EmitRunState();
	}

	private bool IsRunOverInternal()
	{
		return currentPhase is RunPhase.Victory or RunPhase.Defeat;
	}

	private void EmitRunState()
	{
		EmitSignal(
			SignalName.RunStateChanged,
			GetPhaseText(),
			GetDetailText(),
			GetMessageText(),
			IsRunOverInternal());
	}

	private string GetPhaseText()
	{
		return currentPhase switch
		{
			RunPhase.Day => $"Day {currentDay}",
			RunPhase.Night => $"Night {currentNight}",
			RunPhase.Victory => "Victory",
			RunPhase.Defeat => "Defeat",
			_ => string.Empty
		};
	}

	private string GetDetailText()
	{
		return currentPhase switch
		{
			RunPhase.Day => $"{Mathf.CeilToInt(dayTimer)}s remaining",
			RunPhase.Night => $"Enemies: {activeEnemies} | Spawned: {spawnedThisNight}/{currentWave.Length}",
			RunPhase.Victory => "You survived 5 nights.",
			RunPhase.Defeat => "Core destroyed.",
			_ => string.Empty
		};
	}

	private string GetMessageText()
	{
		return currentPhase switch
		{
			RunPhase.Victory => "Victory! You survived 5 nights. Press R to restart.",
			RunPhase.Defeat => "Defeat! Core destroyed. Press R to restart.",
			_ => string.Empty
		};
	}

	private static EnemyType[] BuildWave(int crawlers, int fastEnemies, int tanks)
	{
		List<EnemyType> enemies = new();

		for (int index = 0; index < crawlers; index++)
		{
			enemies.Add(EnemyType.Crawler);
		}

		for (int index = 0; index < fastEnemies; index++)
		{
			enemies.Add(EnemyType.Fast);
		}

		for (int index = 0; index < tanks; index++)
		{
			enemies.Add(EnemyType.Tank);
		}

		return enemies.ToArray();
	}

	private static void EnsureRestartInputAction()
	{
		if (!InputMap.HasAction(RestartRunAction))
		{
			InputMap.AddAction(RestartRunAction);
		}

		foreach (InputEvent inputEvent in InputMap.ActionGetEvents(RestartRunAction))
		{
			if (inputEvent is InputEventKey keyEvent &&
			    (keyEvent.PhysicalKeycode == Key.R || keyEvent.Keycode == Key.R))
			{
				return;
			}
		}

		InputMap.ActionAddEvent(RestartRunAction, new InputEventKey
		{
			PhysicalKeycode = Key.R
		});
	}
}
