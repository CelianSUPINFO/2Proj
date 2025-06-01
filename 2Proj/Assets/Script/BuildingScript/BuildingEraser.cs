using UnityEngine;
using System.Collections.Generic;


// Permet de supprimer des bâtiments avec la souris quand le mode suppression est activé.
// Gère aussi le remboursement des ressources dans les bâtiments de stockage.
public class BuildingEraser : MonoBehaviour
{
    [Header("Références")]
    public LayerMask buildingLayer; // Masque pour détecter les bâtiments

    public bool eraseMode = false; // Active ou désactive le mode suppression

    void Update()
    {
        // Si le mode suppression est désactivé, on ne fait rien
        if (!eraseMode) return;

        HighlightHoveredBuilding(); // Affiche visuellement le bâtiment survolé

        // Clic gauche : tenter de supprimer le bâtiment
        if (Input.GetMouseButtonDown(0))
        {
            TryEraseBuilding();
        }

        // Clic droit : quitter le mode suppression
        if (Input.GetMouseButtonDown(1))
        {
            SetEraseMode(false);
        }
    }

    // Active ou désactive le mode suppression
    public void ToggleEraseMode()
    {
        eraseMode = !eraseMode;
        Debug.Log(eraseMode ? "Mode suppression activé" : "Mode suppression désactivé");
    }

    // Définit l’état du mode suppression manuellement
    void SetEraseMode(bool active)
    {
        eraseMode = active;
        Debug.Log(eraseMode ? "Mode suppression activé" : "Mode suppression désactivé");
    }

    // Permet à d’autres scripts de savoir si le mode est actif
    public bool IsEraseModeActive()
    {
        return eraseMode;
    }

    // Supprime le bâtiment sous la souris, avec vérification
    private void TryEraseBuilding()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos, buildingLayer);

        if (hit != null)
        {
            Building building = hit.GetComponent<Building>();
            BatimentInteractif batimentInteractif = hit.GetComponent<BatimentInteractif>();

            // Si c’est un stockage, empêcher la suppression du dernier
            if (batimentInteractif != null && batimentInteractif.estUnStockage && IsLastStorage())
            {
                Debug.LogWarning("Impossible de supprimer le dernier bâtiment de stockage !");
                return;
            }

            // Rembourse les ressources avant de détruire
            if (building != null && building.data != null)
            {
                RefundHalfCostToStorages(building.data);
            }

            Destroy(hit.gameObject);
            Debug.Log("Bâtiment supprimé avec remboursement dans les stockages.");
        }
        else
        {
            Debug.Log("Aucun bâtiment ici à supprimer.");
        }
    }

    // Change la couleur du bâtiment survolé (rouge = supprimable, gris = bloqué)
    private void HighlightHoveredBuilding()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos, buildingLayer);

        if (hit != null)
        {
            SpriteRenderer sr = hit.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                BatimentInteractif batiment = hit.GetComponent<BatimentInteractif>();

                // Gris si c’est le dernier stockage, rouge sinon
                sr.color = (batiment != null && batiment.estUnStockage && IsLastStorage())
                    ? new Color(0.7f, 0.7f, 0.7f, 1f) // Non supprimable
                    : new Color(1f, 0.5f, 0.5f, 1f);  // Supprimable
            }
        }
    }

     //Vérifie s’il ne reste qu’un seul bâtiment de stockage.
    private bool IsLastStorage()
    {
        BatimentInteractif[] allBatiments = FindObjectsOfType<BatimentInteractif>();
        int storageCount = 0;

        foreach (var batiment in allBatiments)
        {
            if (batiment.estUnStockage)
                storageCount++;
        }

        return storageCount <= 1;
    }


    // Rembourse la moitié du coût de construction dans les bâtiments de stockage disponibles.
    void RefundHalfCostToStorages(BuildingData data)
    {
        List<ResourceRefund> refunds = new();

        // Calcule les montants à rembourser (50 %)
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

        // Récupère tous les bâtiments de stockage
        BatimentInteractif[] allStorages = GetAllStorages();
        if (allStorages.Length == 0)
        {
            Debug.LogWarning("Aucun stockage disponible ! Remboursement perdu.");
            return;
        }

        // Répartit les ressources dans les stockages disponibles
        foreach (var refund in refunds)
        {
            string resourceName = GetResourceName(refund.resourceType);
            int remainingToRefund = refund.amount;

            foreach (var storage in allStorages)
            {
                if (remainingToRefund <= 0) break;

                int spaceAvailable = storage.GetEspaceLibre(resourceName);
                int toAdd = Mathf.Min(remainingToRefund, spaceAvailable);

                if (toAdd > 0 && storage.AjouterRessource(resourceName, toAdd))
                {
                    remainingToRefund -= toAdd;
                    Debug.Log($"Ajouté {toAdd} x {resourceName} dans le stockage {storage.name}");
                }
            }

            if (remainingToRefund > 0)
                Debug.LogWarning($"Stockages pleins ! {remainingToRefund} x {refund.resourceType} perdus.");
            else
                Debug.Log($"Remboursement complet : {refund.amount} x {refund.resourceType}");
        }
    }


    //Retourne tous les bâtiments de stockage présents dans la scène.
    private BatimentInteractif[] GetAllStorages()
    {
        BatimentInteractif[] allBatiments = FindObjectsOfType<BatimentInteractif>();
        List<BatimentInteractif> storages = new();

        foreach (var batiment in allBatiments)
        {
            if (batiment.estUnStockage)
                storages.Add(batiment);
        }

        return storages.ToArray();
    }


    // Convertit le type de ressource en nom lisible utilisé dans le stockage.
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

    // Structure de remboursement (type + quantité)
    private struct ResourceRefund
    {
        public ResourceType resourceType;
        public int amount;
    }
}
