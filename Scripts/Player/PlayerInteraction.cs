using Godot;
using System;
using System.Collections.Generic;

public partial class PlayerInteraction : Area2D
{
	[Signal]
	public delegate void InteractionHintChangedEventHandler(string hintText, bool isVisible);

	private const string InteractAction = "Interact";
	private const string RepairAction = "Repair";

	[Export]
	private int gatherAmount = 5;

	[Export]
	private float gatherInterval = 0.2f;

	[Export]
	private float repairRange = 80f;

	[Export]
	private int repairScrapCost = 2;

	[Export]
	private int repairAmount = 10;

	[Export]
	private float repairInterval = 0.25f;

	private readonly List<ScrapDeposit> nearbyDeposits = new();
	private ResourceManager resourceManager;
	private RunManager runManager;
	private UpgradeManager upgradeManager;
	private Core core;
	private double gatherCooldown;
	private double repairCooldown;
	private string currentHintText = string.Empty;
	private bool isHintVisible;

	public string CurrentHintText => currentHintText;
	public bool IsHintVisible => isHintVisible;

	public override void _Ready()
	{
		EnsureInteractAction();
		EnsureRepairAction();

		resourceManager = GetNodeOrNull<ResourceManager>("/root/ResourceManager");
		runManager = GetNodeOrNull<RunManager>("/root/RunManager");
		upgradeManager = GetNodeOrNull<UpgradeManager>("/root/UpgradeManager");
		core = GetTree().Root.FindChild("Core", true, false) as Core;
		if (resourceManager != null)
		{
			resourceManager.ResourcesChanged += RefreshHint;
		}
		else
		{
			GD.PushWarning("PlayerInteraction could not find the ResourceManager autoload.");
		}

		AreaEntered += OnAreaEntered;
		AreaExited += OnAreaExited;
		RefreshHint();
	}

	public override void _ExitTree()
	{
		if (resourceManager != null)
		{
			resourceManager.ResourcesChanged -= RefreshHint;
		}
	}

	public override void _Process(double delta)
	{
		if (gatherCooldown > 0.0)
		{
			gatherCooldown -= delta;
		}

		if (repairCooldown > 0.0)
		{
			repairCooldown -= delta;
		}

		if (Input.IsActionPressed(RepairAction) && repairCooldown <= 0.0)
		{
			bool hasRepairTarget = GetClosestRepairTarget() != null;
			if (TryRepairClosestTarget() || hasRepairTarget)
			{
				repairCooldown = GetEffectiveRepairInterval();
			}
		}

		if (Input.IsActionPressed(InteractAction) && gatherCooldown <= 0.0)
		{
			if (TryGatherFromClosestDeposit())
			{
				gatherCooldown = gatherInterval;
			}
			else if (GetClosestValidDeposit() != null)
			{
				gatherCooldown = gatherInterval;
			}
		}

		RefreshHint();
	}

	private bool TryRepairClosestTarget()
	{
		if (resourceManager == null)
		{
			return false;
		}

		Node2D target = GetClosestRepairTarget();
		if (target == null)
		{
			return false;
		}

		int missingHealth = GetMissingHealth(target);
		if (missingHealth <= 0)
		{
			return false;
		}

		Dictionary<ResourceType, int> repairCost = new()
		{
			{ ResourceType.Scrap, GetEffectiveRepairCost() }
		};

		if (!resourceManager.CanSpend(repairCost) || !resourceManager.Spend(repairCost))
		{
			FeedbackEffects.SpawnText(
				this,
				target.GlobalPosition,
				"Need Scrap",
				FeedbackEffects.WarningColor,
				FeedbackCategory.Error,
				1.0f,
				$"{target.GetInstanceId()}:need-scrap");
			return false;
		}

		int repairCostAmount = GetEffectiveRepairCost();
		int repaired = RepairTarget(target, Math.Min(GetEffectiveRepairAmount(), missingHealth));
		if (repaired <= 0)
		{
			resourceManager.AddResource(ResourceType.Scrap, repairCostAmount);
			return false;
		}

		FeedbackEffects.SpawnText(
			this,
			GlobalPosition,
			$"-{repairCostAmount} Scrap",
			FeedbackEffects.SpendColor,
			FeedbackCategory.Repair,
			0.1f,
			$"{GetInstanceId()}:repair-cost");
		runManager?.RecordRepair(repairCostAmount, repaired);
		return true;
	}

	private void OnAreaEntered(Area2D area)
	{
		if (area is not ScrapDeposit deposit || nearbyDeposits.Contains(deposit))
		{
			return;
		}

		nearbyDeposits.Add(deposit);
		RefreshHint();
	}

	private void OnAreaExited(Area2D area)
	{
		if (area is ScrapDeposit deposit)
		{
			nearbyDeposits.Remove(deposit);
			RefreshHint();
		}
	}

	private bool TryGatherFromClosestDeposit()
	{
		if (resourceManager == null)
		{
			return false;
		}

		ScrapDeposit deposit = GetClosestValidDeposit();
		if (deposit == null)
		{
			RefreshHint();
			return false;
		}

		int availableStorage = resourceManager.GetAvailableSpace(ResourceType.Scrap);
		if (availableStorage <= 0)
		{
			RefreshHint();
			return false;
		}

		int amountToGather = Math.Min(GetEffectiveGatherAmount(), availableStorage);
		int gatheredAmount = deposit.Gather(amountToGather);

		if (gatheredAmount <= 0)
		{
			RefreshHint();
			return false;
		}

		resourceManager.AddResource(ResourceType.Scrap, gatheredAmount);
		FeedbackEffects.SpawnText(
			this,
			deposit.GlobalPosition,
			$"+{gatheredAmount} Scrap",
			FeedbackEffects.ScrapGainColor,
			FeedbackCategory.Gathering,
			0.1f,
			$"{GetInstanceId()}:gather");
		GetNodeOrNull<RunManager>("/root/RunManager")?.RecordScrapGatheredManually(gatheredAmount);
		RefreshHint();
		return true;
	}

	private ScrapDeposit GetClosestValidDeposit()
	{
		ScrapDeposit closestDeposit = null;
		float closestDistanceSquared = float.MaxValue;

		for (int index = nearbyDeposits.Count - 1; index >= 0; index--)
		{
			ScrapDeposit deposit = nearbyDeposits[index];
			if (!IsInstanceValid(deposit) || deposit.IsEmpty)
			{
				nearbyDeposits.RemoveAt(index);
				continue;
			}

			float distanceSquared = GlobalPosition.DistanceSquaredTo(deposit.GlobalPosition);
			if (distanceSquared < closestDistanceSquared)
			{
				closestDistanceSquared = distanceSquared;
				closestDeposit = deposit;
			}
		}

		return closestDeposit;
	}

	private Node2D GetClosestRepairTarget()
	{
		Node2D closestTarget = null;
		float closestDistanceSquared = repairRange * repairRange;

		if (core != null && IsInstanceValid(core) && core.NeedsRepair)
		{
			float coreDistanceSquared = GlobalPosition.DistanceSquaredTo(core.GlobalPosition);
			if (coreDistanceSquared <= closestDistanceSquared)
			{
				closestDistanceSquared = coreDistanceSquared;
				closestTarget = core;
			}
		}

		foreach (Node node in GetTree().GetNodesInGroup("Buildings"))
		{
			if (node is not Building building ||
			    !IsInstanceValid(building) ||
			    !building.NeedsRepair)
			{
				continue;
			}

			float distanceSquared = GlobalPosition.DistanceSquaredTo(building.GlobalPosition);
			if (distanceSquared <= closestDistanceSquared)
			{
				closestDistanceSquared = distanceSquared;
				closestTarget = building;
			}
		}

		return closestTarget;
	}

	private static int RepairTarget(Node2D target, int amount)
	{
		return target switch
		{
			Core coreTarget => coreTarget.Repair(amount),
			Building buildingTarget => buildingTarget.Repair(amount),
			_ => 0
		};
	}

	private static int GetMissingHealth(Node2D target)
	{
		return target switch
		{
			Core coreTarget => coreTarget.MaxHealth - coreTarget.CurrentHealth,
			Building buildingTarget => buildingTarget.MaxHealth - buildingTarget.CurrentHealth,
			_ => 0
		};
	}

	private void RefreshHint()
	{
		Node2D repairTarget = GetClosestRepairTarget();
		if (repairTarget != null)
		{
			if (Input.IsActionPressed(RepairAction) &&
			    resourceManager != null &&
			    resourceManager.GetAmount(ResourceType.Scrap) < GetEffectiveRepairCost())
			{
				SetHint("Need Scrap to repair", true);
				return;
			}

			SetHint("Hold F to repair", true);
			return;
		}

		ScrapDeposit closestDeposit = GetClosestValidDeposit();
		if (closestDeposit == null)
		{
			SetHint(string.Empty, false);
			return;
		}

		if (resourceManager != null && resourceManager.IsFull(ResourceType.Scrap))
		{
			SetHint("Scrap storage full", true);
			return;
		}

		SetHint("Press E to gather Scrap", true);
	}

	private int GetEffectiveGatherAmount()
	{
		float multiplier = upgradeManager?.PlayerGatherAmountMultiplier ?? 1f;
		return Mathf.Max(1, Mathf.RoundToInt(gatherAmount * multiplier));
	}

	private float GetEffectiveRepairInterval()
	{
		float multiplier = upgradeManager?.PlayerRepairSpeedMultiplier ?? 1f;
		return repairInterval / multiplier;
	}

	private int GetEffectiveRepairAmount()
	{
		float multiplier = upgradeManager?.PlayerRepairEfficiencyMultiplier ?? 1f;
		return Mathf.Max(1, Mathf.RoundToInt(repairAmount * multiplier));
	}

	private int GetEffectiveRepairCost()
	{
		float multiplier = upgradeManager?.RepairCostMultiplier ?? 1f;
		return Mathf.Max(1, Mathf.CeilToInt(repairScrapCost * multiplier));
	}

	private void SetHint(string hintText, bool visible)
	{
		if (currentHintText == hintText && isHintVisible == visible)
		{
			return;
		}

		currentHintText = hintText;
		isHintVisible = visible;
		EmitSignal(SignalName.InteractionHintChanged, currentHintText, isHintVisible);
	}

	private static void EnsureInteractAction()
	{
		if (!InputMap.HasAction(InteractAction))
		{
			InputMap.AddAction(InteractAction);
		}

		if (!ActionHasKey(InteractAction, Key.E))
		{
			InputMap.ActionAddEvent(InteractAction, new InputEventKey
			{
				PhysicalKeycode = Key.E
			});
		}
	}

	private static void EnsureRepairAction()
	{
		if (!InputMap.HasAction(RepairAction))
		{
			InputMap.AddAction(RepairAction);
		}

		if (!ActionHasKey(RepairAction, Key.F))
		{
			InputMap.ActionAddEvent(RepairAction, new InputEventKey
			{
				PhysicalKeycode = Key.F
			});
		}
	}

	private static bool ActionHasKey(string action, Key key)
	{
		foreach (InputEvent inputEvent in InputMap.ActionGetEvents(action))
		{
			if (inputEvent is InputEventKey keyEvent &&
			    (keyEvent.PhysicalKeycode == key || keyEvent.Keycode == key))
			{
				return true;
			}
		}

		return false;
	}
}
