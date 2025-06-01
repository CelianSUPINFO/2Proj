using System.Collections.Generic;
using UnityEngine;

public static class BuildingManager
{
    private static List<BuildingData> allBuildingData;

    /// <summary>
    /// Charge tous les BuildingData depuis Resources/Buildings
    /// </summary>
    public static void LoadAllBuildings()
    {
        allBuildingData = new List<BuildingData>(Resources.LoadAll<BuildingData>("Buildings"));
        Debug.Log($"[BuildingManager] {allBuildingData.Count} bâtiments chargés.");
    }


    /// <summary>
    /// Déverrouille un bâtiment en utilisant son nom
    /// </summary>
    public static void UnlockBuilding(string buildingId)
    {
        try
        {
            if (string.IsNullOrEmpty(buildingId))
            {
                Debug.LogWarning("[UnlockBuilding] ID de bâtiment vide !");
                return;
            }

            if (allBuildingData == null || allBuildingData.Count == 0)
                LoadAllBuildings();

            BuildingData building = allBuildingData.Find(b => b.buildingName == buildingId);
            if (building != null)
            {
                building.locked = false;
                Debug.Log($"✅ Bâtiment débloqué : {buildingId}");
            }
            else
            {
                Debug.LogWarning($"❌ Bâtiment introuvable : {buildingId}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"⚠️ Erreur dans UnlockBuilding({buildingId}) : {e.Message}");
        }
    }

    /// <summary>
    /// Vérifie si un bâtiment est déjà débloqué
    /// </summary>
    public static bool IsBuildingUnlocked(string buildingId)
    {
        if (string.IsNullOrEmpty(buildingId))
            return false;

        if (allBuildingData == null || allBuildingData.Count == 0)
            LoadAllBuildings();

        BuildingData building = allBuildingData.Find(b => b.buildingName == buildingId);
        return building != null && !building.locked;
    }
}
