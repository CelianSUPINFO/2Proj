using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuildingBarUI : MonoBehaviour
{
    [Header("Références UI")]
    public GameObject buildingButtonPrefab;
    public Transform buildingBarPanel;

    [Header("Données")]
    public List<BuildingData> allBuildings;
    private List<GameObject> currentButtons = new();

    public void SelectAge(int ageIndex)
    {
        GameAge selectedAge = (GameAge)ageIndex;
        RefreshBar(selectedAge);
    }

    void RefreshBar(GameAge selectedAge)
    {
        foreach (GameObject btn in currentButtons)
            Destroy(btn);
        currentButtons.Clear();

        List<BuildingData> buildings = allBuildings.FindAll(b => b.unlockAge.ToString() == selectedAge.ToString());
        Debug.Log("Bâtiments trouvés : " + buildings.Count + " pour l'âge " + selectedAge);

        foreach (BuildingData building in buildings)
        {
            GameObject btn = Instantiate(buildingButtonPrefab, buildingBarPanel);

            Image icon = btn.GetComponent<Image>();
            if (icon != null && building.icon != null)
                icon.sprite = building.icon;

            Button button = btn.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() =>
                {
                    FindObjectOfType<SimpleBuildingPlacer>().SelectBuildingByData(building);
                });
            }

            currentButtons.Add(btn);
        }
    }
}
