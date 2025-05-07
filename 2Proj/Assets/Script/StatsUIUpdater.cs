using TMPro;
using UnityEngine;

public class StatsUIUpdater : MonoBehaviour
{
    [Header("UI Texts")]
    public TMP_Text populationText;
    public TMP_Text woodText;
    public TMP_Text stoneText;
    public TMP_Text ironText;

    private void Start()
    {
        GameStatsManager.Instance.onStatsChanged += UpdateUI;
        UpdateUI(); // mettre à jour au lancement
    }

    private void OnDestroy()
    {
        GameStatsManager.Instance.onStatsChanged -= UpdateUI;
    }

    private void UpdateUI()
    {
        var stats = GameStatsManager.Instance;
        populationText.text = $"Population : {stats.population}";
        woodText.text = $"Bois : {stats.wood}";
        stoneText.text = $"Pierre : {stats.stone}";
        ironText.text = $"Fer : {stats.iron}";
    }
}
