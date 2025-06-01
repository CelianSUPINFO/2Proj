using UnityEngine;
using TMPro;


// UI qui affiche les détails d’un bâtiment lorsqu’on le survole dans l’interface.
public class BuildingTooltipUI : MonoBehaviour
{
    public static BuildingTooltipUI Instance; // Instance unique du tooltip pour accès global

    [Header("Références UI")]
    public GameObject panel;          // Panneau contenant les textes (à activer/désactiver)
    public TMP_Text nameText;         // Texte du nom du bâtiment
    public TMP_Text descriptionText;  // Texte de description
    public TMP_Text costText;         // Texte listant les coûts en ressources


    // Initialise le singleton et cache le panneau au démarrage.
    void Awake()
    {
        Instance = this;
        HideTooltip(); // Le panneau est caché par défaut
    }


    // Affiche le tooltip avec les informations du BuildingData donné.
    public void ShowTooltip(BuildingData data)
    {
        panel.SetActive(true); // Affiche le panneau

        // Affiche le nom et la description
        nameText.text = data.buildingName;
        descriptionText.text = data.description;

        // Construit la chaîne des coûts (ressources nécessaires)
        string costString = "";
        foreach (var cost in data.cost.resourceCosts)
        {
            costString += $"{cost.type}: {cost.amount}\n";
        }
        costText.text = costString.Trim(); // Trim pour enlever le dernier retour à la ligne
    }

  
    // Cache complètement le panneau de tooltip.
    public void HideTooltip()
    {
        panel.SetActive(false);
    }
}
