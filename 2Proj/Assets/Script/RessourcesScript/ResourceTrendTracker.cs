using System.Collections.Generic;
using UnityEngine;

public class ResourceTrendTracker : MonoBehaviour
{
    [System.Serializable]
    public class ResourceTrend
    {
        public ResourceType type;
        public int currentAmount;
        public int previousAmount;
        public float trendPerMinute => (currentAmount - previousAmount) / 1f; // 1 minute d’intervalle
    }

    public float intervalSeconds = 60f;
    public List<ResourceTrend> trends = new();

    private float timer;

    void Start()
    {
        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            trends.Add(new ResourceTrend
            {
                type = type,
                currentAmount = ResourceManager.Instance.Get(type),
                previousAmount = ResourceManager.Instance.Get(type)
            });
        }

        timer = intervalSeconds;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            UpdateTrends();
            timer = intervalSeconds;
        }
    }

    void UpdateTrends()
    {
        foreach (var trend in trends)
        {
            trend.previousAmount = trend.currentAmount;
            trend.currentAmount = ResourceManager.Instance.Get(trend.type);
        }

        // Tu peux ici déclencher un affichage ou UI update
        Debug.Log("[Tendances]");
        foreach (var t in trends)
        {
            if (t.type is ResourceType.Plank or ResourceType.Brick or ResourceType.Iron or ResourceType.Gold)
                Debug.Log($"{t.type}: {t.trendPerMinute:+0.##;-0.##;0} par minute");
        }
    }

    public float GetTrend(ResourceType type)
    {
        var trend = trends.Find(t => t.type == type);
        return trend != null ? trend.trendPerMinute : 0f;
    }
}
