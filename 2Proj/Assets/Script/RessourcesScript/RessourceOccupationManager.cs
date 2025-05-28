using UnityEngine;
using System.Collections.Generic;

public static class RessourceOccupationManager
{
    public static HashSet<GameObject> ressourcesOccupees = new HashSet<GameObject>();

    public static bool EstOccupe(GameObject ressource) => ressourcesOccupees.Contains(ressource);

    public static void Occuper(GameObject ressource)
    {
        ressourcesOccupees.Add(ressource);
    }

    public static void Liberer(GameObject ressource)
    {
        ressourcesOccupees.Remove(ressource);
    }
}