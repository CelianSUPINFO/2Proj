using System.Collections.Generic;
using UnityEngine;

// Permet de créer facilement un objet "TechTreeData" depuis l'éditeur Unity (clic droit dans le projet)
[CreateAssetMenu(menuName = "TechTree/TechTreeData")]
public class TechTreeData : ScriptableObject
{
    // Liste de tous les nœuds de technologie dans l'arbre de recherche
    public List<TechNode> techNodes;
}
