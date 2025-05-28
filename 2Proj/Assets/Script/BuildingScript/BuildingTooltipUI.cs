using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class BuildingTooltipUI : MonoBehaviour
{
    public static BuildingTooltipUI Instance;

    public GameObject panel;
    public TMP_Text nameText;
    public TMP_Text descriptionText;
    public TMP_Text costText;

    void Awake()
    {
        Instance = this;
        HideTooltip();
    }

    public void ShowTooltip(BuildingData data)
    {
        panel.SetActive(true);

        nameText.text = data.buildingName;
        descriptionText.text = data.description;

        string costString = "";
        foreach (var cost in data.cost.resourceCosts)
        {
            costString += $"{cost.type}: {cost.amount}\n";
        }
        costText.text = costString.Trim();
    }


    public void HideTooltip()
    {
        panel.SetActive(false);
    }
}
