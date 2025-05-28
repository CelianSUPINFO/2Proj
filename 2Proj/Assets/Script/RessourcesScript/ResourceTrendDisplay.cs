using TMPro;
using UnityEngine;

public class ResourceTrendDisplay : MonoBehaviour
{
    public ResourceType resourceType;
    public TMP_Text targetText;
    private ResourceTrendTracker tracker;

    void Start()
    {
        tracker = FindObjectOfType<ResourceTrendTracker>();
    }

    void Update()
    {
        if (tracker != null)
        {
            float trend = tracker.GetTrend(resourceType);
            string nomFrancais = GetNomFrancais(resourceType);
            string tendance = $"{(trend >= 0 ? "+" : "")}{trend:F1}/min";

            targetText.text = $"{nomFrancais} : {tendance}";
        }
    }

    string GetNomFrancais(ResourceType type)
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
            _ => type.ToString()
        };
    }
}
