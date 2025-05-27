using UnityEngine;
using System.Collections.Generic;

public enum TypeBesoin
{
    Aucun,
    Fatigue,
    Faim,
    Soif
}

[RequireComponent(typeof(Collider2D))]
public class BatimentInteractif : MonoBehaviour
{
    [Header("Capacité")]
    public int capaciteMax = 2;

    [Header("Régénération")]
    public bool regenereBesoin = false;
    public TypeBesoin typeBesoin = TypeBesoin.Fatigue;

    [Header("Stockage")]
    public bool estUnStockage = false;

    [System.Serializable]
    public class RessourceStockee
    {
        public string nom;
        public int quantite;

        public RessourceStockee(string nom, int quantite)
        {
            this.nom = nom;
            this.quantite = quantite;
        }
    }

    private List<RessourceStockee> stock = new List<RessourceStockee>();
    public int maxTypes = 4;
    public int maxParType = 20;

    [Header("Extension future")]
    public bool aFonctionPersonnalisee = false;

    private List<PersonnageData> occupants = new List<PersonnageData>();
    private Dictionary<PersonnageData, float> tempsRestant = new Dictionary<PersonnageData, float>();
    private Dictionary<PersonnageData, float> timerRegen = new Dictionary<PersonnageData, float>();

    public bool EstDisponible()
    {
        return occupants.Count < capaciteMax;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out PersonnageData perso)) return;

        if (occupants.Contains(perso) || !EstDisponible()) return;

        occupants.Add(perso);
        perso.cibleObjet = gameObject;

        // Démarrer régénération si activée
        if (regenereBesoin)
        {
            tempsRestant[perso] = GetTempsTotal(typeBesoin);
            timerRegen[perso] = 1f;
            perso.enRegeneration = true;
        }

        // Si c'est un bâtiment de stockage
        if (estUnStockage && perso.sacADos.quantite > 0)
        {
            StockerDepuis(perso);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.TryGetComponent(out PersonnageData perso)) return;

        occupants.Remove(perso);
        tempsRestant.Remove(perso);
        timerRegen.Remove(perso);
        perso.enRegeneration = false;
    }

    private void Update()
    {
        if (!regenereBesoin) return;

        List<PersonnageData> finis = new List<PersonnageData>();

        foreach (PersonnageData perso in occupants)
        {
            if (!tempsRestant.ContainsKey(perso)) continue;

            timerRegen[perso] -= Time.deltaTime;

            if (timerRegen[perso] <= 0f)
            {
                int gain = GetGainParTick(typeBesoin);
                switch (typeBesoin)
                {
                    case TypeBesoin.Fatigue:
                        perso.fatigue = Mathf.Min(100f, perso.fatigue + gain);
                        break;
                    case TypeBesoin.Faim:
                        perso.faim = Mathf.Min(100f, perso.faim + gain);
                        break;
                    case TypeBesoin.Soif:
                        perso.soif = Mathf.Min(100f, perso.soif + gain);
                        break;
                }

                tempsRestant[perso] -= 1f;
                timerRegen[perso] = 1f;

                if (tempsRestant[perso] <= 0f)
                {
                    finis.Add(perso);
                }
            }
        }

        foreach (var p in finis)
        {
            tempsRestant.Remove(p);
            timerRegen.Remove(p);
            occupants.Remove(p);
            p.enRegeneration = false;
        }
    }

    private int GetGainParTick(TypeBesoin besoin)
    {
        switch (besoin)
        {
            case TypeBesoin.Fatigue: return 5;
            case TypeBesoin.Faim: return 10;
            case TypeBesoin.Soif: return 10;
            default: return 0;
        }
    }

    private float GetTempsTotal(TypeBesoin besoin)
    {
        switch (besoin)
        {
            case TypeBesoin.Fatigue: return 20f;
            case TypeBesoin.Faim: return 10f;
            case TypeBesoin.Soif: return 10f;
            default: return 0f;
        }
    }

    private void StockerDepuis(PersonnageData perso)
    {
        string type = perso.sacADos.ressourceActuelle;
        int quantite = perso.sacADos.quantite;

        if (string.IsNullOrEmpty(type)) return;

        int quantiteAvant = ObtenirQuantite(type);

        RessourceStockee existante = stock.Find(r => r.nom == type);

        if (existante != null)
        {
            int ajoutPossible = Mathf.Min(maxParType - existante.quantite, quantite);
            existante.quantite += ajoutPossible;
            perso.sacADos.quantite -= ajoutPossible;
        }
        else if (stock.Count < maxTypes)
        {
            int ajout = Mathf.Min(maxParType, quantite);
            stock.Add(new RessourceStockee(type, ajout));
            perso.sacADos.quantite -= ajout;
        }

        if (perso.sacADos.quantite <= 0)
        {
            perso.sacADos.Vider();
        }

        // 🔥 NOUVEAU : Notifier le ResourceManager des changements
        int quantiteApres = ObtenirQuantite(type);
        if (quantiteAvant != quantiteApres)
        {
            Debug.Log($"[Stockage {name}] {type}: {quantiteAvant} → {quantiteApres}");
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.NotifyResourceChanged();
            }
        }

        Debug.Log($"[Stockage {name}] Contenu : " + string.Join(", ", stock.ConvertAll(r => $"{r.nom}:{r.quantite}")));
    }

    public int ObtenirQuantite(string ressource)
    {
        var r = stock.Find(r => r.nom == ressource);
        return r != null ? r.quantite : 0;
    }

    public List<RessourceStockee> GetAllStockedResources()
    {
        return new List<RessourceStockee>(stock);
    }

    public int RetirerRessource(string nom, int quantiteVoulu)
    {
        var r = stock.Find(s => s.nom.Equals(nom, System.StringComparison.OrdinalIgnoreCase));
        if (r == null || r.quantite <= 0) return 0;

        int quantiteAvant = r.quantite;
        int aRetirer = Mathf.Min(quantiteVoulu, r.quantite);
        r.quantite -= aRetirer;
        
        if (r.quantite <= 0) 
        {
            stock.Remove(r);
        }

        // 🔥 NOUVEAU : Notifier le ResourceManager des changements
        if (aRetirer > 0)
        {
            Debug.Log($"[Stockage {name}] Retiré {aRetirer} x {nom} ({quantiteAvant} → {r?.quantite ?? 0})");
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.NotifyResourceChanged();
            }
        }

        return aRetirer;
    }

    /// <summary>
    /// 🔥 NOUVELLE MÉTHODE : Ajouter directement des ressources au stockage
    /// Utile pour la production de bâtiments ou les récompenses
    /// </summary>
    public bool AjouterRessource(string nom, int quantite)
    {
        if (string.IsNullOrEmpty(nom) || quantite <= 0) return false;

        int quantiteAvant = ObtenirQuantite(nom);
        RessourceStockee existante = stock.Find(r => r.nom == nom);

        if (existante != null)
        {
            int ajoutPossible = Mathf.Min(maxParType - existante.quantite, quantite);
            if (ajoutPossible > 0)
            {
                existante.quantite += ajoutPossible;
                
                Debug.Log($"[Stockage {name}] Ajouté {ajoutPossible} x {nom} ({quantiteAvant} → {existante.quantite})");
                if (ResourceManager.Instance != null)
                {
                    ResourceManager.Instance.NotifyResourceChanged();
                }
                return true;
            }
        }
        else if (stock.Count < maxTypes)
        {
            int ajout = Mathf.Min(maxParType, quantite);
            stock.Add(new RessourceStockee(nom, ajout));
            
            Debug.Log($"[Stockage {name}] Nouveau type ajouté: {ajout} x {nom}");
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.NotifyResourceChanged();
            }
            return true;
        }

        return false; // Stockage plein
    }

    /// <summary>
    /// 🔥 NOUVELLE MÉTHODE : Obtenir l'espace libre pour un type de ressource
    /// </summary>
    public int GetEspaceLibre(string nom)
    {
        RessourceStockee existante = stock.Find(r => r.nom == nom);
        if (existante != null)
        {
            return maxParType - existante.quantite;
        }
        else if (stock.Count < maxTypes)
        {
            return maxParType;
        }
        return 0;
    }

    /// <summary>
    /// 🔥 NOUVELLE MÉTHODE : Vérifier si le stockage peut accepter une certaine quantité
    /// </summary>
    public bool PeutAccepter(string nom, int quantite)
    {
        return GetEspaceLibre(nom) >= quantite;
    }
}