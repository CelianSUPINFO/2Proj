using System.Collections.Generic;
using UnityEngine;

// Ce script sert à suivre l’évolution (la tendance) des ressources dans le temps
public class ResourceTrendTracker : MonoBehaviour
{
    // Classe interne pour stocker l’évolution d’une ressource
    [System.Serializable]
    public class ResourceTrend
    {
        public ResourceType type;             // Type de ressource (ex : bois, pierre…)
        public int currentAmount;             // Quantité actuelle de cette ressource
        public int previousAmount;            // Quantité qu’il y avait la dernière fois qu’on a vérifié

        // Calcul de la tendance : différence entre maintenant et avant, divisée par 1 minute
        public float trendPerMinute => (currentAmount - previousAmount) / 1f;
    }

    public float intervalSeconds = 60f;       // Temps entre deux relevés (ici 60 secondes = 1 minute)
    public List<ResourceTrend> trends = new(); // Liste des tendances pour chaque ressource

    private float timer; // Chrono interne pour attendre entre deux relevés

    void Start()
    {
        // Initialisation de la liste des ressources à suivre
        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            trends.Add(new ResourceTrend
            {
                type = type,
                currentAmount = ResourceManager.Instance.Get(type), // On récupère la quantité actuelle
                previousAmount = ResourceManager.Instance.Get(type) // Et on l'enregistre comme "avant"
            });
        }

        timer = intervalSeconds; // On démarre le chrono
    }

    void Update()
    {
        // Chaque frame, on fait baisser le chrono
        timer -= Time.deltaTime;

        // Si une minute est passée, on met à jour les tendances
        if (timer <= 0f)
        {
            UpdateTrends();
            timer = intervalSeconds; // On relance le timer pour la prochaine minute
        }
    }

    // Met à jour les tendances pour toutes les ressources
    void UpdateTrends()
    {
        foreach (var trend in trends)
        {
            trend.previousAmount = trend.currentAmount;                      // On mémorise l’ancienne valeur
            trend.currentAmount = ResourceManager.Instance.Get(trend.type); // On récupère la nouvelle valeur
        }

        // Affichage dans la console (pour le debug)
        Debug.Log("[Tendances]");
        foreach (var t in trends)
        {
            // On affiche seulement certaines ressources (pour ne pas surcharger la console)
            if (t.type is ResourceType.Plank or ResourceType.Brick or ResourceType.Iron or ResourceType.Gold)
                Debug.Log($"{t.type}: {t.trendPerMinute:+0.##;-0.##;0} par minute");
        }
    }

    // Méthode appelée par l'UI pour connaître la tendance d'une ressource
    public float GetTrend(ResourceType type)
    {
        var trend = trends.Find(t => t.type == type); // On cherche la tendance correspondant au type
        return trend != null ? trend.trendPerMinute : 0f;
    }
}
