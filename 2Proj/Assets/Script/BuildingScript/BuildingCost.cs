using System.Collections.Generic;

// liste des ressources nécessaires pour construire un bâtiment
[System.Serializable]
public class BuildingCost
{
    public List<ResourceAmount> resourceCosts; // Liste de ressources avec leur quantité
}
