using Godot;

public partial class Building : Node2D
{
	[Export]
	public BuildingType BuildingType { get; set; }

	private Label statusLabel;
	private CanvasItem statusIndicator;
	private ResourceManager resourceManager;
	private UpgradeManager upgradeManager;
	private BuildingStatus status = BuildingStatus.Idle;

	public string DisplayName => BuildingDefinitions.Get(BuildingType).DisplayName;
	public BuildingStatus Status => status;
	protected ResourceManager ResourceManager => resourceManager;
	protected UpgradeManager UpgradeManager => upgradeManager;

	public override void _Ready()
	{
		resourceManager = GetNodeOrNull<ResourceManager>("/root/ResourceManager");
		upgradeManager = GetNodeOrNull<UpgradeManager>("/root/UpgradeManager");
		statusLabel = GetNodeOrNull<Label>("StatusLabel");
		statusIndicator = GetNodeOrNull<CanvasItem>("StatusIndicator");
		SetStatus(BuildingStatus.Idle);
	}

	protected void SetStatus(BuildingStatus nextStatus)
	{
		if (status == nextStatus && statusLabel != null && statusLabel.Text == GetStatusText(nextStatus))
		{
			return;
		}

		status = nextStatus;
		UpdateStatusVisuals();
	}

	private void UpdateStatusVisuals()
	{
		if (statusLabel != null)
		{
			statusLabel.Text = GetStatusText(status);
			statusLabel.Modulate = GetStatusColor(status);
		}

		if (statusIndicator != null)
		{
			statusIndicator.Modulate = GetStatusColor(status);
		}
	}

	private static string GetStatusText(BuildingStatus buildingStatus)
	{
		return buildingStatus switch
		{
			BuildingStatus.Working => "Working",
			BuildingStatus.MissingInput => "Missing Input",
			BuildingStatus.OutputFull => "Output Full",
			BuildingStatus.InvalidPlacement => "Invalid",
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
			_ => new Color(0.82f, 0.84f, 0.78f, 1f)
		};
	}
}
