using TMPro;
using UnityEngine;

// Ce script permet d'afficher une ressource spécifique à l'écran (ex : Bois : 15)
public class ResourceDisplay : MonoBehaviour
{
    public ResourceType resourceType;       // Le type de ressource à afficher (bois, pierre, etc.)
    public TMP_Text targetText;             // Référence vers le champ texte dans l’UI

    void Start()
    {
        // On s'abonne à l'événement pour être prévenu quand une ressource change
        ResourceManager.Instance.onResourceChanged += UpdateDisplay;

        // Mise à jour immédiate à l'ouverture (au cas où la valeur a déjà changé)
        UpdateDisplay();
    }

    void OnDestroy()
    {
        // On se désabonne pour éviter les erreurs si l'objet est détruit
        ResourceManager.Instance.onResourceChanged -= UpdateDisplay;
    }

    // Cette méthode met à jour le texte affiché à l’écran
    void UpdateDisplay()
    {
        int amount = ResourceManager.Instance.Get(resourceType);     // On récupère la quantité actuelle
        string name = GetDisplayName(resourceType);                  // On traduit le nom en français
        targetText.text = $"{name} : {amount}";                      // Exemple affiché : "Bois : 30"
    }

    // Traduit le nom d’un type de ressource en français pour l’interface utilisateur
    private string GetDisplayName(ResourceType type)
    {
        return type switch
        {
            ResourceType.Wood => "Bois",
            ResourceType.Stone => "Pierre",
            ResourceType.Food => "Nourriture",
            ResourceType.Water => "Eau",
            ResourceType.Clay => "Argile",
            ResourceType.Wheat => "Blé",
            ResourceType.Fish => "Poisson",
            ResourceType.Plank => "Planches",
            ResourceType.Flour => "Farine",
            ResourceType.Bread => "Pain",
            ResourceType.Meat => "Viande",
            ResourceType.Leather => "Cuir",
            ResourceType.Brick => "Briques",
            ResourceType.Iron => "Fer",
            ResourceType.Tools => "Outils",
            ResourceType.Cloth => "Tissu",
            ResourceType.Gold => "Or",
            ResourceType.Population => "Population",
            ResourceType.Search => "Recherche",
            _ => type.ToString() // Par défaut, on garde le nom brut si pas prévu
        };
    }
}
