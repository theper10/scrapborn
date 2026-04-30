using System.Collections.Generic;

public sealed class UpgradeDefinition
{
	public UpgradeDefinition(UpgradeType type, string displayName, string description)
	{
		Type = type;
		DisplayName = displayName;
		Description = description;
	}

	public UpgradeType Type { get; }
	public string DisplayName { get; }
	public string Description { get; }
}

public static class UpgradeDefinitions
{
	private static readonly Dictionary<UpgradeType, UpgradeDefinition> definitions = new()
	{
		{
			UpgradeType.PlayerMoveSpeed,
			new UpgradeDefinition(UpgradeType.PlayerMoveSpeed, "Quick Servos", "Player move speed +20%.")
		},
		{
			UpgradeType.PlayerMaxHealth,
			new UpgradeDefinition(UpgradeType.PlayerMaxHealth, "Reinforced Shell", "Player max health +25.")
		},
		{
			UpgradeType.CoreMaxHealth,
			new UpgradeDefinition(UpgradeType.CoreMaxHealth, "Core Plating", "Core max health +50.")
		},
		{
			UpgradeType.TurretFireRate,
			new UpgradeDefinition(UpgradeType.TurretFireRate, "Faster Turrets", "Turret fire rate +20%.")
		},
		{
			UpgradeType.TurretDamage,
			new UpgradeDefinition(UpgradeType.TurretDamage, "Sharper Rounds", "Turret damage +20%.")
		},
		{
			UpgradeType.DrillProduction,
			new UpgradeDefinition(UpgradeType.DrillProduction, "Hotter Drill Bits", "Drill production +25%.")
		},
		{
			UpgradeType.GeneratorProduction,
			new UpgradeDefinition(UpgradeType.GeneratorProduction, "Better Dynamos", "Generator production +25%.")
		},
		{
			UpgradeType.AssemblerSpeed,
			new UpgradeDefinition(UpgradeType.AssemblerSpeed, "Assembler Tuning", "Assembler speed +25%.")
		},
		{
			UpgradeType.BuildingCostDiscount,
			new UpgradeDefinition(UpgradeType.BuildingCostDiscount, "Scrap-Saver Plans", "Building costs -20%.")
		},
		{
			UpgradeType.GainScrap,
			new UpgradeDefinition(UpgradeType.GainScrap, "Emergency Scrap", "Gain +50 Scrap instantly.")
		}
	};

	public static IReadOnlyDictionary<UpgradeType, UpgradeDefinition> All => definitions;

	public static UpgradeDefinition Get(UpgradeType type)
	{
		return definitions[type];
	}
}
