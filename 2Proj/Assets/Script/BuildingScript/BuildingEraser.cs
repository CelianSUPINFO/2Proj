using UnityEngine;
using System.Collections.Generic;

public class BuildingEraser : MonoBehaviour
{
    [Header("Références")]
    public LayerMask buildingLayer; 

    public bool eraseMode = false;

    void Update()
    {
        if (eraseMode == false){
            return;
        } 

        HighlightHoveredBuilding();

        if (Input.GetMouseButtonDown(0))
        {
            TryEraseBuilding();
        }

        if (Input.GetMouseButtonDown(1))
        {
            SetEraseMode(false);
        }
    }

    public void ToggleEraseMode()
    {   
        eraseMode = !eraseMode;
        Debug.Log(eraseMode ? "Mode suppression activé" : "Mode suppression désactivé");
    }

    void SetEraseMode(bool active)
    {
        eraseMode = active;
        Debug.Log(eraseMode ? "Mode suppression activé" : "Mode suppression désactivé");
    }

    public bool IsEraseModeActive()
    {
        return eraseMode;
    }

    private void TryEraseBuilding()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos, buildingLayer);

        if (hit != null)
        {
            Building building = hit.GetComponent<Building>();
            BatimentInteractif batimentInteractif = hit.GetComponent<BatimentInteractif>();
            
            // Vérifier si c'est le dernier stockage
            if (batimentInteractif != null && batimentInteractif.estUnStockage)
            {
                if (IsLastStorage())
                {
                    Debug.LogWarning("❌ Impossible de supprimer le dernier bâtiment de stockage !");
                    return;
                }
            }

            // Effectuer le remboursement avant destruction
            if (building != null && building.data != null)
            {
                RefundHalfCostToStorages(building.data);
            }

            Destroy(hit.gameObject);
            Debug.Log("✅ Bâtiment supprimé avec remboursement dans les stockages.");
        }
        else
        {
            Debug.Log("Aucun bâtiment ici à supprimer.");
        }
    }

    private void HighlightHoveredBuilding()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos, buildingLayer);

        if (hit != null)
        {
            SpriteRenderer sr = hit.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                // Vérifier si c'est le dernier stockage pour changer la couleur
                BatimentInteractif batiment = hit.GetComponent<BatimentInteractif>();
                if (batiment != null && batiment.estUnStockage && IsLastStorage())
                {
                    sr.color = new Color(0.7f, 0.7f, 0.7f, 1f); // Gris = non supprimable
                }
                else
                {
                    sr.color = new Color(1f, 0.5f, 0.5f, 1f); // Rouge = supprimable
                }
            }
        }
    }

    /// <summary>
    /// Vérifie s'il ne reste qu'un seul bâtiment de stockage dans la scène
    /// </summary>
    private bool IsLastStorage()
    {
        BatimentInteractif[] allBatiments = FindObjectsOfType<BatimentInteractif>();
        int storageCount = 0;
        
        foreach (var batiment in allBatiments)
        {
            if (batiment.estUnStockage)
            {
                storageCount++;
            }
        }
        
        return storageCount <= 1;
    }

    /// <summary>
    /// Rembourse 50% du coût dans les bâtiments de stockage disponibles
    /// </summary>
    void RefundHalfCostToStorages(BuildingData data)
    {
        // Calculer le remboursement à 50%
        List<ResourceRefund> refunds = new List<ResourceRefund>();
        
        foreach (var res in data.cost.resourceCosts)
        {
            int refundAmount = Mathf.FloorToInt(res.amount * 0.5f);
            if (refundAmount > 0)
            {
                refunds.Add(new ResourceRefund 
                { 
                    resourceType = res.type, 
                    amount = refundAmount 
                });
            }
        }

        // Obtenir tous les stockages disponibles
        BatimentInteractif[] allStorages = GetAllStorages();
        
        if (allStorages.Length == 0)
        {
            Debug.LogWarning("❌ Aucun stockage disponible ! Remboursement perdu.");
            return;
        }

        // Distribuer les remboursements dans les stockages
        foreach (var refund in refunds)
        {
            string resourceName = GetResourceName(refund.resourceType);
            int remainingToRefund = refund.amount;
            
            Debug.Log($"🔁 Tentative de remboursement : {remainingToRefund} x {refund.resourceType}");

            // Essayer de distribuer dans tous les stockages
            foreach (var storage in allStorages)
            {
                if (remainingToRefund <= 0) break;

                int spaceAvailable = storage.GetEspaceLibre(resourceName);
                int toAdd = Mathf.Min(remainingToRefund, spaceAvailable);
                
                if (toAdd > 0)
                {
                    if (storage.AjouterRessource(resourceName, toAdd))
                    {
                        remainingToRefund -= toAdd;
                        Debug.Log($"✅ Ajouté {toAdd} x {resourceName} dans le stockage {storage.name}");
                    }
                }
            }

            // Vérifier si tout a pu être remboursé
            if (remainingToRefund > 0)
            {
                Debug.LogWarning($"⚠️ Stockages pleins ! {remainingToRefund} x {refund.resourceType} perdus.");
            }
            else
            {
                Debug.Log($"✅ Remboursement complet : {refund.amount} x {refund.resourceType}");
            }
        }
    }

    /// <summary>
    /// Obtient tous les bâtiments de stockage de la scène
    /// </summary>
    private BatimentInteractif[] GetAllStorages()
    {
        BatimentInteractif[] allBatiments = FindObjectsOfType<BatimentInteractif>();
        List<BatimentInteractif> storages = new List<BatimentInteractif>();
        
        foreach (var batiment in allBatiments)
        {
            if (batiment.estUnStockage)
            {
                storages.Add(batiment);
            }
        }
        
        return storages.ToArray();
    }

    /// <summary>
    /// Convertit un ResourceType vers le nom utilisé dans les stockages
    /// (Même logique que dans ResourceManager)
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
    /// Structure pour gérer les remboursements
    /// </summary>
    private struct ResourceRefund
    {
        public ResourceType resourceType;
        public int amount;
    }
}