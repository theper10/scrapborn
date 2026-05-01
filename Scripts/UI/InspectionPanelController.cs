using Godot;

public partial class InspectionPanelController : CanvasLayer
{
	private SelectionController selectionController;
	private Control root;
	private Label titleLabel;
	private Label bodyLabel;
	private IInspectable selectedInspectable;

	public override void _Ready()
	{
		root = GetNode<Control>("Root");
		titleLabel = GetNode<Label>("Root/Panel/VBox/TitleLabel");
		bodyLabel = GetNode<Label>("Root/Panel/VBox/BodyLabel");
		selectionController = GetParent()?.GetNodeOrNull<SelectionController>("SelectionController");
		if (selectionController != null)
		{
			selectionController.SelectionChanged += OnSelectionChanged;
			OnSelectionChanged(selectionController.SelectedNode);
		}
		else
		{
			root.Visible = false;
			GD.PushWarning("InspectionPanel could not find the SelectionController node.");
		}
	}

	public override void _ExitTree()
	{
		if (selectionController != null)
		{
			selectionController.SelectionChanged -= OnSelectionChanged;
		}
	}

	public override void _Process(double delta)
	{
		Refresh();
	}

	private void OnSelectionChanged(Node selectedNode)
	{
		selectedInspectable = selectedNode as IInspectable;
		Refresh();
	}

	private void Refresh()
	{
		if (selectedInspectable == null || !selectedInspectable.IsSelectable)
		{
			root.Visible = false;
			return;
		}

		root.Visible = true;
		titleLabel.Text = selectedInspectable.InspectableName;
		bodyLabel.Text = selectedInspectable.GetInspectionText();
	}
}
