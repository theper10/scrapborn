using Godot;

public partial class RunSummaryPanelController : CanvasLayer
{
	private RunManager runManager;
	private Control root;
	private Label titleLabel;
	private Label flavorLabel;
	private Label statsLabel;

	public override void _Ready()
	{
		CacheNodes();
		ConnectRunManager();
		root.Visible = false;
	}

	public override void _ExitTree()
	{
		if (runManager != null)
		{
			runManager.RunStateChanged -= OnRunStateChanged;
		}
	}

	private void CacheNodes()
	{
		root = GetNode<Control>("Root");
		titleLabel = GetNode<Label>("Root/Panel/VBox/TitleLabel");
		flavorLabel = GetNodeOrNull<Label>("Root/Panel/VBox/FlavorLabel");
		statsLabel = GetNode<Label>("Root/Panel/VBox/StatsLabel");
	}

	private void ConnectRunManager()
	{
		runManager = GetNodeOrNull<RunManager>("/root/RunManager");
		if (runManager == null)
		{
			GD.PushWarning("RunSummaryPanel could not find the RunManager autoload.");
			return;
		}

		runManager.RunStateChanged += OnRunStateChanged;
	}

	private void OnRunStateChanged(string phaseText, string detailText, string messageText, bool isRunOver)
	{
		root.Visible = isRunOver;
		if (!isRunOver || runManager == null)
		{
			return;
		}

		titleLabel.Text = phaseText;
		if (flavorLabel != null)
		{
			flavorLabel.Text = phaseText == "Victory"
				? "The factory survives... for now."
				: "The machines overran the core.";
		}

		statsLabel.Text = BuildStatsText(runManager.Stats);
	}

	private static string BuildStatsText(RunStats stats)
	{
		string upgradeNames = stats.ChosenUpgradeNames.Count > 0
			? $"\nChosen upgrades: {string.Join(", ", stats.ChosenUpgradeNames)}"
			: string.Empty;

		return
			$"Nights survived: {stats.NightsSurvived}\n" +
			$"Enemies killed: {stats.EnemiesKilled}\n" +
			$"Buildings placed: {stats.BuildingsPlaced}\n" +
			$"Buildings sold: {stats.BuildingsSold}\n" +
			$"Buildings destroyed: {stats.BuildingsDestroyed}\n" +
			$"Deposits depleted: {stats.DepositsDepleted}\n" +
			$"Scrap mined manually: {stats.ScrapGatheredManually}\n" +
			$"Scrap mined by Drills: {stats.ScrapProducedByDrills}\n" +
			$"Refunded: {stats.ScrapRefunded} Scrap, {stats.EnergyRefunded} Energy, {stats.AmmoRefunded} Ammo\n" +
			$"Energy produced: {stats.EnergyProduced}\n" +
			$"Ammo produced: {stats.AmmoProduced}\n" +
			$"Scrap spent on repairs: {stats.ScrapSpentOnRepairs}\n" +
			$"Health repaired: {stats.HealthRepaired}\n" +
			$"Upgrades chosen: {stats.UpgradesChosen}" +
			upgradeNames +
			"\n\n" +
			"Press R to restart\n" +
			"Press M for Main Menu";
	}
}
