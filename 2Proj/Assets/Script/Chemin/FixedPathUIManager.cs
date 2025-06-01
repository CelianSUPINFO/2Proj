using UnityEngine;
using UnityEngine.UI;
// Gère l'interface utilisateur pour placer des chemins selon l'âge débloqué.
public class FixedPathUIManager : MonoBehaviour
{
    [System.Serializable]
    public class AgePathButton
    {
        public GameAge age;         // Âge requis pour débloquer ce bouton de chemin
        public Button button;       // Bouton UI
        public PathData pathData;   // Données du chemin lié
    }

    public GameObject pathButtonPanel;               // Panneau contenant les boutons
    public Button toggleMenuButton;                  // Bouton pour afficher/cacher le menu
    public PathPlacementManager pathPlacementManager; // Script qui place le chemin
    public AgePathButton[] ageButtons;               // Liste des boutons de chemins par âge

    private bool isPlacing = false;

    void Start()
    {
        // Quand on clique sur le bouton principal, on ouvre/ferme le menu
        toggleMenuButton.onClick.AddListener(TogglePathButtons);

        // On assigne à chaque bouton son action de placement
        foreach (var ageBtn in ageButtons)
        {
            var capturedPath = ageBtn.pathData;
            ageBtn.button.onClick.AddListener(() => StartPlacing(capturedPath));
        }

        UpdateButtons(); // Active seulement les boutons valides selon l’âge
    }

    // Active ou désactive le menu de boutons
    void TogglePathButtons()
    {
        if (pathButtonPanel.activeSelf)
            StopPlacing();
        else
            UpdateButtons();

        pathButtonPanel.SetActive(!pathButtonPanel.activeSelf);
    }

    // Active les boutons correspondant à l’âge courant
    void UpdateButtons()
    {
        GameAge currentAge = AgeManager.Instance.GetCurrentAge();
        foreach (var ageBtn in ageButtons)
            ageBtn.button.gameObject.SetActive(ageBtn.age <= currentAge);
    }

    void StartPlacing(PathData pathData)
    {
        isPlacing = true;
        pathPlacementManager.StartPlacing(pathData.prefab);
    }

    void StopPlacing()
    {
        isPlacing = false;
        pathPlacementManager.StopPlacing();
    }
}
