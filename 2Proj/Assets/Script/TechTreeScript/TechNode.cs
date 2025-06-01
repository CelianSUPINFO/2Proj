using System;
using System.Collections.Generic;
using UnityEngine;

// Représente un nœud de recherche dans l'arbre technologique
[Serializable] // Permet de voir/modifier cette classe dans l’inspecteur Unity
public class TechNode
{
    public string id; // Identifiant unique de la technologie (ex: "agriculture", "forge", etc.)
    public string displayName; // Nom affiché à l’écran dans l’UI
    public Sprite icon; // Icône représentant la technologie
    public int cost; // Coût en points de recherche pour débloquer cette technologie
    public List<string> prerequisiteIds; // Liste des ID de technologies requises avant celle-ci
    public bool unlocked = false; // Indique si cette technologie a déjà été débloquée
}
