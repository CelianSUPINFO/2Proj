using UnityEngine;

[CreateAssetMenu(fileName = "NewBuilding", menuName = "Game/Building")]
public class BuildingData : ScriptableObject
{
    public string buildingName;
    public GameObject prefab;
    public GameAge unlockAge;
    public BuildingCost cost;
    [TextArea]
    public string description;
}
