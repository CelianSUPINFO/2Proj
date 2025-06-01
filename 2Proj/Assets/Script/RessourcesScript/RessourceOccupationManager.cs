using UnityEngine;
using System.Collections.Generic;

// Classe statique pour gérer quelles ressources sont actuellement "occupées" (coupées, exploitées, etc.)
public static class RessourceOccupationManager
{
    // Liste des ressources actuellement occupées. On utilise un HashSet pour avoir des recherches rapides.
    public static HashSet<GameObject> ressourcesOccupees = new HashSet<GameObject>();

    // Vérifie si une ressource est déjà occupée (retourne true si oui)
    public static bool EstOccupe(GameObject ressource) => ressourcesOccupees.Contains(ressource);

    // Marque une ressource comme occupée (par exemple : un personnage va couper cet arbre)
    public static void Occuper(GameObject ressource)
    {
        ressourcesOccupees.Add(ressource);
    }

    // Libère une ressource (par exemple : après la collecte ou si le personnage change de cible)
    public static void Liberer(GameObject ressource)
    {
        ressourcesOccupees.Remove(ressource);
    }
}
