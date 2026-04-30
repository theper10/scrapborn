using Godot;

public partial class ResourceProducerBuilding : Building
{
	[Export]
	private ResourceType outputType = ResourceType.Scrap;

	[Export]
	private int outputAmount = 2;

	[Export]
	private float productionInterval = 2f;

	private double productionTimer;

	protected virtual ResourceType OutputType => outputType;
	protected int OutputAmount => outputAmount;

	public override void _Process(double delta)
	{
		productionTimer += delta;
		if (productionTimer < productionInterval)
		{
			return;
		}

		productionTimer = 0.0;
		TryProduce();
	}

	protected virtual bool CanProduce()
	{
		return ResourceManager != null;
	}

	protected virtual void TryProduce()
	{
		if (!CanProduce())
		{
			SetStatus(BuildingStatus.InvalidPlacement);
			return;
		}

		if (ResourceManager.IsFull(OutputType))
		{
			SetStatus(BuildingStatus.OutputFull);
			return;
		}

		ResourceManager.AddResource(OutputType, outputAmount);
		SetStatus(BuildingStatus.Working);
	}
}
