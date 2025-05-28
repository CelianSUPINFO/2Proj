using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;


public class BuildingBarUI : MonoBehaviour
{
    [Header("Références UI")]
    public GameObject buildingButtonPrefab;
    public Transform buildingBarPanel;
    public TMP_Text errorText; // <-- à assigner dans l'inspecteur

    [Header("Données")]
    public List<BuildingData> allBuildings;
    private List<GameObject> currentButtons = new();

    private Coroutine errorCoroutine;


    void Start()
    {
        GameAge currentAge = AgeManager.Instance.GetCurrentAge();
        RefreshBar(currentAge);
    }


    public void SelectAge(int ageIndex)
    {
        GameAge selectedAge = (GameAge)ageIndex;
        GameAge currentAge = AgeManager.Instance.GetCurrentAge();

        if ((int)selectedAge <= (int)currentAge)
        {
            RefreshBar(selectedAge);
        }
        else
        {
            ShowErrorMessage("Vous n'avez pas atteint cet âge !");
        }
    }

    public void RefreshBar(GameAge selectedAge)
    {
        foreach (GameObject btn in currentButtons)
            Destroy(btn);
        currentButtons.Clear();

        List<BuildingData> buildings = allBuildings.FindAll(b => b.unlockAge == selectedAge && !b.locked);
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
                    FindFirstObjectByType<SimpleBuildingPlacer>().SelectBuildingByData(building);
                });
            }

            // ✅ Tooltip : ajouter EventTrigger
            EventTrigger trigger = btn.AddComponent<EventTrigger>();

            // PointerEnter → Affiche le tooltip
            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((_) =>
            {
                Vector3 mousePos = Input.mousePosition;
                BuildingTooltipUI.Instance.ShowTooltip(building);
            });
            trigger.triggers.Add(enterEntry);

            // PointerExit → Cache le tooltip
            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((_) =>
            {
                BuildingTooltipUI.Instance.HideTooltip();
            });
            trigger.triggers.Add(exitEntry);

            currentButtons.Add(btn);
        }

    }

    void ShowErrorMessage(string message)
    {
        if (errorCoroutine != null)
            StopCoroutine(errorCoroutine);

        errorCoroutine = StartCoroutine(ShowTemporaryMessage(message, 2f));
    }

    IEnumerator ShowTemporaryMessage(string message, float duration)
    {
        errorText.text = message;
        errorText.gameObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        errorText.gameObject.SetActive(false);
    }
}
