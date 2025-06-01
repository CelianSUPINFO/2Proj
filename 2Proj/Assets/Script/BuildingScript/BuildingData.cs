using UnityEngine;

// stocke les infos d’un type de bâtiment (modèle, coût, âge, etc.)
[CreateAssetMenu(fileName = "NewBuilding", menuName = "Game/Building")]
public class BuildingData : ScriptableObject
{
    public string buildingName; // Nom du bâtiment 
    public GameObject prefab; // Le prefab à instancier en jeu
    public GameAge unlockAge; // Âge nécessaire pour débloquer le bâtiment
    public bool locked = true; // Si le bâtiment est verrouillé au départ
    public Sprite icon; // Icône à afficher dans l’UI
    public BuildingFunction function; // Fonction du bâtiment 
    public BuildingCost cost; // Coût de construction 
    public int capacity; // Capacité maximale 
    [TextArea] public string description; // Texte de description dans l’UI

    // Déverrouille certains bâtiments par défaut
    private void OnEnable()
    {
        if (buildingName == "Cabane en pierre" || buildingName == "Stockage")
        {
            locked = false;
        }
    }
}
