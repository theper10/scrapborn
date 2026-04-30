public sealed class RunStats
{
	public int EnemiesKilled { get; private set; }
	public int BuildingsPlaced { get; private set; }
	public int ScrapGatheredManually { get; private set; }
	public int ScrapProducedByDrills { get; private set; }
	public int EnergyProduced { get; private set; }
	public int AmmoProduced { get; private set; }
	public int UpgradesChosen { get; private set; }
	public int NightsSurvived { get; private set; }

	public void Reset()
	{
		EnemiesKilled = 0;
		BuildingsPlaced = 0;
		ScrapGatheredManually = 0;
		ScrapProducedByDrills = 0;
		EnergyProduced = 0;
		AmmoProduced = 0;
		UpgradesChosen = 0;
		NightsSurvived = 0;
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

	public void RecordUpgradeChosen()
	{
		UpgradesChosen++;
	}

	public void RecordNightSurvived()
	{
		NightsSurvived++;
	}

	public string ToDebugString()
	{
		return $"Stats: Kills {EnemiesKilled} | Buildings {BuildingsPlaced} | Manual Scrap {ScrapGatheredManually} | Drill Scrap {ScrapProducedByDrills} | Energy {EnergyProduced} | Ammo {AmmoProduced} | Upgrades {UpgradesChosen} | Nights {NightsSurvived}";
	}
}
