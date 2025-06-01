using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// Enum qui représente tous les types de ressources disponibles dans le jeu
public enum ResourceType
{
    Wood, Stone, Food, Water, Clay, Wheat, Fish, Plank, Flour,
    Bread, Meat, Leather, Brick, Iron, Tools, Cloth, Gold,
    Population, Search,
}

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    // Ressources virtuelles (pas stockées physiquement dans le monde)
    private Dictionary<ResourceType, int> virtualResources = new Dictionary<ResourceType, int>();

    public delegate void OnResourceChanged();
    public event OnResourceChanged onResourceChanged;

    private void Awake()
    {
        // Singleton : une seule instance de ResourceManager
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializeVirtualResources(); // Initialise les ressources virtuelles
    }

    void Update()
    {
        SyncPopulationWithCharacters(); // Met à jour la population virtuelle selon les personnages présents
    }

    private void InitializeVirtualResources()
    {
        virtualResources[ResourceType.Population] = 0;
        virtualResources[ResourceType.Search] = 0;
    }

    // Convertit une string (comme "bois") en ResourceType (comme ResourceType.Wood)
    private ResourceType? GetResourceTypeFromString(string resourceName)
    {
        return resourceName?.ToLower() switch
        {
            "bois" => ResourceType.Wood,
            "pierre" => ResourceType.Stone,
            // ... autres mappings
            "nourriture" => ResourceType.Food,
            _ => null
        };
    }

    // Retourne la quantité totale d’une ressource (dans tous les stockages + sacs + ressources virtuelles)
    public int Get(ResourceType type)
    {
        if (virtualResources.ContainsKey(type))
            return virtualResources[type];

        int total = 0;
        string resourceName = GetResourceName(type);

        foreach (var stockage in FindObjectsOfType<BatimentInteractif>())
        {
            if (stockage.estUnStockage)
                total += stockage.ObtenirQuantite(resourceName);
        }

        foreach (var perso in FindObjectsOfType<PersonnageData>())
        {
            if (perso.sacADos.ressourceActuelle == resourceName)
                total += perso.sacADos.quantite;
        }

        return total;
    }

    // Transforme un ResourceType en string utilisable par les objets du jeu
    private string GetResourceName(ResourceType type)
    {
        return type switch
        {
            ResourceType.Wood => "Bois",
            ResourceType.Stone => "Pierre",
            // ... autres types
            ResourceType.Food => "Nourriture",
            _ => type.ToString()
        };
    }

    // Ajoute une ressource virtuelle (Population, Search, etc.)
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
            Debug.LogWarning($"Impossible d’ajouter {type}. Modifier stockage/sac pour ressources physiques.");
        }
    }

    // Dépense une ressource (physique ou virtuelle)
    public bool Spend(ResourceType type, int amount)
    {
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

        return SpendFromPhysicalSources(type, amount);
    }

    // Retire physiquement des ressources depuis les stockages et les sacs
    private bool SpendFromPhysicalSources(ResourceType type, int totalAmount)
    {
        if (totalAmount <= 0) return true;

        string resourceName = GetResourceName(type);
        int remaining = totalAmount;

        foreach (var stockage in FindObjectsOfType<BatimentInteractif>())
        {
            if (!stockage.estUnStockage || remaining <= 0) continue;

            int retiree = stockage.RetirerRessource(resourceName, remaining);
            remaining -= retiree;

            if (retiree > 0)
                Debug.Log($"[-] Retiré {retiree} x {resourceName} du stockage {stockage.name}");
        }

        if (remaining > 0)
        {
            foreach (var perso in FindObjectsOfType<PersonnageData>())
            {
                if (remaining <= 0) break;
                if (perso.sacADos.ressourceActuelle != resourceName) continue;

                int aRetirer = Mathf.Min(remaining, perso.sacADos.quantite);
                perso.sacADos.quantite -= aRetirer;
                remaining -= aRetirer;

                if (perso.sacADos.quantite <= 0)
                    perso.sacADos.Vider();

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
            Debug.LogWarning($"❌ Manque {remaining} pour {type}");
        }

        return success;
    }

    // Vérifie si on a assez d’une ressource
    public bool HasEnough(ResourceType type, int amount)
    {
        return Get(type) >= amount;
    }

    // Retourne toutes les ressources du jeu avec leur quantité
    public Dictionary<ResourceType, int> GetAllResources()
    {
        Dictionary<ResourceType, int> allResources = new();
        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            allResources[type] = Get(type);
        return allResources;
    }

    // Vérifie si on a assez de ressources pour un coût de bâtiment
    public bool HasEnough(BuildingCost cost)
    {
        foreach (var res in cost.resourceCosts)
        {
            if (!HasEnough(res.type, res.amount))
                return false;
        }
        return true;
    }

    // Dépense les ressources nécessaires pour construire un bâtiment
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

    // Met à jour la ressource Population pour qu’elle soit égale au nombre de personnages
    public void SyncPopulationWithCharacters()
    {
        int nbPersonnages = FindObjectsOfType<PersonnageData>().Length;
        if (virtualResources[ResourceType.Population] != nbPersonnages)
        {
            virtualResources[ResourceType.Population] = nbPersonnages;
            onResourceChanged?.Invoke();
        }
    }

    // Déclenche une mise à jour de l'UI (si besoin de forcer un rafraîchissement)
    public void NotifyResourceChanged()
    {
        onResourceChanged?.Invoke();
    }

    // Affiche dans la console toutes les ressources présentes dans le monde
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
                    string resourceName = GetResourceName(type);

                    foreach (var stockage in FindObjectsOfType<BatimentInteractif>())
                    {
                        if (stockage.estUnStockage)
                        {
                            int qty = stockage.ObtenirQuantite(resourceName);
                            if (qty > 0)
                                Debug.Log($"  - Stockage {stockage.name}: {qty}");
                        }
                    }

                    foreach (var perso in FindObjectsOfType<PersonnageData>())
                    {
                        if (perso.sacADos.ressourceActuelle == resourceName && perso.sacADos.quantite > 0)
                            Debug.Log($"  - Sac de {perso.name}: {perso.sacADos.quantite}");
                    }
                }
            }
        }
    }
}
