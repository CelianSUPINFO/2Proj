using UnityEngine;

public class GameStatsManager : MonoBehaviour
{
    public static GameStatsManager Instance { get; private set; }

    [Header("Ressources")]
    public int population = 0;
    public int wood = 0;
    public int stone = 0;
    public int iron = 0;

    public delegate void OnStatsChanged();
    public event OnStatsChanged onStatsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void AddResource(string type, int amount)
    {
        switch (type.ToLower())
        {
            case "wood": wood += amount; break;
            case "stone": stone += amount; break;
            case "iron": iron += amount; break;
            case "population": population += amount; break;
        }

        onStatsChanged?.Invoke(); // notifie les UI
    }

    public void RemoveResource(string type, int amount)
    {
        AddResource(type, -amount);
    }
}
