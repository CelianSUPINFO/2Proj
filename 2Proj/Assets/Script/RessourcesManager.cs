using System.Collections.Generic;
using UnityEngine;

public enum ResourceType
{
    Wood,
    Stone,
    Iron,
    Clay,
    Gold,
    Food,
    Water,
    Leather,
    Plank,
    Brick,
    IronIngot,
    GoldIngot,
    Population
}

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    private Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>();

    public delegate void OnResourceChanged();
    public event OnResourceChanged onResourceChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializeResources();
    }

    private void InitializeResources()
    {
        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            resources[type] = 0;
        }

        
        resources[ResourceType.Wood] = 100;
        resources[ResourceType.Food] = 50;
        resources[ResourceType.Population] = 10;
    }

    public int Get(ResourceType type)
    {
        return resources.ContainsKey(type) ? resources[type] : 0;
    }

    public void Add(ResourceType type, int amount)
    {
        if (!resources.ContainsKey(type))
            resources[type] = 0;

        resources[type] += amount;
        Debug.Log($"[+]{type} : {amount} → total: {resources[type]}");

        onResourceChanged?.Invoke(); 
    }

    public bool Spend(ResourceType type, int amount)
    {
        if (Get(type) >= amount)
        {
            resources[type] -= amount;
            Debug.Log($"[-]{type} : {amount} → total: {resources[type]}");

            onResourceChanged?.Invoke(); 
            return true;
        }

        Debug.LogWarning($"Pas assez de {type} (besoin: {amount}, actuel: {Get(type)})");
        return false;
    }

    public bool HasEnough(ResourceType type, int amount)
    {
        return Get(type) >= amount;
    }

    public Dictionary<ResourceType, int> GetAllResources()
    {
        return new Dictionary<ResourceType, int>(resources);
    }
}
