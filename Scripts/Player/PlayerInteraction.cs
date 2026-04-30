using Godot;
using System;
using System.Collections.Generic;

public partial class PlayerInteraction : Area2D
{
	[Signal]
	public delegate void InteractionHintChangedEventHandler(string hintText, bool isVisible);

	private const string InteractAction = "Interact";

	[Export]
	private int gatherAmount = 5;

	[Export]
	private float gatherInterval = 0.2f;

	private readonly List<ScrapDeposit> nearbyDeposits = new();
	private ResourceManager resourceManager;
	private double gatherCooldown;
	private string currentHintText = string.Empty;
	private bool isHintVisible;

	public string CurrentHintText => currentHintText;
	public bool IsHintVisible => isHintVisible;

	public override void _Ready()
	{
		EnsureInteractAction();

		resourceManager = GetNodeOrNull<ResourceManager>("/root/ResourceManager");
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

		if (!Input.IsActionPressed(InteractAction) || gatherCooldown > 0.0)
		{
			return;
		}

		if (TryGatherFromClosestDeposit())
		{
			gatherCooldown = gatherInterval;
			return;
		}

		if (GetClosestValidDeposit() != null)
		{
			gatherCooldown = gatherInterval;
		}
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

		int amountToGather = Math.Min(gatherAmount, availableStorage);
		int gatheredAmount = deposit.Gather(amountToGather);

		if (gatheredAmount <= 0)
		{
			RefreshHint();
			return false;
		}

		resourceManager.AddResource(ResourceType.Scrap, gatheredAmount);
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

	private void RefreshHint()
	{
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
