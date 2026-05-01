using Godot;
using System.Collections.Generic;

public sealed class BuildingDefinition
{
	public BuildingDefinition(
		BuildingType type,
		string displayName,
		string scenePath,
		Dictionary<ResourceType, int> cost,
		string purpose)
	{
		Type = type;
		DisplayName = displayName;
		ScenePath = scenePath;
		Cost = cost;
		Purpose = purpose;
	}

	public BuildingType Type { get; }
	public string DisplayName { get; }
	public string ScenePath { get; }
	public Dictionary<ResourceType, int> Cost { get; }
	public string Purpose { get; }
}

public static class BuildingDefinitions
{
	private static readonly Dictionary<BuildingType, BuildingDefinition> definitions = new()
	{
		{
			BuildingType.Drill,
			new BuildingDefinition(
				BuildingType.Drill,
				"Drill",
				"res://Scenes/Buildings/Drill.tscn",
				new Dictionary<ResourceType, int> { { ResourceType.Scrap, 20 } },
				"Produces Scrap near deposits")
		},
		{
			BuildingType.Generator,
			new BuildingDefinition(
				BuildingType.Generator,
				"Generator",
				"res://Scenes/Buildings/Generator.tscn",
				new Dictionary<ResourceType, int> { { ResourceType.Scrap, 20 } },
				"Produces Energy")
		},
		{
			BuildingType.Assembler,
			new BuildingDefinition(
				BuildingType.Assembler,
				"Assembler",
				"res://Scenes/Buildings/Assembler.tscn",
				new Dictionary<ResourceType, int>
				{
					{ ResourceType.Scrap, 35 },
					{ ResourceType.Energy, 8 }
				},
				"Makes Ammo")
		},
		{
			BuildingType.Turret,
			new BuildingDefinition(
				BuildingType.Turret,
				"Turret",
				"res://Scenes/Buildings/Turret.tscn",
				new Dictionary<ResourceType, int>
				{
					{ ResourceType.Scrap, 25 },
					{ ResourceType.Energy, 3 }
				},
				"Shoots enemies")
		},
		{
			BuildingType.Storage,
			new BuildingDefinition(
				BuildingType.Storage,
				"Storage",
				"res://Scenes/Buildings/Storage.tscn",
				new Dictionary<ResourceType, int> { { ResourceType.Scrap, 20 } },
				"Increases capacity")
		}
	};

	public static BuildingDefinition Get(BuildingType type)
	{
		return definitions[type];
	}

	public static string FormatCost(BuildingType type)
	{
		return FormatCost(Get(type).Cost);
	}

	public static string FormatCost(Dictionary<ResourceType, int> cost)
	{
		List<string> parts = new();

		foreach (ResourceType resourceType in System.Enum.GetValues<ResourceType>())
		{
			if (cost.TryGetValue(resourceType, out int amount) && amount > 0)
			{
				parts.Add($"{amount} {resourceType}");
			}
		}

		return parts.Count > 0 ? string.Join(", ", parts) : "Free";
	}

	public static string FormatCompactCost(Dictionary<ResourceType, int> cost)
	{
		List<string> parts = new();

		foreach (ResourceType resourceType in System.Enum.GetValues<ResourceType>())
		{
			if (cost.TryGetValue(resourceType, out int amount) && amount > 0)
			{
				parts.Add($"{amount}{GetResourceAbbreviation(resourceType)}");
			}
		}

		return parts.Count > 0 ? string.Join(" ", parts) : "Free";
	}

	public static PackedScene LoadScene(BuildingType type)
	{
		return ResourceLoader.Load<PackedScene>(Get(type).ScenePath);
	}

	private static string GetResourceAbbreviation(ResourceType type)
	{
		return type switch
		{
			ResourceType.Scrap => "S",
			ResourceType.Energy => "E",
			ResourceType.Ammo => "A",
			_ => type.ToString()
		};
	}
}
