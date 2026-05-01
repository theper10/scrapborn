using Godot;

public partial class SelectionController : Node2D
{
	[Signal]
	public delegate void SelectionChangedEventHandler(Node selectedNode);

	[Signal]
	public delegate void HoverChangedEventHandler(Node hoveredNode);

	[Export]
	private float buildingSelectionRadius = 36f;

	[Export]
	private float coreSelectionRadius = 48f;

	private BuildingPlacer buildingPlacer;
	private RunManager runManager;
	private IInspectable selectedInspectable;
	private Node selectedNode;
	private IInspectable hoveredInspectable;
	private Node hoveredNode;

	public IInspectable SelectedInspectable => selectedInspectable;
	public Node SelectedNode => selectedNode;
	public IInspectable HoveredInspectable => hoveredInspectable;
	public Node HoveredNode => hoveredNode;

	public override void _Ready()
	{
		buildingPlacer = GetNodeOrNull<BuildingPlacer>("../BuildingPlacer");
		runManager = GetNodeOrNull<RunManager>("/root/RunManager");
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (inputEvent is not InputEventMouseButton mouseButton ||
		    !mouseButton.Pressed ||
		    mouseButton.ButtonIndex != MouseButton.Left)
		{
			return;
		}

		if (!CanSelectNow())
		{
			return;
		}

		SelectNode(FindSelectableAt(GetGlobalMousePosition()));
		GetViewport().SetInputAsHandled();
	}

	public override void _Process(double delta)
	{
		UpdateHover();

		if (selectedNode != null &&
		    (!IsInstanceValid(selectedNode) ||
		     selectedInspectable == null ||
		     !selectedInspectable.IsSelectable))
		{
			ClearSelection();
		}
	}

	public void ClearSelection()
	{
		SelectNode(null);
	}

	private bool CanSelectNow()
	{
		if (GetTree().Paused)
		{
			return false;
		}

		if (buildingPlacer?.IsBuildModeActive == true)
		{
			return false;
		}

		return runManager == null || runManager.CurrentPhase is RunPhase.Day or RunPhase.Night;
	}

	private bool CanHoverNow()
	{
		return CanSelectNow();
	}

	private void UpdateHover()
	{
		if (!CanHoverNow())
		{
			SetHoveredNode(null);
			return;
		}

		SetHoveredNode(FindSelectableAt(GetGlobalMousePosition()));
	}

	private Node FindSelectableAt(Vector2 worldPosition)
	{
		Node closestNode = null;
		float closestDistanceSquared = float.MaxValue;

		Core core = GetTree().Root.FindChild("Core", true, false) as Core;
		TryUseCandidate(core, worldPosition, coreSelectionRadius, ref closestNode, ref closestDistanceSquared);

		foreach (Node node in GetTree().GetNodesInGroup("Buildings"))
		{
			if (node is Building building)
			{
				TryUseCandidate(building, worldPosition, buildingSelectionRadius, ref closestNode, ref closestDistanceSquared);
			}
		}

		return closestNode;
	}

	private static void TryUseCandidate(
		Node2D candidate,
		Vector2 worldPosition,
		float selectionRadius,
		ref Node closestNode,
		ref float closestDistanceSquared)
	{
		if (candidate is not IInspectable inspectable ||
		    !IsInstanceValid(candidate) ||
		    !inspectable.IsSelectable)
		{
			return;
		}

		float distanceSquared = candidate.GlobalPosition.DistanceSquaredTo(worldPosition);
		float radiusSquared = selectionRadius * selectionRadius;
		if (distanceSquared > radiusSquared || distanceSquared >= closestDistanceSquared)
		{
			return;
		}

		closestDistanceSquared = distanceSquared;
		closestNode = candidate;
	}

	private void SelectNode(Node node)
	{
		if (selectedNode == node)
		{
			return;
		}

		selectedInspectable?.SetSelected(false);
		selectedNode = node;
		selectedInspectable = node as IInspectable;
		selectedInspectable?.SetSelected(true);
		EmitSignal(SignalName.SelectionChanged, selectedNode);
	}

	private void SetHoveredNode(Node node)
	{
		if (hoveredNode == node)
		{
			return;
		}

		hoveredNode = node;
		hoveredInspectable = node as IInspectable;
		EmitSignal(SignalName.HoverChanged, hoveredNode);
	}
}
