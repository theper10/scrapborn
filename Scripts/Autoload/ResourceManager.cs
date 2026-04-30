using Godot;
using System;
using System.Collections.Generic;

public partial class ResourceManager : Node
{
	[Signal]
	public delegate void ResourcesChangedEventHandler();

	private readonly Dictionary<ResourceType, int> amounts = new()
	{
		{ ResourceType.Scrap, 50 },
		{ ResourceType.Energy, 0 },
		{ ResourceType.Ammo, 0 }
	};

	private readonly Dictionary<ResourceType, int> maxValues = new()
	{
		{ ResourceType.Scrap, 200 },
		{ ResourceType.Energy, 100 },
		{ ResourceType.Ammo, 100 }
	};

	public override void _Ready()
	{
		ClampAllResources();
		EmitSignal(SignalName.ResourcesChanged);
	}

	public int AddResource(ResourceType type, int amount)
	{
		return SetAmount(type, GetAmount(type) + amount);
	}

	public bool CanSpend(Dictionary<ResourceType, int> cost)
	{
		if (cost == null)
		{
			return false;
		}

		foreach (KeyValuePair<ResourceType, int> entry in cost)
		{
			if (entry.Value < 0 || GetAmount(entry.Key) < entry.Value)
			{
				return false;
			}
		}

		return true;
	}

	public bool Spend(Dictionary<ResourceType, int> cost)
	{
		if (!CanSpend(cost))
		{
			return false;
		}

		foreach (KeyValuePair<ResourceType, int> entry in cost)
		{
			amounts[entry.Key] = Math.Clamp(GetAmount(entry.Key) - entry.Value, 0, GetMax(entry.Key));
		}

		EmitSignal(SignalName.ResourcesChanged);
		return true;
	}

	public int GetAmount(ResourceType type)
	{
		return amounts.TryGetValue(type, out int value) ? value : 0;
	}

	public int GetMax(ResourceType type)
	{
		return maxValues.TryGetValue(type, out int value) ? value : 0;
	}

	public void AddMaxResource(ResourceType type, int amount)
	{
		if (amount == 0)
		{
			return;
		}

		maxValues[type] = Math.Max(0, GetMax(type) + amount);
		amounts[type] = Math.Clamp(GetAmount(type), 0, GetMax(type));
		EmitSignal(SignalName.ResourcesChanged);
	}

	public void AddMaxResources(Dictionary<ResourceType, int> bonuses)
	{
		if (bonuses == null || bonuses.Count == 0)
		{
			return;
		}

		foreach (KeyValuePair<ResourceType, int> bonus in bonuses)
		{
			maxValues[bonus.Key] = Math.Max(0, GetMax(bonus.Key) + bonus.Value);
			amounts[bonus.Key] = Math.Clamp(GetAmount(bonus.Key), 0, GetMax(bonus.Key));
		}

		EmitSignal(SignalName.ResourcesChanged);
	}

	public int GetAvailableSpace(ResourceType type)
	{
		return Math.Max(0, GetMax(type) - GetAmount(type));
	}

	public bool IsFull(ResourceType type)
	{
		return GetAvailableSpace(type) <= 0;
	}

	private int SetAmount(ResourceType type, int value)
	{
		int previousAmount = GetAmount(type);
		amounts[type] = Math.Clamp(value, 0, GetMax(type));

		if (amounts[type] != previousAmount)
		{
			EmitSignal(SignalName.ResourcesChanged);
		}

		return amounts[type] - previousAmount;
	}

	private void ClampAllResources()
	{
		foreach (ResourceType type in Enum.GetValues<ResourceType>())
		{
			amounts[type] = Math.Clamp(GetAmount(type), 0, GetMax(type));
		}
	}
}
