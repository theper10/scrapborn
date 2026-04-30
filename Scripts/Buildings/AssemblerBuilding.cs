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
		productionTimer += delta;
		if (productionTimer < productionInterval)
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
		ResourceManager.AddResource(ResourceType.Ammo, ammoOutput);
		SetStatus(BuildingStatus.Working);
	}
}
