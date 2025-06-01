using UnityEngine;
using UnityEngine.UI;

public class FixedPathUIManager : MonoBehaviour
{
    [System.Serializable]
    public class AgePathButton
    {
        public GameAge age;
        public Button button;
        public PathData pathData;
    }

    public GameObject pathButtonPanel;
    public Button toggleMenuButton;
    public PathPlacementManager pathPlacementManager;
    public AgePathButton[] ageButtons;

    private bool isPlacing = false;

    void Start()
    {
        toggleMenuButton.onClick.AddListener(TogglePathButtons);

        foreach (var ageBtn in ageButtons)
        {
            var capturedPath = ageBtn.pathData;
            ageBtn.button.onClick.AddListener(() => StartPlacing(capturedPath));
        }

        UpdateButtons();
    }

    void TogglePathButtons()
    {
        if (pathButtonPanel.activeSelf)
        {
            StopPlacing();
        }
        else
        {
            UpdateButtons();
        }

        pathButtonPanel.SetActive(!pathButtonPanel.activeSelf);
    }

    void UpdateButtons()
    {
        GameAge currentAge = AgeManager.Instance.GetCurrentAge();

        foreach (var ageBtn in ageButtons)
        {
            ageBtn.button.gameObject.SetActive(ageBtn.age <= currentAge);
        }
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
