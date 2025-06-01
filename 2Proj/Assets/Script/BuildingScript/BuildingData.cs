using UnityEngine;

[CreateAssetMenu(fileName = "NewBuilding", menuName = "Game/Building")]
public class BuildingData : ScriptableObject
{
    public string buildingName;
    public GameObject prefab;
    public GameAge unlockAge;
    public bool locked = true; // Par défaut : verrouillé
    public Sprite icon;
    public BuildingFunction function;
    public BuildingCost cost;
    public int capacity;
    [TextArea] public string description;

    private void OnEnable()
    {
        // Déverrouiller automatiquement certaines constructions spécifiques
        if (buildingName == "Cabane en pierre" || buildingName == "Stockage")
        {
            locked = false;
        }
    }
}
