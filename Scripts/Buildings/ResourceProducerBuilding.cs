using Godot;

public partial class ResourceProducerBuilding : Building
{
	[Export]
	private ResourceType outputType = ResourceType.Scrap;

	[Export]
	private int outputAmount = 2;

	[Export]
	private float productionInterval = 2f;

	private double productionTimer;

	protected virtual ResourceType OutputType => outputType;
	protected int OutputAmount => outputAmount;
	protected float ProductionInterval => productionInterval;

	public override void _Process(double delta)
	{
		if (IsDestroyed)
		{
			return;
		}

		productionTimer += delta;
		if (productionTimer < productionInterval)
		{
			return;
		}

		productionTimer = 0.0;
		TryProduce();
	}

	protected virtual bool CanProduce()
	{
		return ResourceManager != null;
	}

	protected virtual void TryProduce()
	{
		if (!CanProduce())
		{
			SetStatus(BuildingStatus.InvalidPlacement);
			return;
		}

		if (ResourceManager.IsFull(OutputType))
		{
			SetStatus(BuildingStatus.OutputFull);
			return;
		}

		int producedAmount = ResourceManager.AddResource(OutputType, GetEffectiveOutputAmount());
		RecordProducedAmount(producedAmount);
		SetStatus(BuildingStatus.Working);
	}

	protected override string GetInspectionDetails()
	{
		return
			$"Produces: {OutputType}\n" +
			$"Rate: +{GetEffectiveOutputAmount()} every {FormatSeconds(productionInterval)}";
	}

	protected int GetEffectiveOutputAmount()
	{
		float multiplier = BuildingType switch
		{
			BuildingType.Drill => UpgradeManager?.DrillProductionMultiplier ?? 1f,
			BuildingType.Generator => UpgradeManager?.GeneratorProductionMultiplier ?? 1f,
			_ => 1f
		};

		return Mathf.Max(1, Mathf.RoundToInt(outputAmount * multiplier));
	}

	protected static string FormatSeconds(float seconds)
	{
		return $"{seconds:0.##}s";
	}

	private void RecordProducedAmount(int amount)
	{
		if (amount <= 0)
		{
			return;
		}

		RunManager runManager = GetNodeOrNull<RunManager>("/root/RunManager");
		if (runManager == null)
		{
			return;
		}

		if (BuildingType == BuildingType.Drill && OutputType == ResourceType.Scrap)
		{
			runManager.RecordScrapProducedByDrill(amount);
		}
		else if (BuildingType == BuildingType.Generator && OutputType == ResourceType.Energy)
		{
			runManager.RecordEnergyProduced(amount);
		}
	}
}
