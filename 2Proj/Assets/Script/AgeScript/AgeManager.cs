using UnityEngine;
using TMPro;

public class AgeManager : MonoBehaviour
{
    public static AgeManager Instance { get; private set; }

    [SerializeField] public GameAge currentAge = GameAge.StoneAge;
    [SerializeField] private TMP_Text ageDisplayText; // assigné depuis l’inspecteur

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        UpdateAgeDisplay();
    }

    public GameAge GetCurrentAge()
    {
        return currentAge;
    }

    public void AdvanceToNextAge()
    {
        if ((int)currentAge < System.Enum.GetValues(typeof(GameAge)).Length - 1)
        {
            currentAge++;
            Debug.Log("Nouvel âge : " + currentAge);
            UpdateAgeDisplay();
        }
        else
        {
            Debug.Log("Tu es déjà à l’âge maximum.");
        }
    }

    private void UpdateAgeDisplay()
    {
        if (ageDisplayText != null)
            ageDisplayText.text = $"{GetDisplayName(currentAge)}";
    }

    private string GetDisplayName(GameAge age)
    {
        return age switch
        {
            GameAge.StoneAge => "Âge de pierre",
            GameAge.AncientAge => "Âge antique",
            GameAge.MedievalAge => "Âge médiéval",
            GameAge.IndustrialAge => "Âge industriel",
            _ => age.ToString()
        };
    }
}
