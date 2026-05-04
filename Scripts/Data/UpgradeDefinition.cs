using System.Collections.Generic;

public sealed class UpgradeDefinition
{
	public UpgradeDefinition(
		UpgradeType type,
		string displayName,
		string description,
		UpgradeCategory category,
		UpgradeRarity rarity)
	{
		Type = type;
		DisplayName = displayName;
		Description = description;
		Category = category;
		Rarity = rarity;
	}

	public UpgradeType Type { get; }
	public string DisplayName { get; }
	public string Description { get; }
	public UpgradeCategory Category { get; }
	public UpgradeRarity Rarity { get; }
}

public static class UpgradeDefinitions
{
	private static readonly Dictionary<UpgradeType, UpgradeDefinition> definitions = new()
	{
		{ UpgradeType.PlayerMoveSpeed, Define(UpgradeType.PlayerMoveSpeed, "Quick Servos", "Player move speed +20%.", UpgradeCategory.Player, UpgradeRarity.Uncommon) },
		{ UpgradeType.PlayerMoveSpeedSmall, Define(UpgradeType.PlayerMoveSpeedSmall, "Lightweight Joints", "Player move speed +15%.", UpgradeCategory.Player, UpgradeRarity.Common) },
		{ UpgradeType.PlayerMaxHealth, Define(UpgradeType.PlayerMaxHealth, "Reinforced Shell", "Player max health +25.", UpgradeCategory.Player, UpgradeRarity.Common) },
		{ UpgradeType.PlayerMaxHealthLarge, Define(UpgradeType.PlayerMaxHealthLarge, "Layered Chassis", "Player max health +30.", UpgradeCategory.Player, UpgradeRarity.Uncommon) },
		{ UpgradeType.PlayerRepairSpeed, Define(UpgradeType.PlayerRepairSpeed, "Fast Patch Kit", "Repair 25% faster.", UpgradeCategory.Player, UpgradeRarity.Uncommon) },
		{ UpgradeType.PlayerRepairEfficiency, Define(UpgradeType.PlayerRepairEfficiency, "Better Welds", "Repair restores 25% more health.", UpgradeCategory.Player, UpgradeRarity.Uncommon) },
		{ UpgradeType.PlayerGatherAmount, Define(UpgradeType.PlayerGatherAmount, "Wide Magnet Claw", "Manual Scrap gathering +50%.", UpgradeCategory.Player, UpgradeRarity.Common) },
		{ UpgradeType.PlayerDamageResistance, Define(UpgradeType.PlayerDamageResistance, "Shock Padding", "Player damage taken -10%.", UpgradeCategory.Player, UpgradeRarity.Rare) },

		{ UpgradeType.CoreMaxHealth, Define(UpgradeType.CoreMaxHealth, "Core Plating", "Core max health +50.", UpgradeCategory.Core, UpgradeRarity.Common) },
		{ UpgradeType.CoreMaxHealthLarge, Define(UpgradeType.CoreMaxHealthLarge, "Heavy Core Plating", "Core max health +75.", UpgradeCategory.Core, UpgradeRarity.Uncommon) },
		{ UpgradeType.CoreDamageResistance, Define(UpgradeType.CoreDamageResistance, "Dampened Core Mounts", "Core damage taken -10%.", UpgradeCategory.Core, UpgradeRarity.Rare) },
		{ UpgradeType.CoreDayRegen, Define(UpgradeType.CoreDayRegen, "Daylight Auto-Repair", "Core regenerates +1 HP/sec during Day.", UpgradeCategory.Core, UpgradeRarity.Rare) },
		{ UpgradeType.CoreEmergencyRepair, Define(UpgradeType.CoreEmergencyRepair, "Emergency Core Patch", "Instantly restore 50 Core HP.", UpgradeCategory.Core, UpgradeRarity.Uncommon) },

		{ UpgradeType.TurretFireRate, Define(UpgradeType.TurretFireRate, "Faster Turrets", "Turret fire rate +20%.", UpgradeCategory.Turret, UpgradeRarity.Uncommon) },
		{ UpgradeType.TurretFireRateSmall, Define(UpgradeType.TurretFireRateSmall, "Greased Feeders", "Turret fire rate +15%.", UpgradeCategory.Turret, UpgradeRarity.Common) },
		{ UpgradeType.TurretDamage, Define(UpgradeType.TurretDamage, "Sharper Rounds", "Turret damage +20%.", UpgradeCategory.Turret, UpgradeRarity.Uncommon) },
		{ UpgradeType.TurretRange, Define(UpgradeType.TurretRange, "Long Barrel Mounts", "Turret range +15%.", UpgradeCategory.Turret, UpgradeRarity.Uncommon) },
		{ UpgradeType.TurretAmmoEfficiency, Define(UpgradeType.TurretAmmoEfficiency, "Spent Brass Recovery", "Turrets have a 20% chance not to consume Ammo.", UpgradeCategory.Turret, UpgradeRarity.Rare) },
		{ UpgradeType.TurretFocusFire, Define(UpgradeType.TurretFocusFire, "Armor-Piercing Focus", "Turrets deal +20% damage to Tanks.", UpgradeCategory.Turret, UpgradeRarity.Rare) },

		{ UpgradeType.DrillProduction, Define(UpgradeType.DrillProduction, "Hotter Drill Bits", "Drill production +25%.", UpgradeCategory.Production, UpgradeRarity.Common) },
		{ UpgradeType.GeneratorProduction, Define(UpgradeType.GeneratorProduction, "Better Dynamos", "Generator production +25%.", UpgradeCategory.Production, UpgradeRarity.Common) },
		{ UpgradeType.AssemblerSpeed, Define(UpgradeType.AssemblerSpeed, "Assembler Tuning", "Assembler speed +25%.", UpgradeCategory.Production, UpgradeRarity.Uncommon) },
		{ UpgradeType.AssemblerSpeedSmall, Define(UpgradeType.AssemblerSpeedSmall, "Cleaner Assembly Jig", "Assembler speed +20%.", UpgradeCategory.Production, UpgradeRarity.Common) },
		{ UpgradeType.AssemblerAmmoOutput, Define(UpgradeType.AssemblerAmmoOutput, "Extended Ammo Dies", "Assemblers output +1 Ammo.", UpgradeCategory.Production, UpgradeRarity.Uncommon) },
		{ UpgradeType.StorageCapacityBonus, Define(UpgradeType.StorageCapacityBonus, "Stacked Storage Bins", "Storage capacity bonuses +50%.", UpgradeCategory.Production, UpgradeRarity.Uncommon) },
		{ UpgradeType.ProductionSpeedAll, Define(UpgradeType.ProductionSpeedAll, "Factory Rhythm", "All production buildings work 10% faster.", UpgradeCategory.Production, UpgradeRarity.Rare) },

		{ UpgradeType.BuildingCostDiscount, Define(UpgradeType.BuildingCostDiscount, "Scrap-Saver Plans", "Building costs -20%.", UpgradeCategory.Economy, UpgradeRarity.Rare) },
		{ UpgradeType.BuildingCostDiscountSmall, Define(UpgradeType.BuildingCostDiscountSmall, "Lean Blueprints", "Building costs -15%.", UpgradeCategory.Economy, UpgradeRarity.Uncommon) },
		{ UpgradeType.GainScrap, Define(UpgradeType.GainScrap, "Emergency Scrap", "Gain +50 Scrap instantly.", UpgradeCategory.Economy, UpgradeRarity.Common) },
		{ UpgradeType.GainScrapLarge, Define(UpgradeType.GainScrapLarge, "Scrap Cache", "Gain +75 Scrap instantly.", UpgradeCategory.Economy, UpgradeRarity.Common) },
		{ UpgradeType.GainEnergy, Define(UpgradeType.GainEnergy, "Charged Capacitors", "Gain +25 Energy instantly.", UpgradeCategory.Economy, UpgradeRarity.Common) },
		{ UpgradeType.GainAmmo, Define(UpgradeType.GainAmmo, "Ammo Crate", "Gain +30 Ammo instantly.", UpgradeCategory.Economy, UpgradeRarity.Common) },
		{ UpgradeType.RepairCostDiscount, Define(UpgradeType.RepairCostDiscount, "Salvage Patches", "Repair costs -25%.", UpgradeCategory.Economy, UpgradeRarity.Uncommon) }
	};

	public static IReadOnlyDictionary<UpgradeType, UpgradeDefinition> All => definitions;

	public static UpgradeDefinition Get(UpgradeType type)
	{
		return definitions[type];
	}

	private static UpgradeDefinition Define(
		UpgradeType type,
		string displayName,
		string description,
		UpgradeCategory category,
		UpgradeRarity rarity)
	{
		return new UpgradeDefinition(type, displayName, description, category, rarity);
	}
}
