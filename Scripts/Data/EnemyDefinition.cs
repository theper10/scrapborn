using Godot;
using System.Collections.Generic;

public sealed class EnemyDefinition
{
	public EnemyDefinition(EnemyType type, string displayName, string scenePath)
	{
		Type = type;
		DisplayName = displayName;
		ScenePath = scenePath;
	}

	public EnemyType Type { get; }
	public string DisplayName { get; }
	public string ScenePath { get; }
}

public static class EnemyDefinitions
{
	private static readonly Dictionary<EnemyType, EnemyDefinition> definitions = new()
	{
		{
			EnemyType.Crawler,
			new EnemyDefinition(EnemyType.Crawler, "Crawler", "res://Scenes/Enemies/CrawlerEnemy.tscn")
		},
		{
			EnemyType.Fast,
			new EnemyDefinition(EnemyType.Fast, "Fast", "res://Scenes/Enemies/FastEnemy.tscn")
		},
		{
			EnemyType.Tank,
			new EnemyDefinition(EnemyType.Tank, "Tank", "res://Scenes/Enemies/TankEnemy.tscn")
		}
	};

	public static EnemyDefinition Get(EnemyType type)
	{
		return definitions[type];
	}

	public static PackedScene LoadScene(EnemyType type)
	{
		return ResourceLoader.Load<PackedScene>(Get(type).ScenePath);
	}
}
