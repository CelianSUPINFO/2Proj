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
            ResourceType.Iron => "Fer",
            ResourceType.Clay => "Argile",
            ResourceType.Gold => "Or",
            ResourceType.Food => "Nourriture",
            ResourceType.Water => "Eau",
            ResourceType.Leather => "Cuir",
            ResourceType.Plank => "Planches",
            ResourceType.Brick => "Briques",
            ResourceType.IronIngot => "Lingots de fer",
            ResourceType.GoldIngot => "Lingots d'or",
            ResourceType.Population => "Population",
            _ => type.ToString()
        };
    }
}
