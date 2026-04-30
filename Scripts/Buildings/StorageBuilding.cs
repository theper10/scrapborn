using Godot;
using System.Collections.Generic;

public partial class StorageBuilding : Building
{
	[Export]
	private int scrapMaxBonus = 100;

	[Export]
	private int energyMaxBonus = 50;

	[Export]
	private int ammoMaxBonus = 50;

	private bool hasAppliedBonus;

	public override void _Ready()
	{
		base._Ready();
		ApplyStorageBonus();
	}

	private void ApplyStorageBonus()
	{
		if (hasAppliedBonus || ResourceManager == null)
		{
			return;
		}

		hasAppliedBonus = true;
		ResourceManager.AddMaxResources(new Dictionary<ResourceType, int>
		{
			{ ResourceType.Scrap, scrapMaxBonus },
			{ ResourceType.Energy, energyMaxBonus },
			{ ResourceType.Ammo, ammoMaxBonus }
		});
		SetStatus(BuildingStatus.Idle);
	}
}
