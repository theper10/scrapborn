using Godot;

public partial class DrillBuilding : ResourceProducerBuilding
{
	[Export]
	private float depositWorkRange = 96f;

	protected override bool CanProduce()
	{
		return base.CanProduce() && FindNearestValidScrapDeposit() != null;
	}

	protected override void TryProduce()
	{
		if (ResourceManager == null)
		{
			SetStatus(BuildingStatus.InvalidPlacement);
			return;
		}

		ScrapDeposit deposit = FindNearestValidScrapDeposit();
		if (deposit == null)
		{
			SetStatus(BuildingStatus.InvalidPlacement);
			FeedbackEffects.SpawnText(
				this,
				GlobalPosition,
				"Needs Scrap Deposit",
				FeedbackEffects.WarningColor,
				FeedbackCategory.Error,
				1.5f,
				$"{GetInstanceId()}:invalid");
			return;
		}

		if (ResourceManager.IsFull(ResourceType.Scrap))
		{
			SetStatus(BuildingStatus.OutputFull);
			FeedbackEffects.PlaySfx(this, "error");
			FeedbackEffects.SpawnText(
				this,
				GlobalPosition,
				"Scrap Full",
				FeedbackEffects.WarningColor,
				FeedbackCategory.Error,
				1.5f,
				$"{GetInstanceId()}:full");
			return;
		}

		int amountToExtract = Mathf.Min(GetEffectiveOutputAmount(), ResourceManager.GetAvailableSpace(ResourceType.Scrap));
		int extractedAmount = deposit.Extract(amountToExtract);
		if (extractedAmount <= 0)
		{
			SetStatus(BuildingStatus.InvalidPlacement);
			return;
		}

		int producedAmount = ResourceManager.AddResource(ResourceType.Scrap, extractedAmount);
		if (producedAmount <= 0)
		{
			SetStatus(BuildingStatus.OutputFull);
			return;
		}

		GetNodeOrNull<RunManager>("/root/RunManager")?.RecordScrapProducedByDrill(producedAmount);
		FeedbackEffects.PlaySfx(this, "drill");
		FeedbackEffects.SpawnText(
			this,
			GlobalPosition,
			$"+{producedAmount} Scrap",
			FeedbackEffects.ScrapGainColor,
			FeedbackCategory.Production,
			0.05f,
			$"{GetInstanceId()}:produce");
		PulseFeedbackVisual(new Color(1f, 0.86f, 0.48f, 1f));
		SetStatus(BuildingStatus.Working);
	}

	protected override string GetInspectionDetails()
	{
		return
			$"Produces: {OutputType}\n" +
			$"Rate: +{GetEffectiveOutputAmount()} every {FormatSeconds(GetEffectiveProductionInterval())}\n" +
			$"Requires: near Scrap deposit\n" +
			$"Nearest deposit: {GetNearestDepositText()}";
	}

	private ScrapDeposit FindNearestValidScrapDeposit()
	{
		Node world = GetParent()?.GetParent();
		if (world == null)
		{
			return null;
		}

		ScrapDeposit closestDeposit = null;
		float rangeSquared = depositWorkRange * depositWorkRange;
		float closestDistanceSquared = float.MaxValue;
		foreach (Node node in GetTree().GetNodesInGroup("ScrapDeposits"))
		{
			if (node is not ScrapDeposit deposit || !IsInstanceValid(deposit) || deposit.IsEmpty)
			{
				continue;
			}

			float distanceSquared = GlobalPosition.DistanceSquaredTo(deposit.GlobalPosition);
			if (distanceSquared <= rangeSquared && distanceSquared < closestDistanceSquared)
			{
				closestDistanceSquared = distanceSquared;
				closestDeposit = deposit;
			}
		}

		return closestDeposit;
	}

	private string GetNearestDepositText()
	{
		ScrapDeposit deposit = FindNearestValidScrapDeposit();
		if (deposit == null)
		{
			return "None";
		}

		return $"{deposit.CurrentAmount} / {deposit.StartingAmount}";
	}
}
