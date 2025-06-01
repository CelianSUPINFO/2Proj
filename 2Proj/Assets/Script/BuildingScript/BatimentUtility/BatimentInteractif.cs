using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

// Énumération pour les différents types de besoins que peut satisfaire un bâtiment
public enum TypeBesoin { Aucun, Fatigue, Faim, Soif }

// Le bâtiment doit avoir un Collider2D pour détecter les personnages
[RequireComponent(typeof(Collider2D))]
public class BatimentInteractif : MonoBehaviour
{
    // Layer pour détecter les autres bâtiments
    private LayerMask layerBatiment;

    // SECTION CONFIGURATION DU BÂTIMENT
    [Header("Capacité")] 
    public int capaciteMax = 2; // Nombre max de personnages dans le bâtiment
    
    [Header("Régénération")] 
    public bool regenereBesoin = false; // Est-ce que ce bâtiment régénère un besoin ?
    public TypeBesoin typeBesoin = TypeBesoin.Fatigue; // Quel besoin il régénère
    
    [Header("Stockage")] 
    public bool estUnStockage = false; // Est-ce un entrepôt ?
    
    // Dictionnaire pour suivre le temps de production de chaque personnage
    private Dictionary<PersonnageData, float> timerProduction = new Dictionary<PersonnageData, float>();
    
    // Layer pour détecter le sol (pour placement des personnages)
    [SerializeField] private LayerMask layerSol;

    // CLASSE POUR DÉFINIR LES PRODUCTIONS PAR MÉTIER
    [System.Serializable]
    public class MetierProductionInfo
    {
        public JobType metier; // Quel métier peut utiliser ce bâtiment
        public string ressourceProduite; // Qu'est-ce qu'il produit
        public float dureeProduction; // Combien de temps ça prend
        public bool transformation; // Est-ce qu'il transforme des ressources ?
        public string ressourceRequise; // Ressource nécessaire pour transformer
        public int quantiteRequise; // Combien il faut de ressource
        public int quantiteProduite; // Combien on obtient à la fin
    }

    // LISTE DE TOUTES LES PRODUCTIONS POSSIBLES
    // Chaque métier a ses propres règles de production
    public List<MetierProductionInfo> metierProductions = new List<MetierProductionInfo>()
    {
        // Productions de base (extraction)
        new MetierProductionInfo { metier = JobType.Bucheron, ressourceProduite = "Bois", dureeProduction = 20f, quantiteProduite = 5 },
        new MetierProductionInfo { metier = JobType.CarrierPierre, ressourceProduite = "Pierre", dureeProduction = 20f, quantiteProduite = 5 },
        new MetierProductionInfo { metier = JobType.CarrierFer, ressourceProduite = "Fer", dureeProduction = 30f, quantiteProduite = 5 },
        new MetierProductionInfo { metier = JobType.CarrierOr, ressourceProduite = "Or", dureeProduction = 40f, quantiteProduite = 5 },
        new MetierProductionInfo { metier = JobType.FermierAnimaux, ressourceProduite = "Viande", dureeProduction = 20f, quantiteProduite = 5 },
        new MetierProductionInfo { metier = JobType.FermierBle, ressourceProduite = "Ble", dureeProduction = 30f, quantiteProduite = 5 },
        new MetierProductionInfo { metier = JobType.Chercheur, ressourceProduite = "Recherche", dureeProduction = 30f, quantiteProduite = 5 },
        new MetierProductionInfo { metier = JobType.Pecheur, ressourceProduite = "Poisson", dureeProduction = 20f, quantiteProduite = 5 },
        
        // Productions avec transformation (besoin de ressources)
        new MetierProductionInfo { metier = JobType.Boulanger, ressourceProduite = "Pain", dureeProduction = 30f, transformation = true, ressourceRequise = "Ble", quantiteRequise = 5, quantiteProduite = 5 },
        new MetierProductionInfo { metier = JobType.Scieur, ressourceProduite = "Planche", dureeProduction = 30f, transformation = true, ressourceRequise = "Bois", quantiteRequise = 5, quantiteProduite = 5 },
        new MetierProductionInfo { metier = JobType.Forgeron, ressourceProduite = "Outil", dureeProduction = 30f, transformation = true, ressourceRequise = "fer", quantiteRequise = 5, quantiteProduite = 5 },
    };

    // Données du bâtiment (âge de déblocage, etc.)
    public BuildingData data;

    // CLASSE POUR GÉRER LE STOCKAGE DES RESSOURCES
    [System.Serializable]
    public class RessourceStockee
    {
        public string nom; // Nom de la ressource (ex: "Bois")
        public int quantite; // Combien on en a

        // Constructeur pour créer une nouvelle ressource stockée
        public RessourceStockee(string nom, int quantite)
        {
            this.nom = nom;
            this.quantite = quantite;
        }
    }

    // VARIABLES DE STOCKAGE
    public List<RessourceStockee> stock = new List<RessourceStockee>(); // Liste des ressources stockées
    public int maxTypes = 4; // Nombre max de types de ressources différentes
    public int maxParType = 20; // Quantité max par type de ressource

    // LISTES POUR GÉRER LES PERSONNAGES
    private List<PersonnageData> occupants = new List<PersonnageData>(); // Qui est dans le bâtiment
    private Dictionary<PersonnageData, float> tempsRestant = new Dictionary<PersonnageData, float>(); // Temps restant pour régénération
    private Dictionary<PersonnageData, float> timerRegen = new Dictionary<PersonnageData, float>(); // Timer pour régénération

    // SECTION PORT (pour transport entre îles)
    [Header("Port")]
    public bool estUnPort = false; // Est-ce un port ?
    public BatimentInteractif portCible; // Vers quel port on va
    public float delaiTraversée = 10f; // Durée du voyage

    [Header("Animation bateau")]
    public GameObject bateauPrefab; // Prefab du bateau
    public Transform pointDepartBateau; // D'où part le bateau
    public Transform pointArriveeBateau; // Où arrive le bateau

    // SECTION MÉTIER (pour assigner des travailleurs)
    [Header("Métier associé")]
    public JobType metierAssocie; // Quel métier travaille ici
    private List<PersonnageData> travailleursActuels = new List<PersonnageData>(); // Qui travaille ici

    // Variables de contrôle
    private bool dejaInitialise = false; // Pour éviter double initialisation
    private bool pointsInitialises = false; // Points bateau initialisés ?
    public bool estPlace = false; // Le bâtiment est-il placé ?

    // MÉTHODE APPELÉE AU DÉMARRAGE
    private void Start()
    {
        // Initialise le layer pour détecter les bâtiments
        layerBatiment = LayerMask.GetMask("Buildings");

        // Si ce bâtiment a un métier associé, on essaie d'assigner des travailleurs
        if (metierAssocie != JobType.Aucun)
        {
            Debug.Log($"[Batiment {name}] Initialisation et tentative d'assignation de métier.");
            StartCoroutine(AssignerTravailleursInitiaux());
        }
    }

    // COROUTINE POUR ASSIGNER DES TRAVAILLEURS AU DÉMARRAGE
    private IEnumerator AssignerTravailleursInitiaux()
    {
        // Nettoie la liste des travailleurs (enlève les null)
        travailleursActuels.RemoveAll(p => p == null);
        
        // Si on n'est pas à capacité max et qu'on a un métier
        if (travailleursActuels.Count < capaciteMax && metierAssocie != JobType.Aucun)
        {
            // Pour chaque place libre
            for (int i = travailleursActuels.Count; i < capaciteMax; i++)
            {
                // Cherche un personnage sans métier et sans bâtiment
                PersonnageData candidat = MetierAssignmentManager.Instance.TrouverPersonnageSansMetierEtSansBatiment();
                Debug.Log($"[Batiment {name}] Tentative d'assignation, trouvé: {(candidat != null ? candidat.name : "aucun")}");
                
                if (candidat != null)
                {
                    // Assigne le candidat à ce bâtiment
                    candidatsAssignerAuBatiment(candidat);
                }
                else
                {
                    Debug.Log($"[Batiment {name}] Aucun candidat dispo. Attente...");
                }
                
                // Petit délai pour éviter les bugs d'assignation en masse
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    // MÉTHODE POUR ASSIGNER UN CANDIDAT AU BÂTIMENT
    public void candidatsAssignerAuBatiment(PersonnageData candidat)
    {
        // Vérifications de sécurité
        if (travailleursActuels.Count >= capaciteMax) return; // Déjà plein
        if (candidat == null) return; // Pas de candidat
        if (travailleursActuels.Contains(candidat)) return; // Déjà assigné
        if (candidat.metier != JobType.Aucun) return; // A déjà un métier
        if (candidat.batimentAssigné != null) return; // A déjà un bâtiment

        // Assigne le personnage au bâtiment et lui donne le métier
        candidat.AssignerAuBatiment(this, metierAssocie);
        travailleursActuels.Add(candidat);
        Debug.Log($"[Batiment {name}] Ajout de {candidat.name} ({metierAssocie})");
    }

    // MÉTHODE APPELÉE QUAND UN TRAVAILLEUR MEURT
    public void GererMortTravailleur(PersonnageData mort)
    {
        Debug.Log($"mort et gestion {mort.name}");

        // Seulement pour les bâtiments de production (pas régénération/stockage)
        if (!regenereBesoin && !estUnStockage)
        {
            Debug.Log("mort et gestion       ok");
            // Nettoie la liste et relance l'assignation
            travailleursActuels.RemoveAll(p => p == null);
            Debug.Log($"[Batiment {name}] Travailleur {mort.name} mort, lancement réassignation");
            StartCoroutine(AssignerTravailleursInitiaux());
        }
    }

    // VÉRIFIE SI LE BÂTIMENT A DE LA PLACE
    public bool EstDisponible()
    {
        return occupants.Count < capaciteMax;
    }

    // MÉTHODE APPELÉE QUAND UN PERSONNAGE ENTRE DANS LE BÂTIMENT
    private void OnTriggerEnter2D(Collider2D other)
    {     
        // Vérifie si c'est un personnage
        if (!other.TryGetComponent(out PersonnageData perso)) return;
        // Vérifie s'il n'est pas déjà dedans et s'il y a de la place
        if (occupants.Contains(perso) || !EstDisponible()) return;

        // Ajoute le personnage à la liste des occupants
        occupants.Add(perso);
        perso.cibleObjet = gameObject;

        // GESTION DE LA PRODUCTION
        // Ne réinitialise pas si un timer existe déjà pour éviter les bugs
        if (!timerProduction.ContainsKey(perso))
        {
            // Cherche les infos de production pour ce métier
            MetierProductionInfo info = metierProductions.Find(i => i.metier == perso.metier);
            if (info != null)
            {
                // Si c'est une transformation, vérifie qu'on a les ressources
                if (info.transformation)
                {
                    int retiré = RetirerRessource(info.ressourceRequise, info.quantiteRequise);
                    if (retiré < info.quantiteRequise)
                    {
                        Debug.Log($"{name}: Pas assez de {info.ressourceRequise} pour transformer. {perso.name} attend.");
                        return;
                    }
                }

                // Calcule la durée (avec bonus d'âge et d'outil)
                float duree = AppliquerBonusAge(info.dureeProduction);
                if (perso.aOutil) duree /= 2f; // Les outils divisent le temps par 2
                timerProduction[perso] = duree;

                Debug.Log($"[PROD] {perso.name} commence la production de {info.ressourceProduite} ({info.dureeProduction}s)");
            }
        }

        // GESTION DES PORTS
        if (estUnPort && portCible != null)
        {
            StartCoroutine(TraverserAvecBateau(perso));
        }

        // GESTION DE LA RÉGÉNÉRATION
        if (regenereBesoin)
        {
            // Initialise les timers de régénération
            tempsRestant[perso] = GetTempsTotal(typeBesoin);
            timerRegen[perso] = 1f; // Régénère chaque seconde
            perso.enRegeneration = true;
        }

        // GESTION DU STOCKAGE
        if (estUnStockage && perso.sacADos.quantite > 0)
        {
            StockerDepuis(perso); // Transfert le contenu du sac vers le stockage
        }
    }

    // MÉTHODE APPELÉE QUAND UN PERSONNAGE SORT DU BÂTIMENT
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.TryGetComponent(out PersonnageData perso)) return;

        // Nettoie toutes les données liées à ce personnage
        occupants.Remove(perso);
        tempsRestant.Remove(perso);
        timerRegen.Remove(perso);
        timerProduction.Remove(perso); // Important : arrête la production
        perso.enRegeneration = false;
    }

    // VÉRIFIE SI CE PORT EST DÉJÀ LA CIBLE D'UN AUTRE PORT
    public bool EstDejaCible()
    {
        return FindObjectsOfType<BatimentInteractif>()
            .Any(p => p != this && p.estUnPort && p.portCible == this);
    }

    // MÉTHODE UPDATE - APPELÉE À CHAQUE FRAME
    private void Update()
    {
        // GESTION DES PORTS
        // Si c'est un port et qu'il n'a pas de cible valide, en cherche une
        if (estUnPort && (portCible == null || portCible.EstDejaCible()))
        {
            portCible = TrouverPortLePlusPropreEtLibre();
        }

        // Initialise les points bateau si nécessaire
        if (estUnPort && portCible != null && !pointsInitialises)
        {
            InitialiserPointsBateau();
            pointsInitialises = true;
        }

        // Nettoie la liste des occupants (enlève les personnages détruits)
        occupants.RemoveAll(p => p == null);

        // Listes pour gérer les personnages qui ont fini
        List<PersonnageData> finis = new();
        List<PersonnageData> occupantsSnapshot = new(occupants); // Copie pour éviter modifications pendant boucle

        // 1. GESTION DE LA RÉGÉNÉRATION
        if (regenereBesoin)
        {
            foreach (PersonnageData perso in occupantsSnapshot)
            {
                if (!tempsRestant.ContainsKey(perso)) continue;

                // Décompte le timer de régénération
                timerRegen[perso] -= Time.deltaTime;

                // Si une seconde s'est écoulée
                if (timerRegen[perso] <= 0f)
                {
                    // Calcule le gain selon le type de besoin
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

                    // Réduit le temps restant et remet le timer à 1 seconde
                    tempsRestant[perso] -= 1f;
                    timerRegen[perso] = 1f;

                    // Si la régénération est terminée
                    if (tempsRestant[perso] <= 0f)
                    {
                        finis.Add(perso);
                    }
                }
            }
        }

        // 2. GESTION DE LA PRODUCTION
        List<PersonnageData> producteurs = new(timerProduction.Keys);
        producteurs.RemoveAll(p => p == null); // Nettoie les producteurs null
        
        // Nettoie aussi le dictionnaire des timers
        foreach (var key in new List<PersonnageData>(timerProduction.Keys))
        {
            if (key == null)
                timerProduction.Remove(key);
        }

        // Dictionnaire pour stocker les productions à relancer
        Dictionary<PersonnageData, MetierProductionInfo> aRelancer = new();

        // Pour chaque producteur
        foreach (var producteur in producteurs)
        {
            // Décompte le timer de production
            timerProduction[producteur] -= Time.deltaTime;

            // Si la production est terminée
            if (timerProduction[producteur] <= 0f)
            {
                // Récupère les infos de production
                MetierProductionInfo info = metierProductions.Find(i => i.metier == producteur.metier);
                if (info != null)
                {
                    // Essaie d'ajouter la ressource produite au sac
                    bool ajouté = producteur.sacADos.Ajouter(info.ressourceProduite, info.quantiteProduite);
                    if (ajouté)
                    {
                        Debug.Log($"[{name}] {producteur.name} a produit {info.quantiteProduite} {info.ressourceProduite} (dans son sac)");
                        
                        // GESTION SPÉCIALE DES OUTILS
                        if (info.ressourceProduite == "Outil")
                        {
                            int outilsDistribues = 0;
                            // Distribue les outils aux personnages qui n'en ont pas
                            for (int i = 0; i < info.quantiteProduite; i++)
                            {
                                var sansOutil = FindObjectsOfType<PersonnageData>()
                                    .Where(p => !p.aOutil)
                                    .OrderBy(_ => UnityEngine.Random.value) // Ordre aléatoire
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

                        // Marque cette production pour relance
                        aRelancer[producteur] = info;
                    }
                    else
                    {
                        // Sac plein - envoie vers un stockage
                        Debug.LogWarning($"[{name}] {producteur.name} n'a pas pu stocker {info.ressourceProduite} (sac plein)");

                        GameObject stockage = producteur.TrouverPlusProcheParTag("Stockage");
                        if (stockage != null)
                        {
                            producteur.cibleObjet = stockage;
                            producteur.DeplacerVers(stockage.transform.position);
                        }

                        // Pas de relance du timer pour éviter production infinie
                    }
                }
            }
        }

        // 3. REDÉMARRE LES TIMERS DE PRODUCTION
        foreach (var kvp in aRelancer)
        {
            float duree = kvp.Value.dureeProduction;
            if (kvp.Key.aOutil) duree /= 2f; // Bonus outil
            timerProduction[kvp.Key] = duree;
        }

        // 4. TERMINE LES RÉGÉNÉRATIONS
        foreach (var p in finis)
        {
            occupants.Remove(p);
            tempsRestant.Remove(p);
            timerRegen.Remove(p);
            p.TerminerRegeneration(); // Méthode pour finir proprement
        }
    }

    // CALCULE LE GAIN PAR SECONDE SELON LE TYPE DE BESOIN
    private int GetGainParTick(TypeBesoin besoin)
    {
        switch (besoin)
        {
            case TypeBesoin.Fatigue: return 5; // Récupère 5 points de fatigue par seconde
            case TypeBesoin.Faim: return 10; // Récupère 10 points de faim par seconde
            case TypeBesoin.Soif: return 10; // Récupère 10 points de soif par seconde
            default: return 0;
        }
    }

    // CALCULE LE TEMPS TOTAL DE RÉGÉNÉRATION
    private float GetTempsTotal(TypeBesoin besoin)
    {
        switch (besoin)
        {
            case TypeBesoin.Fatigue: return 20f; // 20 secondes pour se reposer
            case TypeBesoin.Faim: return 10f; // 10 secondes pour manger
            case TypeBesoin.Soif: return 10f; // 10 secondes pour boire
            default: return 0f;
        }
    }

    // TRANSFERT LES RESSOURCES DU SAC DU PERSONNAGE VERS LE STOCKAGE
    private void StockerDepuis(PersonnageData perso)
    {
        string type = perso.sacADos.ressourceActuelle;
        int quantite = perso.sacADos.quantite;

        if (string.IsNullOrEmpty(type)) return; // Pas de ressource à stocker

        int quantiteAvant = ObtenirQuantite(type);

        // Cherche si on a déjà ce type de ressource
        RessourceStockee existante = stock.Find(r => r.nom == type);

        if (existante != null)
        {
            // On a déjà ce type - ajoute ce qu'on peut
            int ajoutPossible = Mathf.Min(maxParType - existante.quantite, quantite);
            existante.quantite += ajoutPossible;
            perso.sacADos.quantite -= ajoutPossible;
        }
        else if (stock.Count < maxTypes)
        {
            // Nouveau type de ressource - crée une nouvelle entrée
            int ajout = Mathf.Min(maxParType, quantite);
            stock.Add(new RessourceStockee(type, ajout));
            perso.sacADos.quantite -= ajout;
        }

        // Si le sac est vide, le nettoie
        if (perso.sacADos.quantite <= 0)
        {
            perso.sacADos.Vider();
        }

        // Notifie le gestionnaire de ressources des changements
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

    // RETOURNE LA QUANTITÉ D'UNE RESSOURCE STOCKÉE
    public int ObtenirQuantite(string ressource)
    {
        var r = stock.Find(r => r.nom == ressource);
        return r != null ? r.quantite : 0;
    }

    // RETOURNE TOUTES LES RESSOURCES STOCKÉES (COPIE)
    public List<RessourceStockee> GetAllStockedResources()
    {
        return new List<RessourceStockee>(stock);
    }

    // RETIRE UNE QUANTITÉ D'UNE RESSOURCE DU STOCKAGE
    public int RetirerRessource(string nom, int quantiteVoulu)
    {
        // Cherche la ressource (ignore la casse)
        var r = stock.Find(s => s.nom.Equals(nom, System.StringComparison.OrdinalIgnoreCase));
        if (r == null || r.quantite <= 0) return 0; // Pas trouvé ou vide

        int quantiteAvant = r.quantite;
        int aRetirer = Mathf.Min(quantiteVoulu, r.quantite); // Prend ce qu'on peut
        r.quantite -= aRetirer;

        // Si la ressource est épuisée, la supprime de la liste
        if (r.quantite <= 0)
        {
            stock.Remove(r);
        }

        // Notifie le gestionnaire de ressources
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

    // AJOUTE DIRECTEMENT DES RESSOURCES AU STOCKAGE
    // Utile pour la production de bâtiments ou les récompenses
    public bool AjouterRessource(string nom, int quantite)
    {
        if (string.IsNullOrEmpty(nom) || quantite <= 0) return false;

        int quantiteAvant = ObtenirQuantite(nom);
        RessourceStockee existante = stock.Find(r => r.nom == nom);

        if (existante != null)
        {
            // Type existant - ajoute ce qu'on peut
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
            // Nouveau type
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

    // RETOURNE L'ESPACE LIBRE POUR UN TYPE DE RESSOURCE
    public int GetEspaceLibre(string nom)
    {
        RessourceStockee existante = stock.Find(r => r.nom == nom);
        if (existante != null)
        {
            // Type existant - espace restant
            return maxParType - existante.quantite;
        }
        else if (stock.Count < maxTypes)
        {
            // Nouveau type possible - espace max
            return maxParType;
        }
        return 0; // Pas d'espace
    }

    // VÉRIFIE SI LE STOCKAGE PEUT ACCEPTER UNE CERTAINE QUANTITÉ
    public bool PeutAccepter(string nom, int quantite)
    {
        return GetEspaceLibre(nom) >= quantite;
    }

    // COROUTINE POUR GÉRER LA TRAVERSÉE EN BATEAU
    private IEnumerator TraverserAvecBateau(PersonnageData perso)
    {
        Debug.Log($"[PORT] {perso.name} embarque sur le bateau...");

        // Vérifications de sécurité
        if (portCible == null || portCible == this)
        {
            Debug.LogWarning($"[PORT] {name} → portCible invalide !");
            yield break;
        }

        // Initialise les points de départ et d'arrivée
        InitialiserPointsBateau();
        portCible.InitialiserPointsBateau();

        if (pointDepartBateau == null || portCible.pointDepartBateau == null)
        {
            Debug.LogError($"[PORT] Points de bateau manquants !");
            yield break;
        }

        Vector3 start = pointDepartBateau.position;
        Vector3 end = portCible.pointDepartBateau.position;

        //  Stop si distance trop courte
        if (Vector3.Distance(start, end) < 0.1f)
        {
            Debug.LogWarning($"[PORT] {name} → Points trop proches");
            yield break;
        }

        //  Cacher le personnage
        occupants.Remove(perso);
        perso.gameObject.SetActive(false);

        //  Créer le bateau
        GameObject bateau = Instantiate(bateauPrefab, start, Quaternion.identity);
        if (bateau == null)
        {
            Debug.LogError("[PORT] Bateau non instancié !");
            yield break;
        }

        float duration = delaiTraversée;
        float speed = Vector3.Distance(start, end) / duration;

        // Mouvement du bateau
        while (bateau != null && Vector3.Distance(bateau.transform.position, end) > 0.05f)
        {
            bateau.transform.position = Vector3.MoveTowards(bateau.transform.position, end, speed * Time.deltaTime);
            yield return null;
        }

        Destroy(bateau);

        // Trouver une position de sol libre
        Vector3 debarquement = portCible.TrouverSolLePlusProche();
        perso.transform.position = debarquement;




        Debug.Log($"[PORT] {perso.name} a débarqué à {debarquement}");
        perso.gameObject.SetActive(true);

        perso.QuitterBatiment();
        perso.cibleObjet = null;
       



    }





   // Initialise les points de départ et d'arrivée pour le transport par bateau
    private void InitialiserPointsBateau()
    {
        if (dejaInitialise) return;
        dejaInitialise = true;

        // Crée un point de départ si non défini
        if (pointDepartBateau == null)
        {
            GameObject depart = new GameObject($"PointDepart_{name}");
            depart.transform.position = transform.position + new Vector3(-1f, -0.5f, 0);
            depart.transform.SetParent(GameObject.Find("GameScene")?.transform); // Pour éviter qu’il soit détruit avec le bâtiment
            pointDepartBateau = depart.transform;
            Debug.Log($"[PORT INIT] {name} → PointDépart créé");
        }

        // Définit automatiquement le point d’arrivée en fonction du port cible
        if (portCible != null && portCible != this)
        {
            portCible.InitialiserPointsBateau(); // Sécurisé grâce à dejaInitialise
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

    // Recherche une position au sol libre autour du bâtiment (ex : pour débarquement)
    private Vector3 TrouverSolLePlusProche()
    {
        float rayon = 2f;
        int essais = 100;
        float rayonDetection = 0.1f;

        for (int i = 0; i < essais; i++)
        {
            Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
            Vector3 testPos = transform.position + (Vector3)(direction * rayon);

            bool solTrouve = Physics2D.OverlapCircle(testPos, rayonDetection, layerSol);
            bool batimentPresent = Physics2D.OverlapCircle(testPos, rayonDetection, layerBatiment);

            if (solTrouve && !batimentPresent)
                return testPos;

            rayon += 0.1f; // élargit progressivement la zone de recherche
        }

        return transform.position; // Retour par défaut si aucun sol libre trouvé
    }

    // Applique un bonus de vitesse de production en fonction de l'âge technologique du bâtiment
    private float AppliquerBonusAge(float baseDuree)
    {
        if (data == null) return baseDuree;

        switch (data.unlockAge)
        {
            case GameAge.AncientAge:
                return baseDuree * 0.8f;
            case GameAge.MedievalAge:
                return baseDuree * 0.65f;
            case GameAge.IndustrialAge:
                return baseDuree * 0.5f;
            default:
                return baseDuree;
        }
    }

    // Cherche un autre port disponible (non ciblé) le plus proche pour établir une traversée
    private BatimentInteractif TrouverPortLePlusPropreEtLibre()
    {
        float minDist = float.MaxValue;
        BatimentInteractif plusProche = null;

        foreach (var b in FindObjectsOfType<BatimentInteractif>())
        {
            if (b == this || !b.estUnPort || b.EstDejaCible())
                continue;

            float dist = Vector3.Distance(transform.position, b.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                plusProche = b;
            }
        }

        return plusProche;
    }


}