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
			FeedbackEffects.SpawnText(
				this,
				GlobalPosition,
				BuildingType == BuildingType.Drill ? "Needs Scrap Deposit" : "Invalid",
				FeedbackEffects.WarningColor,
				FeedbackCategory.Error,
				1.5f,
				$"{GetInstanceId()}:invalid");
			return;
		}

		if (ResourceManager.IsFull(OutputType))
		{
			SetStatus(BuildingStatus.OutputFull);
			FeedbackEffects.SpawnText(
				this,
				GlobalPosition,
				"Full",
				FeedbackEffects.WarningColor,
				FeedbackCategory.Error,
				1.5f,
				$"{GetInstanceId()}:full");
			return;
		}

		int producedAmount = ResourceManager.AddResource(OutputType, GetEffectiveOutputAmount());
		RecordProducedAmount(producedAmount);
		if (producedAmount > 0)
		{
			FeedbackEffects.SpawnText(
				this,
				GlobalPosition,
				$"+{producedAmount} {OutputType}",
				GetResourceFeedbackColor(OutputType),
				FeedbackCategory.Production,
				0.05f,
				$"{GetInstanceId()}:produce");
			PulseFeedbackVisual(GetResourcePulseColor(OutputType));
		}

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

	private static Color GetResourceFeedbackColor(ResourceType resourceType)
	{
		return resourceType switch
		{
			ResourceType.Scrap => FeedbackEffects.ScrapGainColor,
			ResourceType.Energy => FeedbackEffects.EnergyGainColor,
			ResourceType.Ammo => FeedbackEffects.AmmoGainColor,
			_ => Colors.White
		};
	}

	private static Color GetResourcePulseColor(ResourceType resourceType)
	{
		return resourceType switch
		{
			ResourceType.Scrap => new Color(1f, 0.86f, 0.48f, 1f),
			ResourceType.Energy => new Color(0.42f, 0.92f, 1f, 1f),
			ResourceType.Ammo => new Color(0.78f, 1f, 0.52f, 1f),
			_ => Colors.White
		};
	}
}
