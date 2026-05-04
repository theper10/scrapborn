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
		FeedbackEffects.SpawnText(
			this,
			GlobalPosition,
			"+Capacity",
			FeedbackEffects.EnergyGainColor,
			FeedbackCategory.Status,
			0.2f,
			$"{GetInstanceId()}:capacity");
		PulseFeedbackVisual(new Color(0.48f, 0.92f, 1f, 1f));
		SetStatus(BuildingStatus.Idle);
	}

	protected override void OnDestroyed()
	{
		if (!hasAppliedBonus || ResourceManager == null)
		{
			return;
		}

		hasAppliedBonus = false;
		ResourceManager.AddMaxResources(new Dictionary<ResourceType, int>
		{
			{ ResourceType.Scrap, -scrapMaxBonus },
			{ ResourceType.Energy, -energyMaxBonus },
			{ ResourceType.Ammo, -ammoMaxBonus }
		});
	}

	protected override string GetInspectionDetails()
	{
		return
			$"Capacity bonus: +{scrapMaxBonus} Scrap, +{energyMaxBonus} Energy, +{ammoMaxBonus} Ammo";
	}
}
