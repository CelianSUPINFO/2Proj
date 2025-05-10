using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public GameAge currentAge = GameAge.StoneAge;

    [Header("Liste complète")]
    public List<BuildingData> allBuildings;

    [Header("Débloqués selon l'âge")]
    public List<BuildingData> unlockedBuildings;

    void Start()
    {
        UpdateUnlockedBuildings();
    }

    public void AdvanceAge()
    {
        currentAge++;
        UpdateUnlockedBuildings();
    }

    void UpdateUnlockedBuildings()
    {
        unlockedBuildings = allBuildings.FindAll(b => b.unlockAge <= currentAge);
        Debug.Log("✅ Bâtiments débloqués : " + unlockedBuildings.Count);
    }

    public bool CanBuild(BuildingData data)
    {
        return ResourceManager.Instance.HasEnough(ResourceType.Wood, data.cost.wood)
            && ResourceManager.Instance.HasEnough(ResourceType.Stone, data.cost.stone)
            && ResourceManager.Instance.HasEnough(ResourceType.Iron, data.cost.iron)
            && ResourceManager.Instance.HasEnough(ResourceType.Clay, data.cost.clay)
            && ResourceManager.Instance.HasEnough(ResourceType.Gold, data.cost.gold)
            && ResourceManager.Instance.HasEnough(ResourceType.Population, data.cost.population);
    }
}
