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
			return;
		}

		if (ResourceManager.GetAvailableSpace(ResourceType.Ammo) < ammoOutput)
		{
			SetStatus(BuildingStatus.OutputFull);
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
			return;
		}

		ResourceManager.Spend(cost);
		int producedAmmo = ResourceManager.AddResource(ResourceType.Ammo, ammoOutput);
		GetNodeOrNull<RunManager>("/root/RunManager")?.RecordAmmoProduced(producedAmmo);
		SetStatus(BuildingStatus.Working);
	}

	protected override string GetInspectionDetails()
	{
		return
			$"Converts: {scrapInput} Scrap + {energyInput} Energy -> {ammoOutput} Ammo\n" +
			$"Interval: {GetEffectiveProductionInterval():0.##}s";
	}

	private float GetEffectiveProductionInterval()
	{
		float speedMultiplier = UpgradeManager?.AssemblerSpeedMultiplier ?? 1f;
		return productionInterval / speedMultiplier;
	}
}
