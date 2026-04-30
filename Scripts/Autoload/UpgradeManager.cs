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
	private readonly RandomNumberGenerator random = new();

	public IReadOnlyList<UpgradeDefinition> CurrentChoices => currentChoices;

	public float PlayerMoveSpeedMultiplier { get; private set; } = 1f;
	public int PlayerMaxHealthBonus { get; private set; }
	public int CoreMaxHealthBonus { get; private set; }
	public float TurretFireRateMultiplier { get; private set; } = 1f;
	public float TurretDamageMultiplier { get; private set; } = 1f;
	public float DrillProductionMultiplier { get; private set; } = 1f;
	public float GeneratorProductionMultiplier { get; private set; } = 1f;
	public float AssemblerSpeedMultiplier { get; private set; } = 1f;
	public float BuildingCostMultiplier { get; private set; } = 1f;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		random.Randomize();
		ResetUpgrades();
	}

	public void ResetUpgrades()
	{
		currentChoices.Clear();
		PlayerMoveSpeedMultiplier = 1f;
		PlayerMaxHealthBonus = 0;
		CoreMaxHealthBonus = 0;
		TurretFireRateMultiplier = 1f;
		TurretDamageMultiplier = 1f;
		DrillProductionMultiplier = 1f;
		GeneratorProductionMultiplier = 1f;
		AssemblerSpeedMultiplier = 1f;
		BuildingCostMultiplier = 1f;
		EmitSignal(SignalName.UpgradeChoicesChanged, false);
	}

	public void OfferUpgradeChoices()
	{
		currentChoices.Clear();

		List<UpgradeDefinition> availableUpgrades = new(UpgradeDefinitions.All.Values);
		while (currentChoices.Count < 3 && availableUpgrades.Count > 0)
		{
			int index = random.RandiRange(0, availableUpgrades.Count - 1);
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

		UpgradeType upgradeType = currentChoices[choiceIndex].Type;
		ApplyUpgrade(upgradeType);
		currentChoices.Clear();
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
			case UpgradeType.PlayerMaxHealth:
				PlayerMaxHealthBonus += 25;
				break;
			case UpgradeType.CoreMaxHealth:
				CoreMaxHealthBonus += 50;
				break;
			case UpgradeType.TurretFireRate:
				TurretFireRateMultiplier *= 1.2f;
				break;
			case UpgradeType.TurretDamage:
				TurretDamageMultiplier *= 1.2f;
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
			case UpgradeType.BuildingCostDiscount:
				BuildingCostMultiplier *= 0.8f;
				break;
			case UpgradeType.GainScrap:
				GetNodeOrNull<ResourceManager>("/root/ResourceManager")?.AddResource(ResourceType.Scrap, 50);
				break;
		}
	}
}
