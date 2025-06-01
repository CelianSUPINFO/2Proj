using System.Collections.Generic;
using UnityEngine;


// Gère la liste de tous les bâtiments du jeu (données de type ScriptableObject)
// Permet de charger, débloquer et vérifier l’état de chaque bâtiment.

public static class BuildingManager
{
    // Liste interne contenant tous les ScriptableObject BuildingData
    private static List<BuildingData> allBuildingData;

    // Charge tous les BuildingData depuis le dossier Resources/Buildings
    // Cette méthode doit être appelée avant d’utiliser les autres fonctions.
    public static void LoadAllBuildings()
    {
        // Charge tous les assets BuildingData dans le dossier "Resources/Buildings"
        allBuildingData = new List<BuildingData>(Resources.LoadAll<BuildingData>("Buildings"));
        Debug.Log($"[BuildingManager] {allBuildingData.Count} bâtiments chargés.");
    }

    // Déverrouille un bâtiment à partir de son nom (buildingName)
        public static void UnlockBuilding(string buildingId)
    {
        try
        {
            if (string.IsNullOrEmpty(buildingId))
            {
                Debug.LogWarning("[UnlockBuilding] ID de bâtiment vide !");
                return;
            }

            // Recharge les données si jamais la liste est vide
            if (allBuildingData == null || allBuildingData.Count == 0)
                LoadAllBuildings();

            // Recherche du bâtiment par son nom
            BuildingData building = allBuildingData.Find(b => b.buildingName == buildingId);
            if (building != null)
            {
                building.locked = false;
                Debug.Log($"Bâtiment débloqué : {buildingId}");
            }
            else
            {
                Debug.LogWarning($"Bâtiment introuvable : {buildingId}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erreur dans UnlockBuilding({buildingId}) : {e.Message}");
        }
    }

    // Vérifie si un bâtiment est déjà débloqué
    public static bool IsBuildingUnlocked(string buildingId)
    {
        if (string.IsNullOrEmpty(buildingId))
            return false;

        // Recharge la liste si vide
        if (allBuildingData == null || allBuildingData.Count == 0)
            LoadAllBuildings();

        BuildingData building = allBuildingData.Find(b => b.buildingName == buildingId);
        return building != null && !building.locked;
    }
}
