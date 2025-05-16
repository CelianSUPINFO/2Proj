using UnityEngine;

[CreateAssetMenu(fileName = "NewBuilding", menuName = "Game/Building")]
public class BuildingData : ScriptableObject
{
    public string buildingName;
    public GameObject prefab;
    public GameAge unlockAge;
    public Sprite icon;
    public BuildingFunction function;
    public BuildingCost cost;
    public int capacity; 
    [TextArea]
    public string description;
}
