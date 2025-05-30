using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum ResourceType
{
    Wood,
    Stone,
    Food,
    Water,
    Clay,
    Wheat,
    Fish,
    Plank,
    Flour,
    Bread,
    Meat,
    Leather,
    Brick,
    Iron,
    Tools,
    Cloth,
    Gold,
    Population,
    Search,
}

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    // ⚠️ ATTENTION : Ces ressources sont maintenant uniquement pour les ressources "virtuelles"
    // qui n'existent pas physiquement dans le monde (comme Population, Search, etc.)
    private Dictionary<ResourceType, int> virtualResources = new Dictionary<ResourceType, int>();

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
        InitializeVirtualResources();
    }

    void Update()
    {
        SyncPopulationWithCharacters();
    }

    private void InitializeVirtualResources()
    {
        // Seules les ressources "virtuelles" sont initialisées ici
        virtualResources[ResourceType.Population] = 0;
        virtualResources[ResourceType.Search] = 10000;
    }

    /// <summary>
    /// Mapping des ressources physiques (string) vers les ResourceType
    /// </summary>
    private ResourceType? GetResourceTypeFromString(string resourceName)
    {
        return resourceName?.ToLower() switch
        {
            "bois" => ResourceType.Wood,
            "pierre" => ResourceType.Stone,
            "blé" => ResourceType.Wheat,
            "poisson" => ResourceType.Fish,
            "planches" => ResourceType.Plank,
            "farine" => ResourceType.Flour,
            "pain" => ResourceType.Bread,
            "viande" => ResourceType.Meat,
            "cuir" => ResourceType.Leather,
            "briques" => ResourceType.Brick,
            "fer" => ResourceType.Iron,
            "outils" => ResourceType.Tools,
            "tissu" => ResourceType.Cloth,
            "or" => ResourceType.Gold,
            "argile" => ResourceType.Clay,
            "eau" => ResourceType.Water,
            "nourriture" => ResourceType.Food,
            _ => null
        };
    }

    /// <summary>
    /// Calcule la quantité totale d'une ressource dans TOUT le monde
    /// (stockages + sacs à dos + ressources virtuelles)
    /// </summary>
    public int Get(ResourceType type)
    {
        // 1. Ressources virtuelles (Population, Search, etc.)
        if (virtualResources.ContainsKey(type))
        {
            return virtualResources[type];
        }

        // 2. Ressources physiques : calculer depuis les stockages et sacs
        int total = 0;
        string resourceName = GetResourceName(type);

        // Compter dans tous les stockages
        BatimentInteractif[] stockages = FindObjectsOfType<BatimentInteractif>();
        foreach (var stockage in stockages)
        {
            if (stockage.estUnStockage)
            {
                total += stockage.ObtenirQuantite(resourceName);
            }
        }

        // Compter dans tous les sacs à dos
        PersonnageData[] personnages = FindObjectsOfType<PersonnageData>();
        foreach (var perso in personnages)
        {
            if (perso.sacADos.ressourceActuelle == resourceName)
            {
                total += perso.sacADos.quantite;
            }
        }

        return total;
    }

    /// <summary>
    /// Convertit un ResourceType vers le nom utilisé dans les stockages/sacs
    /// </summary>
    private string GetResourceName(ResourceType type)
    {
        return type switch
        {
            ResourceType.Wood => "Bois",
            ResourceType.Stone => "Pierre",
            ResourceType.Wheat => "Blé",
            ResourceType.Fish => "Poisson",
            ResourceType.Plank => "Planches",
            ResourceType.Flour => "Farine",
            ResourceType.Bread => "Pain",
            ResourceType.Meat => "Viande",
            ResourceType.Leather => "Cuir",
            ResourceType.Brick => "Briques",
            ResourceType.Iron => "Fer",
            ResourceType.Tools => "Outils",
            ResourceType.Cloth => "Tissu",
            ResourceType.Gold => "Or",
            ResourceType.Clay => "Argile",
            ResourceType.Water => "Eau",
            ResourceType.Food => "Nourriture",
            _ => type.ToString()
        };
    }

    /// <summary>
    /// Ajouter une ressource virtuelle uniquement
    /// Pour les ressources physiques, il faut directement modifier les stockages/sacs
    /// </summary>
    public void Add(ResourceType type, int amount)
    {
        if (virtualResources.ContainsKey(type))
        {
            virtualResources[type] += amount;
            Debug.Log($"[+]{type} : {amount} → total: {virtualResources[type]}");
            onResourceChanged?.Invoke();
        }
        else
        {
            Debug.LogWarning($"Tentative d'ajout direct de {type}. Utilisez les stockages ou sacs à dos pour les ressources physiques.");
        }
    }

    /// <summary>
    /// Dépenser des ressources en les retirant PHYSIQUEMENT des stockages et sacs à dos
    /// </summary>
    public bool Spend(ResourceType type, int amount)
    {
        // Ressources virtuelles
        if (virtualResources.ContainsKey(type))
        {
            if (virtualResources[type] >= amount)
            {
                virtualResources[type] -= amount;
                Debug.Log($"[-]{type} : {amount} → total: {virtualResources[type]}");
                onResourceChanged?.Invoke();
                return true;
            }
            return false;
        }

        // Ressources physiques : retirer depuis stockages et sacs
        return SpendFromPhysicalSources(type, amount);
    }

    /// <summary>
    /// Retire physiquement des ressources des stockages et sacs à dos
    /// </summary>
    private bool SpendFromPhysicalSources(ResourceType type, int totalAmount)
    {
        if (totalAmount <= 0) return true;

        string resourceName = GetResourceName(type);
        int remaining = totalAmount;

        // 1. D'abord retirer des stockages
        BatimentInteractif[] stockages = FindObjectsOfType<BatimentInteractif>();
        foreach (var stockage in stockages)
        {
            if (!stockage.estUnStockage || remaining <= 0) continue;

            int retiree = stockage.RetirerRessource(resourceName, remaining);
            remaining -= retiree;

            if (retiree > 0)
            {
                Debug.Log($"[-] Retiré {retiree} x {resourceName} du stockage {stockage.name}");
            }
        }

        // 2. Ensuite retirer des sacs à dos si nécessaire
        if (remaining > 0)
        {
            PersonnageData[] personnages = FindObjectsOfType<PersonnageData>();
            foreach (var perso in personnages)
            {
                if (remaining <= 0) break;
                if (perso.sacADos.ressourceActuelle != resourceName) continue;

                int aRetirer = Mathf.Min(remaining, perso.sacADos.quantite);
                perso.sacADos.quantite -= aRetirer;
                remaining -= aRetirer;

                if (perso.sacADos.quantite <= 0)
                {
                    perso.sacADos.Vider();
                }

                Debug.Log($"[-] Retiré {aRetirer} x {resourceName} du sac de {perso.name}");
            }
        }

        bool success = remaining == 0;
        if (success)
        {
            Debug.Log($"✅ Dépensé {totalAmount} x {type} avec succès");
            onResourceChanged?.Invoke();
        }
        else
        {
            Debug.LogWarning($"❌ Impossible de dépenser {totalAmount} x {type} (manque {remaining})");
        }

        return success;
    }

    public bool HasEnough(ResourceType type, int amount)
    {
        return Get(type) >= amount;
    }

    public Dictionary<ResourceType, int> GetAllResources()
    {
        Dictionary<ResourceType, int> allResources = new Dictionary<ResourceType, int>();

        // Ajouter toutes les ressources (physiques + virtuelles)
        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            allResources[type] = Get(type);
        }

        return allResources;
    }

    public bool HasEnough(BuildingCost cost)
    {
        foreach (var res in cost.resourceCosts)
        {
            if (!HasEnough(res.type, res.amount))
                return false;
        }
        return true;
    }

    public bool Spend(BuildingCost cost)
    {
        if (!HasEnough(cost)) return false;

        foreach (var res in cost.resourceCosts)
        {
            if (!Spend(res.type, res.amount))
            {
                Debug.LogError($"Erreur lors de la dépense de {res.type}");
                return false;
            }
        }
        return true;
    }

    public void SyncPopulationWithCharacters()
    {
        int nbPersonnages = GameObject.FindObjectsOfType<PersonnageData>().Length;
        if (virtualResources[ResourceType.Population] != nbPersonnages)
        {
            virtualResources[ResourceType.Population] = nbPersonnages;
            onResourceChanged?.Invoke();
        }
    }

    /// <summary>
    /// Méthode utilitaire pour déclencher manuellement la mise à jour de l'UI
    /// À appeler quand on modifie directement les stockages/sacs
    /// </summary>
    public void NotifyResourceChanged()
    {
        onResourceChanged?.Invoke();
    }

    /// <summary>
    /// Debug : Affiche toutes les ressources et leur provenance
    /// </summary>
    [ContextMenu("Debug - Afficher toutes les ressources")]
    public void DebugShowAllResources()
    {
        Debug.Log("=== RESSOURCES GLOBALES ===");
        
        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            int total = Get(type);
            if (total > 0)
            {
                Debug.Log($"{type}: {total}");
                
                if (!virtualResources.ContainsKey(type))
                {
                    // Détailler la provenance des ressources physiques
                    string resourceName = GetResourceName(type);
                    
                    // Stockages
                    BatimentInteractif[] stockages = FindObjectsOfType<BatimentInteractif>();
                    foreach (var stockage in stockages)
                    {
                        if (stockage.estUnStockage)
                        {
                            int qty = stockage.ObtenirQuantite(resourceName);
                            if (qty > 0)
                            {
                                Debug.Log($"  - Stockage {stockage.name}: {qty}");
                            }
                        }
                    }
                    
                    // Sacs à dos
                    PersonnageData[] personnages = FindObjectsOfType<PersonnageData>();
                    foreach (var perso in personnages)
                    {
                        if (perso.sacADos.ressourceActuelle == resourceName && perso.sacADos.quantite > 0)
                        {
                            Debug.Log($"  - Sac de {perso.name}: {perso.sacADos.quantite}");
                        }
                    }
                }
            }
        }
    }
}