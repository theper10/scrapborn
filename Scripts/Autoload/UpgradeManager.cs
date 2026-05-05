using Godot;
using System;
using System.Collections.Generic;

public partial class UpgradeManager : Node
{
	[Signal]
	public delegate void UpgradeChoicesChangedEventHandler(bool isVisible);

	[Signal]
	public delegate void UpgradeAppliedEventHandler(int upgradeType);

	private readonly List<UpgradeDefinition> currentChoices = new();
	private readonly List<string> chosenUpgradeNames = new();
	private readonly RandomNumberGenerator random = new();

	public IReadOnlyList<UpgradeDefinition> CurrentChoices => currentChoices;
	public IReadOnlyList<string> ChosenUpgradeNames => chosenUpgradeNames;

	public float PlayerMoveSpeedMultiplier { get; private set; } = 1f;
	public int PlayerMaxHealthBonus { get; private set; }
	public float PlayerRepairSpeedMultiplier { get; private set; } = 1f;
	public float PlayerRepairEfficiencyMultiplier { get; private set; } = 1f;
	public float PlayerGatherAmountMultiplier { get; private set; } = 1f;
	public float PlayerDamageTakenMultiplier { get; private set; } = 1f;
	public int CoreMaxHealthBonus { get; private set; }
	public float CoreDamageTakenMultiplier { get; private set; } = 1f;
	public float CoreDayRegenPerSecond { get; private set; }
	public float TurretFireRateMultiplier { get; private set; } = 1f;
	public float TurretDamageMultiplier { get; private set; } = 1f;
	public float TurretRangeMultiplier { get; private set; } = 1f;
	public float TurretAmmoSaveChance { get; private set; }
	public float TurretTankDamageMultiplier { get; private set; } = 1f;
	public float DrillProductionMultiplier { get; private set; } = 1f;
	public float GeneratorProductionMultiplier { get; private set; } = 1f;
	public float AssemblerSpeedMultiplier { get; private set; } = 1f;
	public int AssemblerAmmoOutputBonus { get; private set; }
	public float StorageCapacityMultiplier { get; private set; } = 1f;
	public float ProductionSpeedMultiplier { get; private set; } = 1f;
	public float BuildingCostMultiplier { get; private set; } = 1f;
	public float RepairCostMultiplier { get; private set; } = 1f;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		random.Randomize();
		ResetUpgrades();
	}

	public void ResetUpgrades()
	{
		currentChoices.Clear();
		chosenUpgradeNames.Clear();
		PlayerMoveSpeedMultiplier = 1f;
		PlayerMaxHealthBonus = 0;
		PlayerRepairSpeedMultiplier = 1f;
		PlayerRepairEfficiencyMultiplier = 1f;
		PlayerGatherAmountMultiplier = 1f;
		PlayerDamageTakenMultiplier = 1f;
		CoreMaxHealthBonus = 0;
		CoreDamageTakenMultiplier = 1f;
		CoreDayRegenPerSecond = 0f;
		TurretFireRateMultiplier = 1f;
		TurretDamageMultiplier = 1f;
		TurretRangeMultiplier = 1f;
		TurretAmmoSaveChance = 0f;
		TurretTankDamageMultiplier = 1f;
		DrillProductionMultiplier = 1f;
		GeneratorProductionMultiplier = 1f;
		AssemblerSpeedMultiplier = 1f;
		AssemblerAmmoOutputBonus = 0;
		StorageCapacityMultiplier = 1f;
		ProductionSpeedMultiplier = 1f;
		BuildingCostMultiplier = 1f;
		RepairCostMultiplier = 1f;
		EmitSignal(SignalName.UpgradeChoicesChanged, false);
	}

	public void OfferUpgradeChoices()
	{
		currentChoices.Clear();

		List<UpgradeDefinition> availableUpgrades = new(UpgradeDefinitions.All.Values);
		while (currentChoices.Count < 3 && availableUpgrades.Count > 0)
		{
			int index = ChooseWeightedUpgradeIndex(availableUpgrades);
			currentChoices.Add(availableUpgrades[index]);
			availableUpgrades.RemoveAt(index);
		}

		EmitSignal(SignalName.UpgradeChoicesChanged, true);
	}

	public void SelectUpgrade(int choiceIndex)
	{
		if (choiceIndex < 0 || choiceIndex >= currentChoices.Count)
		{
			return;
		}

		UpgradeDefinition upgrade = currentChoices[choiceIndex];
		UpgradeType upgradeType = upgrade.Type;
		ApplyUpgrade(upgradeType);
		chosenUpgradeNames.Add(upgrade.DisplayName);
		currentChoices.Clear();
		FeedbackEffects.PlaySfx(this, "upgrade_selected");
		EmitSignal(SignalName.UpgradeChoicesChanged, false);
		EmitSignal(SignalName.UpgradeApplied, (int)upgradeType);
	}

	public Dictionary<ResourceType, int> GetDiscountedCost(Dictionary<ResourceType, int> baseCost)
	{
		Dictionary<ResourceType, int> discountedCost = new();

		foreach (KeyValuePair<ResourceType, int> cost in baseCost)
		{
			discountedCost[cost.Key] = Math.Max(0, Mathf.CeilToInt(cost.Value * BuildingCostMultiplier));
		}

		return discountedCost;
	}

	public string FormatDiscountedCost(Dictionary<ResourceType, int> baseCost)
	{
		return BuildingDefinitions.FormatCost(GetDiscountedCost(baseCost));
	}

	private void ApplyUpgrade(UpgradeType upgradeType)
	{
		switch (upgradeType)
		{
			case UpgradeType.PlayerMoveSpeed:
				PlayerMoveSpeedMultiplier *= 1.2f;
				break;
			case UpgradeType.PlayerMoveSpeedSmall:
				PlayerMoveSpeedMultiplier *= 1.15f;
				break;
			case UpgradeType.PlayerMaxHealth:
				PlayerMaxHealthBonus += 25;
				break;
			case UpgradeType.PlayerMaxHealthLarge:
				PlayerMaxHealthBonus += 30;
				break;
			case UpgradeType.PlayerRepairSpeed:
				PlayerRepairSpeedMultiplier *= 1.25f;
				break;
			case UpgradeType.PlayerRepairEfficiency:
				PlayerRepairEfficiencyMultiplier *= 1.25f;
				break;
			case UpgradeType.PlayerGatherAmount:
				PlayerGatherAmountMultiplier *= 1.5f;
				break;
			case UpgradeType.PlayerDamageResistance:
				PlayerDamageTakenMultiplier *= 0.9f;
				break;
			case UpgradeType.CoreMaxHealth:
				CoreMaxHealthBonus += 50;
				break;
			case UpgradeType.CoreMaxHealthLarge:
				CoreMaxHealthBonus += 75;
				break;
			case UpgradeType.CoreDamageResistance:
				CoreDamageTakenMultiplier *= 0.9f;
				break;
			case UpgradeType.CoreDayRegen:
				CoreDayRegenPerSecond += 1f;
				break;
			case UpgradeType.CoreEmergencyRepair:
				if (GetTree().Root.FindChild("Core", true, false) is Core core)
				{
					core.Repair(50);
				}
				break;
			case UpgradeType.TurretFireRate:
				TurretFireRateMultiplier *= 1.2f;
				break;
			case UpgradeType.TurretFireRateSmall:
				TurretFireRateMultiplier *= 1.15f;
				break;
			case UpgradeType.TurretDamage:
				TurretDamageMultiplier *= 1.2f;
				break;
			case UpgradeType.TurretRange:
				TurretRangeMultiplier *= 1.15f;
				break;
			case UpgradeType.TurretAmmoEfficiency:
				TurretAmmoSaveChance = Mathf.Clamp(TurretAmmoSaveChance + 0.2f, 0f, 0.75f);
				break;
			case UpgradeType.TurretFocusFire:
				TurretTankDamageMultiplier *= 1.2f;
				break;
			case UpgradeType.DrillProduction:
				DrillProductionMultiplier *= 1.25f;
				break;
			case UpgradeType.GeneratorProduction:
				GeneratorProductionMultiplier *= 1.25f;
				break;
			case UpgradeType.AssemblerSpeed:
				AssemblerSpeedMultiplier *= 1.25f;
				break;
			case UpgradeType.AssemblerSpeedSmall:
				AssemblerSpeedMultiplier *= 1.2f;
				break;
			case UpgradeType.AssemblerAmmoOutput:
				AssemblerAmmoOutputBonus += 1;
				break;
			case UpgradeType.StorageCapacityBonus:
				StorageCapacityMultiplier *= 1.5f;
				break;
			case UpgradeType.ProductionSpeedAll:
				ProductionSpeedMultiplier *= 1.1f;
				break;
			case UpgradeType.BuildingCostDiscount:
				BuildingCostMultiplier *= 0.8f;
				break;
			case UpgradeType.BuildingCostDiscountSmall:
				BuildingCostMultiplier *= 0.85f;
				break;
			case UpgradeType.GainScrap:
				GetNodeOrNull<ResourceManager>("/root/ResourceManager")?.AddResource(ResourceType.Scrap, 50);
				break;
			case UpgradeType.GainScrapLarge:
				GetNodeOrNull<ResourceManager>("/root/ResourceManager")?.AddResource(ResourceType.Scrap, 75);
				break;
			case UpgradeType.GainEnergy:
				GetNodeOrNull<ResourceManager>("/root/ResourceManager")?.AddResource(ResourceType.Energy, 25);
				break;
			case UpgradeType.GainAmmo:
				GetNodeOrNull<ResourceManager>("/root/ResourceManager")?.AddResource(ResourceType.Ammo, 30);
				break;
			case UpgradeType.RepairCostDiscount:
				RepairCostMultiplier *= 0.75f;
				break;
		}
	}

	private int ChooseWeightedUpgradeIndex(List<UpgradeDefinition> upgrades)
	{
		int totalWeight = 0;
		foreach (UpgradeDefinition upgrade in upgrades)
		{
			totalWeight += GetRarityWeight(upgrade.Rarity);
		}

		int roll = random.RandiRange(1, totalWeight);
		for (int index = 0; index < upgrades.Count; index++)
		{
			roll -= GetRarityWeight(upgrades[index].Rarity);
			if (roll <= 0)
			{
				return index;
			}
		}

		return upgrades.Count - 1;
	}

	private static int GetRarityWeight(UpgradeRarity rarity)
	{
		return rarity switch
		{
			UpgradeRarity.Common => 5,
			UpgradeRarity.Uncommon => 3,
			UpgradeRarity.Rare => 1,
			_ => 1
		};
	}
}
