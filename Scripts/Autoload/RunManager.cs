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

	[Signal]
	public delegate void RunAnnouncementEventHandler(string titleText, string detailText);

	private const string RestartRunAction = "RestartRun";
	private const string ReturnToMainMenuAction = "ReturnToMainMenu";
	private const string MainMenuScenePath = "res://Scenes/UI/MainMenu.tscn";

	private readonly Dictionary<int, EnemyType[]> waveDefinitions = new()
	{
		{ 1, BuildWave(4, 0, 0) },
		{ 2, BuildWave(8, 2, 0) },
		{ 3, BuildWave(10, 5, 1) },
		{ 4, BuildWave(10, 6, 3) },
		{ 5, BuildNightFiveWave() }
	};

	private EnemySpawner enemySpawner;
	private Core core;
	private UpgradeManager upgradeManager;
	private RunPhase currentPhase = RunPhase.Day;
	private int currentDay = 1;
	private int currentNight;
	private int pendingDay;
	private float dayTimer;
	private float spawnTimer;
	private EnemyType[] currentWave = Array.Empty<EnemyType>();
	private int spawnedThisNight;
	private int activeEnemies;
	private string defeatReason = "Core destroyed.";
	private bool isRunSceneBound;
	private bool finalPushAnnounced;
	private readonly RunStats stats = new();

	[Export]
	private float dayDuration = 90f;

	[Export]
	private float spawnInterval = 1f;

	[Export]
	private int maxNights = 5;

	public RunPhase CurrentPhase => currentPhase;
	public int CurrentDay => currentDay;
	public int CurrentNight => currentNight;
	public int ActiveEnemies => activeEnemies;
	public int SpawnedThisNight => spawnedThisNight;
	public int TotalEnemiesThisNight => currentWave.Length;
	public bool IsRunOver => IsRunOverInternal();
	public RunStats Stats => stats;

	public override void _Ready()
	{
		EnsureRestartInputAction();
		EnsureReturnToMainMenuInputAction();
		ProcessMode = ProcessModeEnum.Always;
		upgradeManager = GetNodeOrNull<UpgradeManager>("/root/UpgradeManager");
		if (upgradeManager != null)
		{
			upgradeManager.UpgradeApplied += OnUpgradeApplied;
		}

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
			else if (Input.IsActionJustPressed(ReturnToMainMenuAction))
			{
				ReturnToMainMenu();
			}

			return;
		}

		if (!isRunSceneBound)
		{
			return;
		}

		if (GetTree().Paused && currentPhase is RunPhase.Day or RunPhase.Night)
		{
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
		isRunSceneBound = true;
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
		EmitRunAnnouncement("Day 1", "Gather, build, prepare");
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
				spawnTimer = spawnInterval;
			}
		}

		if (spawnedThisNight >= currentWave.Length && activeEnemies <= 0)
		{
			stats.RecordNightSurvived();
			EmitRunAnnouncement("Wave cleared", $"Night {currentNight} survived");

			if (currentNight >= GetMaxNight())
			{
				StartVictory();
			}
			else
			{
				StartUpgradeSelection(currentNight + 1);
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
		EmitRunAnnouncement($"Day {day}", "Gather, build, prepare");
	}

	private void StartNight(int night)
	{
		currentPhase = RunPhase.Night;
		currentNight = night;
		currentWave = waveDefinitions[Mathf.Clamp(night, 1, waveDefinitions.Count)];
		spawnedThisNight = 0;
		spawnTimer = 0f;
		finalPushAnnounced = false;
		EmitRunState();
		EmitRunAnnouncement($"Night {night}", "Enemies incoming");
		FeedbackEffects.ShakeCamera(this, 2f, 0.15f);
	}

	private void StartUpgradeSelection(int nextDay)
	{
		currentPhase = RunPhase.UpgradeSelection;
		pendingDay = nextDay;
		EmitRunState();
		upgradeManager?.OfferUpgradeChoices();
		GetTree().Paused = true;
	}

	private void SpawnNextEnemy()
	{
		if (enemySpawner == null || spawnedThisNight >= currentWave.Length)
		{
			return;
		}

		if (currentNight == GetMaxNight() &&
		    !finalPushAnnounced &&
		    spawnedThisNight >= Math.Max(0, currentWave.Length - 2))
		{
			finalPushAnnounced = true;
			EmitRunAnnouncement("Final Push", "Last Tanks inbound");
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
		stats.RecordEnemyKilled();
		EmitRunState();
	}

	private void OnCoreHealthChanged(int currentHealth, int maxHealth)
	{
		if (currentHealth <= 0 && !IsRunOverInternal())
		{
			StartDefeat("Core destroyed.");
		}
	}

	private void OnUpgradeApplied(int upgradeType)
	{
		if (currentPhase != RunPhase.UpgradeSelection)
		{
			return;
		}

		GetTree().Paused = false;
		UpgradeDefinition upgrade = UpgradeDefinitions.Get((UpgradeType)upgradeType);
		stats.RecordUpgradeChosen(upgrade.DisplayName);
		StartDay(pendingDay);
	}

	private void StartVictory()
	{
		currentPhase = RunPhase.Victory;
		ClearEnemies();
		EmitRunState();
		EmitRunAnnouncement("Victory", "You survived the final night");
	}

	public void EnterDefeat(string reason)
	{
		if (IsRunOverInternal())
		{
			return;
		}

		StartDefeat(reason);
	}

	private void StartDefeat(string reason)
	{
		defeatReason = string.IsNullOrWhiteSpace(reason) ? "Run failed." : reason;
		currentPhase = RunPhase.Defeat;
		ClearEnemies();
		EmitRunState();
		EmitRunAnnouncement("Defeat", defeatReason);
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

	public void RestartRun()
	{
		GetTree().Paused = false;
		GetNodeOrNull<ResourceManager>("/root/ResourceManager")?.ResetResources();
		upgradeManager?.ResetUpgrades();
		isRunSceneBound = false;
		ResetRunState();
		GetTree().ReloadCurrentScene();
	}

	public void PrepareNewRun()
	{
		GetTree().Paused = false;
		ClearEnemies();
		GetNodeOrNull<ResourceManager>("/root/ResourceManager")?.ResetResources();
		upgradeManager?.ResetUpgrades();
		isRunSceneBound = false;
		enemySpawner = null;
		core = null;
		ResetRunState();
	}

	public void ReturnToMainMenu()
	{
		PrepareNewRun();
		GetTree().ChangeSceneToFile(MainMenuScenePath);
	}

	private void ResetRunState()
	{
		currentPhase = RunPhase.Day;
		currentDay = 1;
		currentNight = 0;
		pendingDay = 0;
		dayTimer = dayDuration;
		spawnTimer = 0f;
		currentWave = Array.Empty<EnemyType>();
		spawnedThisNight = 0;
		activeEnemies = 0;
		defeatReason = "Core destroyed.";
		finalPushAnnounced = false;
		stats.Reset();
		EmitRunState();
	}

	public void RecordBuildingPlaced()
	{
		stats.RecordBuildingPlaced();
	}

	public void RecordScrapGatheredManually(int amount)
	{
		stats.RecordScrapGatheredManually(amount);
	}

	public void RecordScrapProducedByDrill(int amount)
	{
		stats.RecordScrapProducedByDrill(amount);
	}

	public void RecordEnergyProduced(int amount)
	{
		stats.RecordEnergyProduced(amount);
	}

	public void RecordAmmoProduced(int amount)
	{
		stats.RecordAmmoProduced(amount);
	}

	public void RecordBuildingDestroyed()
	{
		stats.RecordBuildingDestroyed();
	}

	public void RecordRepair(int scrapSpent, int healthRepaired)
	{
		stats.RecordRepair(scrapSpent, healthRepaired);
	}

	public void RecordBuildingSold(Dictionary<ResourceType, int> refund)
	{
		stats.RecordBuildingSold(refund);
	}

	public void RecordDepositDepleted()
	{
		stats.RecordDepositDepleted();
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

	private void EmitRunAnnouncement(string titleText, string detailText)
	{
		EmitSignal(SignalName.RunAnnouncement, titleText, detailText);
	}

	private string GetPhaseText()
	{
		return currentPhase switch
		{
			RunPhase.Day => $"Day {currentDay}",
			RunPhase.Night => $"Night {currentNight}",
			RunPhase.UpgradeSelection => "Upgrade",
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
			RunPhase.UpgradeSelection => "Choose one upgrade",
			RunPhase.Victory => "You survived 5 nights.",
			RunPhase.Defeat => defeatReason,
			_ => string.Empty
		};
	}

	private int GetMaxNight()
	{
		return Mathf.Clamp(maxNights, 1, waveDefinitions.Count);
	}

	private string GetMessageText()
	{
		return currentPhase switch
		{
			RunPhase.Victory => "Victory! You survived 5 nights. Press R to restart or M for Main Menu.",
			RunPhase.Defeat => $"Defeat! {defeatReason} Press R to restart or M for Main Menu.",
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

	private static EnemyType[] BuildNightFiveWave()
	{
		List<EnemyType> enemies = new();

		for (int index = 0; index < 14; index++)
		{
			enemies.Add(EnemyType.Crawler);
		}

		for (int index = 0; index < 8; index++)
		{
			enemies.Add(EnemyType.Fast);
		}

		for (int index = 0; index < 3; index++)
		{
			enemies.Add(EnemyType.Tank);
		}

		enemies.Add(EnemyType.Tank);
		enemies.Add(EnemyType.Tank);
		return enemies.ToArray();
	}

	private static void EnsureRestartInputAction()
	{
		EnsureActionHasKey(RestartRunAction, Key.R);
	}

	private static void EnsureReturnToMainMenuInputAction()
	{
		EnsureActionHasKey(ReturnToMainMenuAction, Key.M);
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
