using Godot;
using System;

public partial class Building : Node2D, IInspectable
{
	[Signal]
	public delegate void DestroyedEventHandler(Building building);

	[Export]
	public BuildingType BuildingType { get; set; }

	[Export]
	private int maxHealth;

	private Label statusLabel;
	private CanvasItem statusIndicator;
	private Line2D selectionHighlight;
	private ResourceManager resourceManager;
	private UpgradeManager upgradeManager;
	private BuildingStatus status = BuildingStatus.Idle;

	public string DisplayName => BuildingDefinitions.Get(BuildingType).DisplayName;
	public string InspectableName => DisplayName;
	public BuildingStatus Status => status;
	public int CurrentHealth { get; private set; }
	public int MaxHealth => maxHealth;
	public bool IsDestroyed { get; private set; }
	public bool NeedsRepair => !IsDestroyed && CurrentHealth < maxHealth;
	public bool IsSelectable => !IsDestroyed && Visible;
	public Vector2I GridCell { get; private set; } = new(int.MinValue, int.MinValue);
	protected ResourceManager ResourceManager => resourceManager;
	protected UpgradeManager UpgradeManager => upgradeManager;

	public override void _Ready()
	{
		AddToGroup("Buildings");
		resourceManager = GetNodeOrNull<ResourceManager>("/root/ResourceManager");
		upgradeManager = GetNodeOrNull<UpgradeManager>("/root/UpgradeManager");
		statusLabel = GetNodeOrNull<Label>("StatusLabel");
		statusIndicator = GetNodeOrNull<CanvasItem>("StatusIndicator");
		CreateSelectionHighlight();
		if (maxHealth <= 0)
		{
			maxHealth = GetDefaultMaxHealth(BuildingType);
		}

		CurrentHealth = maxHealth;
		UpdateHealthVisuals();
		SetStatus(BuildingStatus.Idle);
	}

	public void InitializePlacement(Vector2I gridCell)
	{
		GridCell = gridCell;
	}

	public void TakeDamage(int amount)
	{
		if (amount <= 0 || IsDestroyed)
		{
			return;
		}

		CurrentHealth = Math.Clamp(CurrentHealth - amount, 0, maxHealth);
		UpdateHealthVisuals();
		UpdateStatusVisuals();

		if (CurrentHealth <= 0)
		{
			DestroyBuilding();
		}
	}

	public int Repair(int amount)
	{
		if (amount <= 0 || IsDestroyed || CurrentHealth >= maxHealth)
		{
			return 0;
		}

		int previousHealth = CurrentHealth;
		CurrentHealth = Math.Clamp(CurrentHealth + amount, 0, maxHealth);
		UpdateHealthVisuals();
		UpdateStatusVisuals();
		return CurrentHealth - previousHealth;
	}

	public void SetSelected(bool selected)
	{
		if (selectionHighlight != null)
		{
			selectionHighlight.Visible = selected && IsSelectable;
		}
	}

	public string GetInspectionText()
	{
		string details = GetInspectionDetails();
		string repairHint = NeedsRepair ? "\nRepair: Hold F nearby to repair" : string.Empty;
		return
			$"Type: {BuildingType}\n" +
			$"HP: {CurrentHealth} / {maxHealth}\n" +
			$"Status: {GetDisplayStatus()}" +
			(string.IsNullOrEmpty(details) ? string.Empty : $"\n{details}") +
			repairHint;
	}

	public string GetHoverText()
	{
		return $"{InspectableName}\nHP: {CurrentHealth} / {maxHealth}\nStatus: {GetDisplayStatus()}";
	}

	protected void SetStatus(BuildingStatus nextStatus)
	{
		if (IsDestroyed && nextStatus != BuildingStatus.Destroyed)
		{
			return;
		}

		if (status == nextStatus && statusLabel != null && statusLabel.Text == GetStatusText(nextStatus, BuildingType))
		{
			return;
		}

		status = nextStatus;
		UpdateStatusVisuals();
	}

	protected virtual void OnDestroyed()
	{
	}

	protected virtual string GetInspectionDetails()
	{
		return string.Empty;
	}

	private void DestroyBuilding()
	{
		IsDestroyed = true;
		OnDestroyed();
		SetSelected(false);
		SetStatus(BuildingStatus.Destroyed);
		UpdateHealthVisuals();
		EmitSignal(SignalName.Destroyed, this);
	}

	private void CreateSelectionHighlight()
	{
		selectionHighlight = new Line2D
		{
			Name = "SelectionHighlight",
			ZIndex = 80,
			Width = 3f,
			DefaultColor = new Color(1f, 0.92f, 0.25f, 1f),
			Visible = false,
			Closed = true,
			Points = new[]
			{
				new Vector2(-31, -31),
				new Vector2(31, -31),
				new Vector2(31, 31),
				new Vector2(-31, 31)
			}
		};

		AddChild(selectionHighlight);
	}

	private void UpdateHealthVisuals()
	{
		if (IsDestroyed)
		{
			Modulate = new Color(0.25f, 0.25f, 0.25f, 0.45f);
			Visible = false;
			return;
		}

		if (maxHealth > 0 && CurrentHealth < maxHealth)
		{
			Modulate = new Color(1f, 0.72f, 0.52f, 1f);
			return;
		}

		Modulate = Colors.White;
	}

	private void UpdateStatusVisuals()
	{
		if (statusLabel != null)
		{
			statusLabel.Text = GetDisplayStatus();
			statusLabel.Modulate = GetStatusColor(status);
		}

		if (statusIndicator != null)
		{
			statusIndicator.Modulate = GetStatusColor(status);
		}
	}

	private string GetDisplayStatus()
	{
		if (IsDestroyed)
		{
			return "Destroyed";
		}

		string baseStatus = GetStatusText(status, BuildingType);
		if (!NeedsRepair)
		{
			return baseStatus;
		}

		return status == BuildingStatus.Idle ? "Damaged" : $"{baseStatus} (Damaged)";
	}

	private static string GetStatusText(BuildingStatus buildingStatus, BuildingType buildingType)
	{
		if (buildingType == BuildingType.Drill && buildingStatus == BuildingStatus.InvalidPlacement)
		{
			return "Needs Scrap Deposit";
		}

		if (buildingType == BuildingType.Turret && buildingStatus == BuildingStatus.MissingInput)
		{
			return "Missing Ammo";
		}

		return buildingStatus switch
		{
			BuildingStatus.Working => "Working",
			BuildingStatus.MissingInput => "Missing Input",
			BuildingStatus.OutputFull => "Output Full",
			BuildingStatus.InvalidPlacement => "Invalid",
			BuildingStatus.Destroyed => "Destroyed",
			_ => "Idle"
		};
	}

	private static Color GetStatusColor(BuildingStatus buildingStatus)
	{
		return buildingStatus switch
		{
			BuildingStatus.Working => new Color(0.25f, 0.95f, 0.45f, 1f),
			BuildingStatus.MissingInput => new Color(0.95f, 0.78f, 0.25f, 1f),
			BuildingStatus.OutputFull => new Color(0.35f, 0.75f, 1f, 1f),
			BuildingStatus.InvalidPlacement => new Color(0.95f, 0.24f, 0.2f, 1f),
			BuildingStatus.Destroyed => new Color(0.35f, 0.35f, 0.35f, 1f),
			_ => new Color(0.82f, 0.84f, 0.78f, 1f)
		};
	}

	private static int GetDefaultMaxHealth(BuildingType buildingType)
	{
		return buildingType switch
		{
			BuildingType.Drill => 60,
			BuildingType.Generator => 70,
			BuildingType.Assembler => 80,
			BuildingType.Turret => 75,
			BuildingType.Storage => 90,
			_ => 60
		};
	}
}
