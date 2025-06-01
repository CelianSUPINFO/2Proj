using TMPro;             // Pour afficher du texte stylisé dans l’UI avec TextMeshPro
using UnityEngine;

public class ResourceTrendDisplay : MonoBehaviour
{
    public ResourceType resourceType;  // Type de ressource à afficher (ex : bois, pierre…)
    public TMP_Text targetText;        // Référence au composant TextMeshPro qui affichera la tendance
    private ResourceTrendTracker tracker; // Référence au script qui suit l’évolution des ressources

    void Start()
    {
        // Au lancement, on cherche automatiquement un objet dans la scène qui contient le tracker
        tracker = FindObjectOfType<ResourceTrendTracker>();
    }

    void Update()
    {
        // Si on a bien trouvé le tracker
        if (tracker != null)
        {
            // On récupère la tendance de la ressource (quantité gagnée ou perdue par minute)
            float trend = tracker.GetTrend(resourceType);

            // On convertit le nom de la ressource en français pour l’affichage
            string nomFrancais = GetNomFrancais(resourceType);

            // On formate le texte à afficher, avec un "+" si positif, et 1 chiffre après la virgule
            string tendance = $"{(trend >= 0 ? "+" : "")}{trend:F1}/min";

            // On met à jour le texte de l’UI (exemple : "Bois : +2.5/min")
            targetText.text = $"{nomFrancais} : {tendance}";
        }
    }

    // Cette méthode convertit un type de ressource (en anglais) en nom français
    string GetNomFrancais(ResourceType type)
    {
        // Utilisation du switch expression (plus compact que switch classique)
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

            // Par défaut, on affiche le nom brut de l’enum s’il n’est pas géré ci-dessus
            _ => type.ToString()
        };
    }
}
