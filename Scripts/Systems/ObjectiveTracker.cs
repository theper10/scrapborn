using Godot;

public partial class ObjectiveTracker : Node
{
	[Signal]
	public delegate void ObjectiveChangedEventHandler(string objectiveText, bool isVisible);

	private ResourceManager resourceManager;
	private RunManager runManager;
	private int currentObjectiveIndex = -2;
	private bool currentVisibility;

	private readonly string[] objectives =
	{
		"Gather Scrap",
		"Build a Generator",
		"Build an Assembler",
		"Produce Ammo",
		"Build a Turret",
		"Survive Night 1",
		"Choose an Upgrade",
		"Survive until Victory"
	};

	public string CurrentObjectiveText => currentObjectiveIndex >= 0 && currentObjectiveIndex < objectives.Length
		? objectives[currentObjectiveIndex]
		: string.Empty;
	public bool IsObjectiveVisible => currentVisibility;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		resourceManager = GetNodeOrNull<ResourceManager>("/root/ResourceManager");
		runManager = GetNodeOrNull<RunManager>("/root/RunManager");

		if (resourceManager != null)
		{
			resourceManager.ResourcesChanged += RefreshObjective;
		}

		if (runManager != null)
		{
			runManager.RunStateChanged += OnRunStateChanged;
		}

		RefreshObjective();
	}

	public override void _Process(double delta)
	{
		RefreshObjective();
	}

	public override void _ExitTree()
	{
		if (resourceManager != null)
		{
			resourceManager.ResourcesChanged -= RefreshObjective;
		}

		if (runManager != null)
		{
			runManager.RunStateChanged -= OnRunStateChanged;
		}
	}

	private void OnRunStateChanged(string phaseText, string detailText, string messageText, bool isRunOver)
	{
		RefreshObjective();
	}

	private void RefreshObjective()
	{
		int nextIndex = GetCurrentObjectiveIndex();
		bool nextVisibility = ShouldShowObjective(nextIndex);

		if (nextIndex == currentObjectiveIndex && nextVisibility == currentVisibility)
		{
			return;
		}

		currentObjectiveIndex = nextIndex;
		currentVisibility = nextVisibility;

		string objectiveText = nextIndex >= 0 && nextIndex < objectives.Length
			? objectives[nextIndex]
			: string.Empty;
		EmitSignal(SignalName.ObjectiveChanged, objectiveText, nextVisibility);
	}

	private int GetCurrentObjectiveIndex()
	{
		if (runManager == null)
		{
			return -1;
		}

		RunStats stats = runManager.Stats;
		if (stats.ScrapGatheredManually <= 0)
		{
			return 0;
		}

		if (!HasActiveBuilding(BuildingType.Generator) && stats.EnergyProduced <= 0)
		{
			return 1;
		}

		if (!HasActiveBuilding(BuildingType.Assembler) && stats.AmmoProduced <= 0)
		{
			return 2;
		}

		if (stats.AmmoProduced <= 0 && (resourceManager?.GetAmount(ResourceType.Ammo) ?? 0) <= 0)
		{
			return 3;
		}

		if (!HasActiveBuilding(BuildingType.Turret))
		{
			return 4;
		}

		if (stats.NightsSurvived < 1)
		{
			return 5;
		}

		if (stats.UpgradesChosen < 1)
		{
			return 6;
		}

		if (runManager.CurrentPhase != RunPhase.Victory)
		{
			return 7;
		}

		return -1;
	}

	private bool ShouldShowObjective(int objectiveIndex)
	{
		return objectiveIndex >= 0 &&
		       runManager != null &&
		       !GetTree().Paused &&
		       runManager.CurrentPhase is RunPhase.Day or RunPhase.Night &&
		       !runManager.IsRunOver;
	}

	private bool HasActiveBuilding(BuildingType buildingType)
	{
		foreach (Node node in GetTree().GetNodesInGroup("Buildings"))
		{
			if (node is Building building &&
			    IsInstanceValid(building) &&
			    building.BuildingType == buildingType &&
			    !building.IsDestroyed)
			{
				return true;
			}
		}

		return false;
	}
}
