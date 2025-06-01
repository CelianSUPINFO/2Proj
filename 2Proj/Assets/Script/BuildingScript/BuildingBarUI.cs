using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;

// affichage des boutons de bâtiments selon l’âge débloqué
public class BuildingBarUI : MonoBehaviour
{
    [Header("Références UI")]
    public GameObject buildingButtonPrefab; // Prefab de bouton à instancier pour chaque bâtiment
    public Transform buildingBarPanel; // Panel qui contient les boutons
    public TMP_Text errorText; // Texte d’erreur si l’âge est insuffisant

    [Header("Données")]
    public List<BuildingData> allBuildings; // Liste de tous les bâtiments du jeu
    private List<GameObject> currentButtons = new(); // Liste des boutons actuellement affichés
    private Coroutine errorCoroutine;

    void Start()
    {
        // Affiche les bâtiments disponibles pour l’âge actuel
        GameAge currentAge = AgeManager.Instance.GetCurrentAge();
        RefreshBar(currentAge);
    }

    // Méthode appelée quand on sélectionne un âge (via un bouton ou dropdown)
    public void SelectAge(int ageIndex)
    {
        GameAge selectedAge = (GameAge)ageIndex;
        GameAge currentAge = AgeManager.Instance.GetCurrentAge();

        if ((int)selectedAge <= (int)currentAge)
            RefreshBar(selectedAge);
        else
            ShowErrorMessage("Vous n'avez pas atteint cet âge !");
    }

    // Met à jour la liste des bâtiments affichés en fonction de l’âge sélectionné
    public void RefreshBar(GameAge selectedAge)
    {
        // Nettoie les anciens boutons
        foreach (GameObject btn in currentButtons)
            Destroy(btn);
        currentButtons.Clear();

        // Filtre les bâtiments disponibles et déverrouillés pour cet âge
        List<BuildingData> buildings = allBuildings.FindAll(b => b.unlockAge == selectedAge && !b.locked);
        Debug.Log("Bâtiments trouvés : " + buildings.Count + " pour l'âge " + selectedAge);

        // Crée les nouveaux boutons
        foreach (BuildingData building in buildings)
        {
            GameObject btn = Instantiate(buildingButtonPrefab, buildingBarPanel);

            // Applique l’icône du bâtiment sur le bouton
            Image icon = btn.GetComponent<Image>();
            if (icon != null && building.icon != null)
                icon.sprite = building.icon;

            // Ajoute une action quand on clique sur le bouton
            Button button = btn.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() =>
                {
                    FindFirstObjectByType<SimpleBuildingPlacer>().SelectBuildingByData(building);
                });
            }

            // Gestion du tooltip quand on survole
            EventTrigger trigger = btn.AddComponent<EventTrigger>();

            // Quand on survole le bouton
            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener((_) => BuildingTooltipUI.Instance.ShowTooltip(building));
            trigger.triggers.Add(enterEntry);

            // Quand on quitte le bouton
            var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener((_) => BuildingTooltipUI.Instance.HideTooltip());
            trigger.triggers.Add(exitEntry);

            currentButtons.Add(btn);
        }
    }

    // Affiche un message d’erreur temporairement
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
