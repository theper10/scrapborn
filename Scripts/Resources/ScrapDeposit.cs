using Godot;
using System;

public partial class ScrapDeposit : Area2D, IInspectable
{
	[Signal]
	public delegate void DepletedEventHandler(ScrapDeposit deposit);

	[Export]
	public int StartingAmount { get; set; } = 1000;

	private Node2D rock;
	private Node2D metalChunkA;
	private Node2D metalChunkB;
	private Label amountLabel;
	private Line2D selectionHighlight;
	private bool hasDepleted;

	public int CurrentAmount { get; private set; }
	public bool IsEmpty => CurrentAmount <= 0;
	public string InspectableName => "Scrap Deposit";
	public bool IsSelectable => !IsEmpty && Visible;

	public override void _Ready()
	{
		AddToGroup("ScrapDeposits");
		rock = GetNodeOrNull<Node2D>("Rock");
		metalChunkA = GetNodeOrNull<Node2D>("MetalChunkA");
		metalChunkB = GetNodeOrNull<Node2D>("MetalChunkB");
		amountLabel = GetNodeOrNull<Label>("AmountLabel");
		CreateSelectionHighlight();
		CurrentAmount = Math.Max(0, StartingAmount);
		hasDepleted = IsEmpty;
		UpdateVisualState(false);
	}

	public int Gather(int amount)
	{
		return Extract(amount);
	}

	public int Extract(int amount)
	{
		if (amount <= 0 || IsEmpty)
		{
			return 0;
		}

		int gatheredAmount = Math.Min(amount, CurrentAmount);
		CurrentAmount -= gatheredAmount;
		UpdateVisualState();
		return gatheredAmount;
	}

	public void ResetAmount(int amount)
	{
		StartingAmount = Math.Max(0, amount);
		CurrentAmount = StartingAmount;
		hasDepleted = IsEmpty;
		SetSelected(false);
		UpdateVisualState(false);
	}

	public void SetSelected(bool selected)
	{
		if (selectionHighlight != null)
		{
			selectionHighlight.Visible = selected && IsSelectable;
		}
	}

	public string GetHoverText()
	{
		return $"Scrap Deposit\nScrap: {CurrentAmount} / {StartingAmount}\nStatus: {GetStatusText()}";
	}

	public string GetInspectionText()
	{
		return
			$"Scrap: {CurrentAmount} / {StartingAmount}\n" +
			$"Status: {GetStatusText()}\n" +
			"Gather: Hold E nearby";
	}

	private void UpdateVisualState(bool announceDepletion = true)
	{
		Visible = !IsEmpty;
		Monitoring = !IsEmpty;
		Monitorable = !IsEmpty;
		if (selectionHighlight != null && IsEmpty)
		{
			selectionHighlight.Visible = false;
		}

		if (amountLabel != null)
		{
			amountLabel.Text = $"{CurrentAmount}/{StartingAmount}";
			amountLabel.Visible = !IsEmpty;
		}

		if (IsEmpty)
		{
			if (!hasDepleted)
			{
				hasDepleted = true;
				if (announceDepletion)
				{
					FeedbackEffects.SpawnText(
						this,
						GlobalPosition,
						"Deposit depleted",
						FeedbackEffects.WarningColor,
						FeedbackCategory.Status,
						0.5f,
						$"{GetInstanceId()}:depleted");
					GetNodeOrNull<RunManager>("/root/RunManager")?.RecordDepositDepleted();
					EmitSignal(SignalName.Depleted, this);
				}
			}

			return;
		}

		float remainingRatio = StartingAmount <= 0 ? 0f : CurrentAmount / (float)StartingAmount;
		Color tint = remainingRatio < 0.25f
			? new Color(0.72f, 0.62f, 0.46f, 0.9f)
			: Colors.White;
		Modulate = tint;

		float visualScale = Mathf.Lerp(0.74f, 1f, remainingRatio);
		if (rock != null)
		{
			rock.Scale = Vector2.One * visualScale;
		}

		if (metalChunkA != null)
		{
			metalChunkA.Scale = Vector2.One * Mathf.Lerp(0.72f, 1f, remainingRatio);
		}

		if (metalChunkB != null)
		{
			metalChunkB.Scale = Vector2.One * Mathf.Lerp(0.72f, 1f, remainingRatio);
		}
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
				new Vector2(-33, -33),
				new Vector2(33, -33),
				new Vector2(33, 33),
				new Vector2(-33, 33)
			}
		};

		AddChild(selectionHighlight);
	}

	private string GetStatusText()
	{
		if (IsEmpty)
		{
			return "Depleted";
		}

		return StartingAmount > 0 && CurrentAmount <= Mathf.CeilToInt(StartingAmount * 0.25f)
			? "Low"
			: "Available";
	}
}
