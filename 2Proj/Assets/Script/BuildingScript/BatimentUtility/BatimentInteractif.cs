using UnityEngine;
using System.Collections.Generic;
using System.Collections;


public enum TypeBesoin
{
    Aucun,
    Fatigue,
    Faim,
    Soif
}


[RequireComponent(typeof(Collider2D))]
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

    private Dictionary<PersonnageData, float> timerProduction = new();

    [System.Serializable]
    public class MetierProductionInfo
    {
        public JobType metier;
        public string ressourceProduite;
        public float dureeProduction;
        public bool transformation;
        public string ressourceRequise;
        public int quantiteRequise;
        public int quantiteProduite;
    }

    public List<MetierProductionInfo> metierProductions = new List<MetierProductionInfo>()
    {
        new MetierProductionInfo { metier = JobType.Bucheron, ressourceProduite = "Bois", dureeProduction = 20f, quantiteProduite = 5 },
        new MetierProductionInfo { metier = JobType.CarrierPierre, ressourceProduite = "Pierre", dureeProduction = 20f, quantiteProduite = 5 },
        new MetierProductionInfo { metier = JobType.CarrierFer, ressourceProduite = "Fer", dureeProduction = 30f, quantiteProduite = 5 },
        new MetierProductionInfo { metier = JobType.CarrierOr, ressourceProduite = "Or", dureeProduction = 40f, quantiteProduite = 5 },
        new MetierProductionInfo { metier = JobType.FermierAnimaux, ressourceProduite = "Nourriture", dureeProduction = 20f, quantiteProduite = 5 },
        new MetierProductionInfo { metier = JobType.FermierBle, ressourceProduite = "Ble", dureeProduction = 30f, quantiteProduite = 5 },
        new MetierProductionInfo { metier = JobType.Chercheur, ressourceProduite = "Recherche", dureeProduction = 30f, quantiteProduite = 5 },
        new MetierProductionInfo { metier = JobType.Boulanger, ressourceProduite = "Pain", dureeProduction = 30f, transformation = true, ressourceRequise = "Ble", quantiteRequise = 5, quantiteProduite = 5 },
        new MetierProductionInfo { metier = JobType.Scieur, ressourceProduite = "Planche", dureeProduction = 30f, transformation = true, ressourceRequise = "Bois", quantiteRequise = 5, quantiteProduite = 5 },
        new MetierProductionInfo { metier = JobType.Pecheur, ressourceProduite = "Poisson", dureeProduction = 20f, quantiteProduite = 5 },
    };


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

    private List<RessourceStockee> stock = new();
    public int maxTypes = 4;
    public int maxParType = 20;

    [Header("Extension future")]
    public bool aFonctionPersonnalisee = false;

    private List<PersonnageData> occupants = new();
    private Dictionary<PersonnageData, float> tempsRestant = new();
    private Dictionary<PersonnageData, float> timerRegen = new();

    [Header("Métier associé")]
    public JobType metierAssocie;
    private List<PersonnageData> travailleursActuels = new();

    private float verifTimer = 0f;

    private void Start()
    {
        Debug.Log($"[Batiment {name}] Initialisation et tentative d'assignation de métier.");
        if (metierAssocie != JobType.Aucun)
        {
            StartCoroutine(AssignerTravailleursInitiaux());
        }
    }

    private IEnumerator AssignerTravailleursInitiaux()
    {
        travailleursActuels.RemoveAll(p => p == null);

        while (travailleursActuels.Count < capaciteMax)
        {
            PersonnageData candidat = MetierAssignmentManager.Instance.TrouverPersonnageSansMetier();

            if (candidat != null)
            {
                if (!travailleursActuels.Contains(candidat))
                {
                    candidat.AssignerMetier(metierAssocie);
                    candidat.batimentAssigné = this;
                    travailleursActuels.Add(candidat);
                    Debug.Log($"[Batiment {name}] Nouveau travailleur assigné: {candidat.name}");
                }
            }
            else
            {
                Debug.Log($"[Batiment {name}] Aucun candidat disponible pour le métier {metierAssocie}.");
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    public void GererMortTravailleur(PersonnageData mort)
    {
        if (mort == null) return;
        travailleursActuels.RemoveAll(p => p == null);
        if (travailleursActuels.Contains(mort))
        {
            travailleursActuels.Remove(mort);
            StartCoroutine(AssignerTravailleursInitiaux());
        }
    }

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

        if (!timerProduction.ContainsKey(perso))
        {
            MetierProductionInfo info = metierProductions.Find(i => i.metier == perso.metier);
            if (info != null)
            {
                if (info.transformation)
                {
                    int retiré = RetirerRessource(info.ressourceRequise, info.quantiteRequise);
                    if (retiré < info.quantiteRequise)
                    {
                        Debug.Log($"{name}: Pas assez de {info.ressourceRequise} pour transformer.");
                        return;
                    }
                }

                timerProduction[perso] = info.dureeProduction;
                Debug.Log($"[PROD] {perso.name} commence la production de {info.ressourceProduite}");
            }
        }

        if (regenereBesoin)
        {
            tempsRestant[perso] = GetTempsTotal(typeBesoin);
            timerRegen[perso] = 1f;
            perso.enRegeneration = true;
        }

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
        occupants.RemoveAll(p => p == null);
        travailleursActuels.RemoveAll(p => p == null);

        // 🔁 Réassignation automatique s'il manque des travailleurs
        verifTimer -= Time.deltaTime;
        if (verifTimer <= 0f)
        {
            verifTimer = 5f;
            if (travailleursActuels.Count < capaciteMax)
            {
                StartCoroutine(AssignerTravailleursInitiaux());
            }
        }

        // 🔁 Régénération
        List<PersonnageData> finis = new();
        foreach (PersonnageData perso in new List<PersonnageData>(occupants))
        {
            if (!tempsRestant.ContainsKey(perso)) continue;
            timerRegen[perso] -= Time.deltaTime;

            if (timerRegen[perso] <= 0f)
            {
                int gain = GetGainParTick(typeBesoin);
                switch (typeBesoin)
                {
                    case TypeBesoin.Fatigue:
                        perso.fatigue = Mathf.Min(100f, perso.fatigue + gain); break;
                    case TypeBesoin.Faim:
                        perso.faim = Mathf.Min(100f, perso.faim + gain); break;
                    case TypeBesoin.Soif:
                        perso.soif = Mathf.Min(100f, perso.soif + gain); break;
                }

                tempsRestant[perso] -= 1f;
                timerRegen[perso] = 1f;
                if (tempsRestant[perso] <= 0f) finis.Add(perso);
            }
        }

        // 🔁 Production
        List<PersonnageData> producteurs = new(timerProduction.Keys);
        producteurs.RemoveAll(p => p == null);
        foreach (var key in new List<PersonnageData>(timerProduction.Keys))
        {
            if (key == null) timerProduction.Remove(key);
        }

        Dictionary<PersonnageData, MetierProductionInfo> aRelancer = new();
        foreach (var producteur in producteurs)
        {
            timerProduction[producteur] -= Time.deltaTime;

            if (timerProduction[producteur] <= 0f)
            {
                MetierProductionInfo info = metierProductions.Find(i => i.metier == producteur.metier);
                if (info != null)
                {
                    bool ajouté = producteur.sacADos.Ajouter(info.ressourceProduite, info.quantiteProduite);
                    if (ajouté)
                    {
                        Debug.Log($"[{name}] {producteur.name} a produit {info.quantiteProduite} {info.ressourceProduite}");
                        aRelancer[producteur] = info;
                    }
                    else
                    {
                        Debug.LogWarning($"[{name}] Sac plein pour {producteur.name} !");
                        GameObject stockage = producteur.TrouverPlusProcheParTag("Stockage");
                        if (stockage != null)
                        {
                            producteur.cibleObjet = stockage;
                            producteur.DeplacerVers(stockage.transform.position);
                        }
                    }
                }
            }
        }

        foreach (var kvp in aRelancer)
        {
            timerProduction[kvp.Key] = kvp.Value.dureeProduction;
        }

        foreach (var p in finis)
        {
            occupants.Remove(p);
            tempsRestant.Remove(p);
            timerRegen.Remove(p);
            p.enRegeneration = false;
        }
    }

    private int GetGainParTick(TypeBesoin besoin)
    {
        return besoin switch
        {
            TypeBesoin.Fatigue => 5,
            TypeBesoin.Faim => 10,
            TypeBesoin.Soif => 10,
            _ => 0
        };
    }

    private float GetTempsTotal(TypeBesoin besoin)
    {
        return besoin switch
        {
            TypeBesoin.Fatigue => 20f,
            TypeBesoin.Faim => 10f,
            TypeBesoin.Soif => 10f,
            _ => 0f
        };
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

        int quantiteApres = ObtenirQuantite(type);
        if (quantiteAvant != quantiteApres && ResourceManager.Instance != null)
        {
            ResourceManager.Instance.NotifyResourceChanged();
        }
    }

    public int ObtenirQuantite(string ressource)
    {
        var r = stock.Find(r => r.nom == ressource);
        return r != null ? r.quantite : 0;
    }

    public int RetirerRessource(string nom, int quantiteVoulu)
    {
        var r = stock.Find(s => s.nom.Equals(nom, System.StringComparison.OrdinalIgnoreCase));
        if (r == null || r.quantite <= 0) return 0;

        int aRetirer = Mathf.Min(quantiteVoulu, r.quantite);
        r.quantite -= aRetirer;
        if (r.quantite <= 0) stock.Remove(r);

        if (aRetirer > 0 && ResourceManager.Instance != null)
        {
            ResourceManager.Instance.NotifyResourceChanged();
        }

        return aRetirer;
    }

    public bool AjouterRessource(string nom, int quantite)
    {
        if (string.IsNullOrEmpty(nom) || quantite <= 0) return false;

        RessourceStockee existante = stock.Find(r => r.nom == nom);
        if (existante != null)
        {
            int ajoutPossible = Mathf.Min(maxParType - existante.quantite, quantite);
            if (ajoutPossible > 0)
            {
                existante.quantite += ajoutPossible;
                ResourceManager.Instance?.NotifyResourceChanged();
                return true;
            }
        }
        else if (stock.Count < maxTypes)
        {
            int ajout = Mathf.Min(maxParType, quantite);
            stock.Add(new RessourceStockee(nom, ajout));
            ResourceManager.Instance?.NotifyResourceChanged();
            return true;
        }

        return false;
    }

    public int GetEspaceLibre(string nom)
    {
        RessourceStockee existante = stock.Find(r => r.nom == nom);
        if (existante != null)
            return maxParType - existante.quantite;
        else if (stock.Count < maxTypes)
            return maxParType;
        return 0;
    }

    public bool PeutAccepter(string nom, int quantite)
    {
        return GetEspaceLibre(nom) >= quantite;
    }
}