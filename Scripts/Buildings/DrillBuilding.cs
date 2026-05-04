using Godot;

public partial class DrillBuilding : ResourceProducerBuilding
{
	[Export]
	private float depositWorkRange = 96f;

	protected override bool CanProduce()
	{
		return base.CanProduce() && IsNearValidScrapDeposit();
	}

	protected override void TryProduce()
	{
		if (!IsNearValidScrapDeposit())
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

		base.TryProduce();
	}

	protected override string GetInspectionDetails()
	{
		return
			$"Produces: {OutputType}\n" +
			$"Rate: +{GetEffectiveOutputAmount()} every {FormatSeconds(GetEffectiveProductionInterval())}\n" +
			"Requires: near Scrap deposit";
	}

	private bool IsNearValidScrapDeposit()
	{
		Node world = GetParent()?.GetParent();
		if (world == null)
		{
			return false;
		}

		float rangeSquared = depositWorkRange * depositWorkRange;
		foreach (Node child in world.GetChildren())
		{
			if (child is ScrapDeposit deposit &&
			    !deposit.IsEmpty &&
			    GlobalPosition.DistanceSquaredTo(deposit.GlobalPosition) <= rangeSquared)
			{
				return true;
			}
		}

		return false;
	}
}
