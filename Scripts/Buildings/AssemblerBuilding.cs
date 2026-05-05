using Godot;
using System.Collections.Generic;

public partial class AssemblerBuilding : Building
{
	[Export]
	private int scrapInput = 2;

	[Export]
	private int energyInput = 1;

	[Export]
	private int ammoOutput = 3;

	[Export]
	private float productionInterval = 2.5f;

	private double productionTimer;

	public override void _Process(double delta)
	{
		if (IsDestroyed)
		{
			return;
		}

		productionTimer += delta;
		if (productionTimer < GetEffectiveProductionInterval())
		{
			return;
		}

		productionTimer = 0.0;
		TryAssembleAmmo();
	}

	private void TryAssembleAmmo()
	{
		if (ResourceManager == null)
		{
			SetStatus(BuildingStatus.InvalidPlacement);
			FeedbackEffects.SpawnText(
				this,
				GlobalPosition,
				"Invalid",
				FeedbackEffects.WarningColor,
				FeedbackCategory.Error,
				1.5f,
				$"{GetInstanceId()}:invalid");
			return;
		}

		int effectiveAmmoOutput = GetEffectiveAmmoOutput();
		if (ResourceManager.GetAvailableSpace(ResourceType.Ammo) < effectiveAmmoOutput)
		{
			SetStatus(BuildingStatus.OutputFull);
			FeedbackEffects.PlaySfx(this, "error");
			FeedbackEffects.SpawnText(
				this,
				GlobalPosition,
				"Ammo Full",
				FeedbackEffects.WarningColor,
				FeedbackCategory.Error,
				1.5f,
				$"{GetInstanceId()}:ammo-full");
			return;
		}

		Dictionary<ResourceType, int> cost = new()
		{
			{ ResourceType.Scrap, scrapInput },
			{ ResourceType.Energy, energyInput }
		};

		if (!ResourceManager.CanSpend(cost))
		{
			SetStatus(BuildingStatus.MissingInput);
			FeedbackEffects.PlaySfx(this, "error");
			FeedbackEffects.SpawnText(
				this,
				GlobalPosition,
				"Missing Input",
				FeedbackEffects.WarningColor,
				FeedbackCategory.Error,
				1.5f,
				$"{GetInstanceId()}:missing-input");
			return;
		}

		ResourceManager.Spend(cost);
		int producedAmmo = ResourceManager.AddResource(ResourceType.Ammo, effectiveAmmoOutput);
		GetNodeOrNull<RunManager>("/root/RunManager")?.RecordAmmoProduced(producedAmmo);
		FeedbackEffects.PlaySfx(this, "assembler");
		FeedbackEffects.SpawnText(
			this,
			GlobalPosition,
			$"-{scrapInput} Scrap",
			FeedbackEffects.SpendColor,
			FeedbackCategory.Production,
			0.05f,
			$"{GetInstanceId()}:spend-scrap",
			new Vector2(-30f, 0f));
		FeedbackEffects.SpawnText(
			this,
			GlobalPosition,
			$"-{energyInput} Energy",
			FeedbackEffects.SpendColor,
			FeedbackCategory.Production,
			0.05f,
			$"{GetInstanceId()}:spend-energy",
			new Vector2(30f, 0f));
		if (producedAmmo > 0)
		{
			FeedbackEffects.SpawnText(
				this,
				GlobalPosition,
				$"+{producedAmmo} Ammo",
				FeedbackEffects.AmmoGainColor,
				FeedbackCategory.Production,
				0.05f,
				$"{GetInstanceId()}:ammo");
		}

		PulseFeedbackVisual(new Color(0.78f, 1f, 0.52f, 1f));
		SetStatus(BuildingStatus.Working);
	}

	protected override string GetInspectionDetails()
	{
		return
			$"Converts: {scrapInput} Scrap + {energyInput} Energy -> {GetEffectiveAmmoOutput()} Ammo\n" +
			$"Interval: {GetEffectiveProductionInterval():0.##}s";
	}

	private float GetEffectiveProductionInterval()
	{
		float speedMultiplier = (UpgradeManager?.AssemblerSpeedMultiplier ?? 1f) *
			(UpgradeManager?.ProductionSpeedMultiplier ?? 1f);
		return productionInterval / speedMultiplier;
	}

	private int GetEffectiveAmmoOutput()
	{
		return Mathf.Max(1, ammoOutput + (UpgradeManager?.AssemblerAmmoOutputBonus ?? 0));
	}
}
