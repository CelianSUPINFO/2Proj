using TMPro;
using UnityEngine;

public class ResourceDisplay : MonoBehaviour
{
    public ResourceType resourceType;
    public TMP_Text targetText;

    void Start()
    {
        ResourceManager.Instance.onResourceChanged += UpdateDisplay;
        UpdateDisplay(); 
    }

    void OnDestroy()
    {
        ResourceManager.Instance.onResourceChanged -= UpdateDisplay;
    }

    void UpdateDisplay()
    {
        int amount = ResourceManager.Instance.Get(resourceType);
        string name = GetDisplayName(resourceType);
        targetText.text = $"{name} : {amount}";
    }

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
            _ => type.ToString()
        };
    }
}
