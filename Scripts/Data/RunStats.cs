using System.Collections.Generic;

public sealed class RunStats
{
	private readonly List<string> chosenUpgradeNames = new();

	public int EnemiesKilled { get; private set; }
	public int BuildingsPlaced { get; private set; }
	public int ScrapGatheredManually { get; private set; }
	public int ScrapProducedByDrills { get; private set; }
	public int EnergyProduced { get; private set; }
	public int AmmoProduced { get; private set; }
	public int UpgradesChosen { get; private set; }
	public int NightsSurvived { get; private set; }
	public int BuildingsDestroyed { get; private set; }
	public int ScrapSpentOnRepairs { get; private set; }
	public int HealthRepaired { get; private set; }
	public IReadOnlyList<string> ChosenUpgradeNames => chosenUpgradeNames;

	public void Reset()
	{
		chosenUpgradeNames.Clear();
		EnemiesKilled = 0;
		BuildingsPlaced = 0;
		ScrapGatheredManually = 0;
		ScrapProducedByDrills = 0;
		EnergyProduced = 0;
		AmmoProduced = 0;
		UpgradesChosen = 0;
		NightsSurvived = 0;
		BuildingsDestroyed = 0;
		ScrapSpentOnRepairs = 0;
		HealthRepaired = 0;
	}

	public void RecordEnemyKilled()
	{
		EnemiesKilled++;
	}

	public void RecordBuildingPlaced()
	{
		BuildingsPlaced++;
	}

	public void RecordScrapGatheredManually(int amount)
	{
		ScrapGatheredManually += amount;
	}

	public void RecordScrapProducedByDrill(int amount)
	{
		ScrapProducedByDrills += amount;
	}

	public void RecordEnergyProduced(int amount)
	{
		EnergyProduced += amount;
	}

	public void RecordAmmoProduced(int amount)
	{
		AmmoProduced += amount;
	}

	public void RecordUpgradeChosen(string upgradeName = "")
	{
		UpgradesChosen++;
		if (!string.IsNullOrWhiteSpace(upgradeName))
		{
			chosenUpgradeNames.Add(upgradeName);
		}
	}

	public void RecordNightSurvived()
	{
		NightsSurvived++;
	}

	public void RecordBuildingDestroyed()
	{
		BuildingsDestroyed++;
	}

	public void RecordRepair(int scrapSpent, int healthRepaired)
	{
		ScrapSpentOnRepairs += scrapSpent;
		HealthRepaired += healthRepaired;
	}

	public string ToDebugString()
	{
		return $"Stats: Kills {EnemiesKilled} | Buildings {BuildingsPlaced} | Lost {BuildingsDestroyed} | Manual Scrap {ScrapGatheredManually} | Drill Scrap {ScrapProducedByDrills} | Energy {EnergyProduced} | Ammo {AmmoProduced} | Repairs {HealthRepaired} HP/{ScrapSpentOnRepairs} Scrap | Upgrades {UpgradesChosen} | Nights {NightsSurvived}";
	}
}
