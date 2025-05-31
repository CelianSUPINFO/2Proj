using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;


public enum TypeBesoin { Aucun, Fatigue, Faim, Soif }

[RequireComponent(typeof(Collider2D))]
public class BatimentInteractif : MonoBehaviour
{
    [Header("Capacité")] public int capaciteMax = 2;
    [Header("Régénération")] public bool regenereBesoin = false; public TypeBesoin typeBesoin = TypeBesoin.Fatigue;
    [Header("Stockage")] public bool estUnStockage = false;
    private Dictionary<PersonnageData, float> timerProduction = new Dictionary<PersonnageData, float>();
    [SerializeField] private LayerMask layerSol;


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
        new MetierProductionInfo { metier = JobType.FermierAnimaux, ressourceProduite = "Viande", dureeProduction = 20f, quantiteProduite = 5 },
        new MetierProductionInfo { metier = JobType.FermierBle, ressourceProduite = "Ble", dureeProduction = 30f, quantiteProduite = 5 },
        new MetierProductionInfo { metier = JobType.Chercheur, ressourceProduite = "Recherche", dureeProduction = 30f, quantiteProduite = 5 },
        new MetierProductionInfo { metier = JobType.Boulanger, ressourceProduite = "Pain", dureeProduction = 30f, transformation = true, ressourceRequise = "Ble", quantiteRequise = 5, quantiteProduite = 5 },
        new MetierProductionInfo { metier = JobType.Scieur, ressourceProduite = "Planche", dureeProduction = 30f, transformation = true, ressourceRequise = "Bois", quantiteRequise = 5, quantiteProduite = 5 },
        new MetierProductionInfo { metier = JobType.Pecheur, ressourceProduite = "Poisson", dureeProduction = 20f, quantiteProduite = 5 },
        new MetierProductionInfo { metier = JobType.Forgeron, ressourceProduite = "Outil", dureeProduction = 30f, transformation = true, ressourceRequise = "fer", quantiteRequise = 5, quantiteProduite = 5 },
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

    public List<RessourceStockee> stock = new List<RessourceStockee>();
    public int maxTypes = 4;
    public int maxParType = 20;



    private List<PersonnageData> occupants = new List<PersonnageData>();
    private Dictionary<PersonnageData, float> tempsRestant = new Dictionary<PersonnageData, float>();
    private Dictionary<PersonnageData, float> timerRegen = new Dictionary<PersonnageData, float>();

    [Header("Port")]
    public bool estUnPort = false;
    public BatimentInteractif portCible;
    public float delaiTraversée = 10f;

    [Header("Animation bateau")]
    public GameObject bateauPrefab;
    public Transform pointDepartBateau;
    public Transform pointArriveeBateau;




    [Header("Métier associé")]
    public JobType metierAssocie;
    private List<PersonnageData> travailleursActuels = new List<PersonnageData>();

    private bool dejaInitialise = false;
    private bool pointsInitialises = false;



    private void Start()
    {


        if (metierAssocie != JobType.Aucun)
            Debug.Log($"[Batiment {name}] Initialisation et tentative d'assignation de métier.");
        StartCoroutine(AssignerTravailleursInitiaux());
    }



    private IEnumerator AssignerTravailleursInitiaux()
    {
        travailleursActuels.RemoveAll(p => p == null);
        if (travailleursActuels.Count < capaciteMax && metierAssocie != JobType.Aucun)
        {
            for (int i = travailleursActuels.Count; i < capaciteMax; i++)
            {
                PersonnageData candidat = MetierAssignmentManager.Instance.TrouverPersonnageSansMetierEtSansBatiment();
                Debug.Log($"[Batiment {name}] Tentative d'assignation, trouvé: {(candidat != null ? candidat.name : "aucun")}");
                if (candidat != null)
                {
                    candidatsAssignerAuBatiment(candidat);
                }
                else
                {
                    Debug.Log($"[Batiment {name}] Aucun candidat dispo. Attente...");
                }
                yield return new WaitForSeconds(0.1f); // court délai, évite bug assignation en masse
            }
        }
    }

    public void candidatsAssignerAuBatiment(PersonnageData candidat)
    {
        if (travailleursActuels.Count >= capaciteMax) return;
        if (candidat == null) return;
        // Sécurité : évite d'assigner plusieurs fois
        if (travailleursActuels.Contains(candidat)) return;
        if (candidat.metier != JobType.Aucun) return;
        if (candidat.batimentAssigné != null) return;

        candidat.AssignerAuBatiment(this, metierAssocie);
        travailleursActuels.Add(candidat);
        Debug.Log($"[Batiment {name}] Ajout de {candidat.name} ({metierAssocie})");
    }


    public void GererMortTravailleur(PersonnageData mort)
    {
        Debug.Log($"mort et gestion {mort.name}");


        if (!regenereBesoin && !estUnStockage)
        {
            Debug.Log("mort et gestion       ok");
            travailleursActuels.RemoveAll(p => p == null);
            Debug.Log($"[Batiment {name}] Travailleur {mort.name} mort, lancement réassignation");
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

        // --- NE PAS réinitialiser si un timer existe déjà pour ce perso ---
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
                        Debug.Log($"{name}: Pas assez de {info.ressourceRequise} pour transformer. {perso.name} attend.");
                        return;
                    }
                }

                float duree = info.dureeProduction;
                if (perso.aOutil) duree /= 2f;
                timerProduction[perso] = duree;

                Debug.Log($"[PROD] {perso.name} commence la production de {info.ressourceProduite} ({info.dureeProduction}s)");
            }
        }

        // Si c'est un port, initier traversée
        if (estUnPort && portCible != null)
        {
            StartCoroutine(TraverserAvecBateau(perso));
            
        }



        // 🔁 Régénération, si activée
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
        timerProduction.Remove(perso); // <-- Ajoute cette ligne !
        perso.enRegeneration = false;
        if (!perso.gameObject.activeInHierarchy)
        {
            perso.gameObject.SetActive(true);
        }
        
    }


    private void Update()
    {

        if (estUnPort && portCible == null)
        {
            portCible = TrouverPortLePlusProche();
        }

        if (estUnPort && portCible != null && !pointsInitialises)
        {
            InitialiserPointsBateau();
            pointsInitialises = true;
        }


        occupants.RemoveAll(p => p == null);


        List<PersonnageData> finis = new();
        List<PersonnageData> occupantsSnapshot = new(occupants); // éviter modification pendant boucle

        // 🔁 1. Régénération
        if (regenereBesoin)
        {
            foreach (PersonnageData perso in occupantsSnapshot)
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
        }

        // 🔁 2. Production (séparé de la régénération)
        List<PersonnageData> producteurs = new(timerProduction.Keys);
        producteurs.RemoveAll(p => p == null);
        foreach (var key in new List<PersonnageData>(timerProduction.Keys))
        {
            if (key == null)
                timerProduction.Remove(key);
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
                        Debug.Log($"[{name}] {producteur.name} a produit {info.quantiteProduite} {info.ressourceProduite} (dans son sac)");
                        if (info.ressourceProduite == "Outil")
                        {
                            int outilsDistribues = 0;
                            for (int i = 0; i < info.quantiteProduite; i++)
                            {
                                var sansOutil = FindObjectsOfType<PersonnageData>()
                                    .Where(p => !p.aOutil)
                                    .OrderBy(_ => UnityEngine.Random.value)
                                    .FirstOrDefault();

                                if (sansOutil != null)
                                {
                                    sansOutil.aOutil = true;
                                    outilsDistribues++;
                                    Debug.Log($"[OUTIL] {sansOutil.name} a reçu un outil !");
                                }
                            }

                            if (outilsDistribues > 0)
                            {
                                Debug.Log($"[OUTIL] {outilsDistribues} outils ont été distribués à des personnages.");
                            }
                        }

                        aRelancer[producteur] = info;
                    }
                    else
                    {
                        Debug.LogWarning($"[{name}] {producteur.name} n'a pas pu stocker {info.ressourceProduite} (sac plein)");

                        GameObject stockage = producteur.TrouverPlusProcheParTag("Stockage");
                        if (stockage != null)
                        {
                            producteur.cibleObjet = stockage;
                            producteur.DeplacerVers(stockage.transform.position);
                            // Assure-toi que le personnage passera en mode AllerStockage
                        }

                        // ❌ Pas de relance du timer ici pour éviter production infinie quand sac est plein
                    }
                }
            }
        }

        // 🔁 3. Redémarrer les timers de production
        foreach (var kvp in aRelancer)
        {
            float duree = kvp.Value.dureeProduction;
            if (kvp.Key.aOutil) duree /= 2f;
            timerProduction[kvp.Key] = duree;

        }

        // 🔁 4. Terminer les régénérations
        foreach (var p in finis)
        {
            occupants.Remove(p);
            tempsRestant.Remove(p);
            timerRegen.Remove(p);
            p.TerminerRegeneration(); // ✅ Utilise la nouvelle méthode
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

    private IEnumerator TraverserAvecBateau(PersonnageData perso)
    {
        Debug.Log($"[PORT] {perso.name} embarque sur le bateau...");

        // 🔐 Vérification de sécurité
        if (portCible == null || portCible == this)
        {
            Debug.LogWarning($"[PORT] {name} → portCible invalide !");
            yield break;
        }

        // 🔄 Initialisation des points
        InitialiserPointsBateau();
        portCible.InitialiserPointsBateau();

        if (pointDepartBateau == null || portCible.pointDepartBateau == null)
        {
            Debug.LogError($"[PORT] Points de bateau manquants !");
            yield break;
        }

        Vector3 start = pointDepartBateau.position;
        Vector3 end = portCible.pointDepartBateau.position;

        // 🔄 Stop si distance trop courte
        if (Vector3.Distance(start, end) < 0.1f)
        {
            Debug.LogWarning($"[PORT] {name} → Points trop proches");
            yield break;
        }

        // 🔄 Cacher le personnage
        occupants.Remove(perso);
        perso.gameObject.SetActive(false);

        // 🛶 Créer le bateau
        GameObject bateau = Instantiate(bateauPrefab, start, Quaternion.identity);
        if (bateau == null)
        {
            Debug.LogError("[PORT] Bateau non instancié !");
            yield break;
        }

        float duration = delaiTraversée;
        float speed = Vector3.Distance(start, end) / duration;

        // 🌊 Mouvement du bateau
        while (bateau != null && Vector3.Distance(bateau.transform.position, end) > 0.05f)
        {
            bateau.transform.position = Vector3.MoveTowards(bateau.transform.position, end, speed * Time.deltaTime);
            yield return null;
        }

        Destroy(bateau);

        // 📍 Trouver une position de sol libre
        Vector3 debarquement = portCible.TrouverSolLePlusProche();
        perso.transform.position = debarquement;




        Debug.Log($"[PORT] {perso.name} a débarqué à {debarquement}");
        perso.QuitterBatiment();
       



    }



    private BatimentInteractif TrouverPortLePlusProche()
    {
        float minDist = float.MaxValue;
        BatimentInteractif plusProche = null;

        foreach (var b in FindObjectsOfType<BatimentInteractif>())
        {
            if (b == this || !b.estUnPort) continue;

            float dist = Vector3.Distance(transform.position, b.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                plusProche = b;
            }
        }

        return plusProche;
    }

    private void InitialiserPointsBateau()
    {
        if (dejaInitialise) return;
        dejaInitialise = true;

        // 🔹 Création du point de départ
        if (pointDepartBateau == null)
        {
            GameObject depart = new GameObject($"PointDepart_{name}");
            depart.transform.position = transform.position + new Vector3(-1f, -0.5f, 0);
            depart.transform.SetParent(GameObject.Find("GameScene")?.transform); // pour ne pas détruire avec le port
            pointDepartBateau = depart.transform;
            Debug.Log($"[PORT INIT] {name} → PointDépart créé");
        }

        // 🔹 Création automatique du point d'arrivée si portCible existe
        if (portCible != null && portCible != this)
        {
            portCible.InitialiserPointsBateau(); // Sécurisé avec dejaInitialise
            if (portCible.pointDepartBateau != null)
            {
                pointArriveeBateau = portCible.pointDepartBateau;
                Debug.Log($"[PORT INIT] {name} → pointArrivee = {portCible.name}");
            }
            else
            {
                Debug.LogWarning($"[PORT INIT] {portCible.name} → pas de pointDepartBateau !");
            }
        }
    }


  
    Vector3 TrouverSolLePlusProche()
    {
        float rayon = 0.5f;
        int essais = 100;

        for (int i = 0; i < essais; i++)
        {
            Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
            Vector3 testPos = transform.position + (Vector3)(direction * rayon);

            if (Physics2D.OverlapCircle(testPos, 0.1f, layerSol))
                return testPos;

            rayon += 0.1f;
        }

        return transform.position;
    }





}