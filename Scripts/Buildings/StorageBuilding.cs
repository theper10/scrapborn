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
	private int appliedScrapBonus;
	private int appliedEnergyBonus;
	private int appliedAmmoBonus;

	public override void _Ready()
	{
		base._Ready();
		if (UpgradeManager != null)
		{
			UpgradeManager.UpgradeApplied += OnUpgradeApplied;
		}

		ApplyStorageBonus();
	}

	public override void _ExitTree()
	{
		if (UpgradeManager != null)
		{
			UpgradeManager.UpgradeApplied -= OnUpgradeApplied;
		}
	}

	private void ApplyStorageBonus()
	{
		if (hasAppliedBonus || ResourceManager == null)
		{
			return;
		}

		hasAppliedBonus = true;
		appliedScrapBonus = GetEffectiveBonus(scrapMaxBonus);
		appliedEnergyBonus = GetEffectiveBonus(energyMaxBonus);
		appliedAmmoBonus = GetEffectiveBonus(ammoMaxBonus);
		ResourceManager.AddMaxResources(new Dictionary<ResourceType, int>
		{
			{ ResourceType.Scrap, appliedScrapBonus },
			{ ResourceType.Energy, appliedEnergyBonus },
			{ ResourceType.Ammo, appliedAmmoBonus }
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
		RemoveStorageBonus();
	}

	protected override void OnSold()
	{
		RemoveStorageBonus();
	}

	protected override string GetInspectionDetails()
	{
		return
			$"Capacity bonus: +{GetEffectiveBonus(scrapMaxBonus)} Scrap, +{GetEffectiveBonus(energyMaxBonus)} Energy, +{GetEffectiveBonus(ammoMaxBonus)} Ammo";
	}

	private void RemoveStorageBonus()
	{
		if (!hasAppliedBonus || ResourceManager == null)
		{
			return;
		}

		hasAppliedBonus = false;
		ResourceManager.AddMaxResources(new Dictionary<ResourceType, int>
		{
			{ ResourceType.Scrap, -appliedScrapBonus },
			{ ResourceType.Energy, -appliedEnergyBonus },
			{ ResourceType.Ammo, -appliedAmmoBonus }
		});
		appliedScrapBonus = 0;
		appliedEnergyBonus = 0;
		appliedAmmoBonus = 0;
	}

	private void OnUpgradeApplied(int upgradeType)
	{
		if ((UpgradeType)upgradeType != UpgradeType.StorageCapacityBonus || IsDestroyed || ResourceManager == null)
		{
			return;
		}

		RemoveStorageBonus();
		ApplyStorageBonus();
	}

	private int GetEffectiveBonus(int baseBonus)
	{
		float multiplier = UpgradeManager?.StorageCapacityMultiplier ?? 1f;
		return Mathf.Max(0, Mathf.RoundToInt(baseBonus * multiplier));
	}
}
