using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;


public enum JobType
{
    Aucun,
    Bucheron,
    CarrierPierre,
    CarrierFer,
    CarrierOr,
    FermierAnimaux,
    FermierBle,
    Chercheur,
    Boulanger,
    Scieur,
    Pecheur,
    Forgeron,
}

[System.Serializable]
public class Backpack
{
    public string ressourceActuelle = null;
    public int quantite = 0;
    public int capaciteMax = 5;

    private PersonnageData proprietaire;

    public void SetProprietaire(PersonnageData perso)
    {
        proprietaire = perso;
    }

    public bool PeutAjouter(string ressource)
    {
        bool vide = string.IsNullOrEmpty(ressourceActuelle);
        bool peut = vide || ressourceActuelle == ressource;

        if (!peut)
            Debug.LogWarning($"[BACKPACK] Incompatible: a déjà {ressourceActuelle ?? "rien"}, tenté {ressource}");

        return peut;
    }

    public bool Ajouter(string ressource, int quantiteAjoutee)
    {
        if (!PeutAjouter(ressource)) return false;

        int espaceRestant = capaciteMax - quantite;
        if (espaceRestant <= 0) return false;

        int aAjouter = Mathf.Min(quantiteAjoutee, espaceRestant);
        if (aAjouter <= 0) return false;

        int quantiteAvant = quantite;
        ressourceActuelle = string.IsNullOrEmpty(ressourceActuelle) ? ressource : ressourceActuelle;
        quantite += aAjouter;

        // 🔥 NOUVEAU : Notifier le ResourceManager
        if (quantiteAvant != quantite)
        {
            Debug.Log($"[Sac {proprietaire?.name ?? "Unknown"}] {ressource}: {quantiteAvant} → {quantite}");
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.NotifyResourceChanged();
            }
        }

        return true;
    }

    public void Vider()
    {
        bool hadResources = quantite > 0;
        string oldResource = ressourceActuelle;
        
        ressourceActuelle = null;
        quantite = 0;

        //  NOUVEAU : Notifier le ResourceManager
        if (hadResources)
        {
            Debug.Log($"[Sac {proprietaire?.name ?? "Unknown"}] Vidé (contenait {oldResource})");
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.NotifyResourceChanged();
            }
        }
    }

    /// <summary>
    /// 🔥 NOUVELLE MÉTHODE : Retirer une quantité spécifique du sac
    /// </summary>
    public int Retirer(int quantiteVoulue)
    {
        if (quantite <= 0 || quantiteVoulue <= 0) return 0;

        int quantiteAvant = quantite;
        int aRetirer = Mathf.Min(quantiteVoulue, quantite);
        quantite -= aRetirer;

        if (quantite <= 0)
        {
            Vider();  // Cela déclenchera déjà la notification
        }
        else
        {
            // Notifier seulement si on ne vide pas complètement
            Debug.Log($"[Sac {proprietaire?.name ?? "Unknown"}] {ressourceActuelle}: {quantiteAvant} → {quantite}");
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.NotifyResourceChanged();
            }
        }

        return aRetirer;
    }

    public int GetEspaceLibre()
    {
        return capaciteMax - quantite;
    }

    public bool EstPlein()
    {
        return quantite >= capaciteMax;
    }

    public bool EstVide()
    {
        return quantite <= 0 || string.IsNullOrEmpty(ressourceActuelle);
    }
}

public class PersonnageData : MonoBehaviour
{
    public BatimentInteractif batimentAssigné;
    [Header("Équipement")]
    public bool aOutil = false;

    [HideInInspector] public bool enRegeneration = false;
    [Header("Stats")] public float vie = 100f, faim = 100f, soif = 100f, fatigue = 100f;
    [Header("Métier")] public JobType metier = JobType.Aucun;
    [Header("Sac à dos")] public Backpack sacADos = new Backpack();
    [Header("Déplacement")] public float vitesse = 1.5f; public LayerMask layerSol;
    [Header("Contournement")] public LayerMask layerBatiments; public float rayonDetection = 1.2f; public float forceContournement = 2f;
    private Vector3 cible; private float timer; public GameObject cibleObjet; public static event Action<PersonnageData> OnPersonnageMort;
    private Vector3 directionContournement = Vector3.zero; private float timerContournement = 0f; private bool enContournement = false;

    private enum EtatPerso { Normal, Collecte, AttenteCollecte, AllerStockage, DeposerRessource, AllerPort }
    private EtatPerso etatActuel = EtatPerso.Normal;
    private GameObject cibleRessource;
    private float timerCollecte;

    
    Animator anim;
    private Vector3 positionPrecedente;

    private void Start()
    {   
        anim = GetComponent<Animator>();
        layerBatiments = LayerMask.GetMask("Buildings");
        sacADos.SetProprietaire(this);
        name = NomAleatoire.ObtenirNomUnique();
        transform.position = TrouverSolLePlusProche();
        ChoisirNouvelleCible();
        // Enregistrement automatique au manager pour une meilleure gestion
        MetierAssignmentManager.Instance?.EnregistrerPersonnage(this);
    }

    public void NotifyResourceChanged()
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.NotifyResourceChanged();
        }
    }

    private void Update()
    {
        if (!enRegeneration)
        {
            faim -= Time.deltaTime * 0.1f;
            soif -= Time.deltaTime * 0.15f;
            fatigue -= Time.deltaTime * 0.05f;
            vie -= Time.deltaTime * 0.02f;
        }

        if (vie <= 0 || faim <= 0 || soif <= 0 || fatigue <= 0)
        {
            Debug.Log($"{name} est mort.");
            if (this.metier != JobType.Aucun)
            {
                MetierAssignmentManager.Instance.SupprimerPersonnage(this);
            }
            OnPersonnageMort?.Invoke(this);
            Destroy(gameObject);
            return;
        }

        EvaluerBesoinsUrgents();
        timer -= Time.deltaTime;

        // Déplacement
        DeplacementAvecContournement();

        // Calcul de la vitesse réelle (différence entre la nouvelle position et l’ancienne)
        Vector2 velocity = (transform.position - positionPrecedente) / Time.deltaTime;

        // Animation : éviter les erreurs nulles si anim non initialisé
        if (anim != null)
        {
            anim.SetFloat("inputX", velocity.x);
            anim.SetFloat("inputY", velocity.y);

            if (velocity.magnitude > 0.1f)
            {
                anim.SetFloat("lastinputX", Mathf.Sign(velocity.x));
                anim.SetFloat("lastinputY", Mathf.Sign(velocity.y));
            }
        }

        // Met à jour positionPrecedente pour la prochaine frame
        positionPrecedente = transform.position;

        if (!enRegeneration && metier == JobType.Aucun)
        {
            GérerLogiqueSansMetier();
        }
        else
        {
            GérerLogiqueAllerBatimentDeMetier();
        }
    }



    public void AssignerAuBatiment(BatimentInteractif nouveauBatiment, JobType nouveauMetier)
    {


        batimentAssigné = nouveauBatiment;
        AssignerMetier(nouveauMetier);
        Debug.Log($"{name} est maintenant affecté au bâtiment {nouveauBatiment?.name ?? "NULL"} comme {nouveauMetier}");
    }



    public void AssignerMetier(JobType nouveauMetier)
    {
        metier = nouveauMetier;
        etatActuel = EtatPerso.Normal;
        cibleObjet = null;
        cibleRessource = null;
        timerCollecte = 0f;
        DeplacerVers(transform.position); // Stop déplacement
        Debug.Log($"{name} a reçu le métier {metier}, état réinitialisé");
    }

    void GérerLogiqueAllerBatimentDeMetier()
    {
        switch (etatActuel)
        {
            case EtatPerso.Normal:
                // Si sac plein, chercher un stockage
                if (sacADos.quantite >= sacADos.capaciteMax)
                {
                    GameObject stockage = TrouverPlusProcheParTag("Stockage");
                    if (stockage != null)
                    {
                        cibleObjet = stockage;
                        DeplacerVers(stockage.transform.position);
                        etatActuel = EtatPerso.AllerStockage;
                    }
                }
                else
                {
                    GameObject batiment = TrouverBatimentDeMetier();
                    if (batiment != null)
                    {
                        BatimentInteractif batimentInteractif = batiment.GetComponent<BatimentInteractif>();

                        if (batimentInteractif != null && batimentInteractif.metierAssocie == metier)
                        {
                            // 🔄 Nouvelle vérification ici
                            var prodInfo = batimentInteractif.metierProductions.Find(p => p.metier == metier);

                            if (prodInfo != null)
                            {
                                bool estTransformation = prodInfo.transformation;

                                // Si ce n'est pas une transformation et que le sac contient une ressource différente → on va stocker
                                if (!estTransformation &&
                                    !sacADos.EstVide() &&
                                    sacADos.ressourceActuelle != prodInfo.ressourceProduite)
                                {
                                    GameObject stockage = TrouverPlusProcheParTag("Stockage");
                                    if (stockage != null)
                                    {
                                        cibleObjet = stockage;
                                        DeplacerVers(stockage.transform.position);
                                        etatActuel = EtatPerso.AllerStockage;
                                        Debug.Log($"{name} a une ressource non liée à son bâtiment de production. Il va la stocker.");
                                        return;
                                    }
                                }

                                // Si transformation mais sac vide ou ressource inadaptée → aller chercher la bonne ressource
                                if (estTransformation &&
                                    (sacADos.EstVide() || sacADos.ressourceActuelle != prodInfo.ressourceRequise || sacADos.quantite < prodInfo.quantiteRequise))
                                {
                                    GameObject stockageAvecRessource = TrouverStockageAvecRessource(prodInfo.ressourceRequise, prodInfo.quantiteRequise);
                                    if (stockageAvecRessource != null)
                                    {
                                        cibleObjet = stockageAvecRessource;
                                        DeplacerVers(stockageAvecRessource.transform.position);
                                        etatActuel = EtatPerso.AllerStockage;
                                        Debug.Log($"{name} va chercher {prodInfo.ressourceRequise} dans le stockage avant d'aller au bâtiment de transformation {batimentInteractif.name}");
                                        return;
                                    }
                                    else
                                    {
                                        Debug.Log($"{name} n'a pas trouvé de stockage avec {prodInfo.ressourceRequise} disponible !");
                                    }
                                }
                            }

                            // Aller au bâtiment (cas normal ou transformation avec bonne ressource en sac)
                            cibleObjet = batiment;
                            DeplacerVers(batiment.transform.position);
                        }
                    }
                }
                break;

            case EtatPerso.AllerStockage:
                if (cibleObjet == null)
                {
                    etatActuel = EtatPerso.Normal;
                    return;
                }

                float dist = Vector3.Distance(transform.position, cibleObjet.transform.position);
                if (dist < 0.5f)
                {
                    Debug.Log($"{name} est arrivé à son bâtiment de métier ({metier})");
                    etatActuel = EtatPerso.DeposerRessource;
                }
                break;

            case EtatPerso.DeposerRessource:
                Debug.Log($"{name} a déposé : {sacADos.ressourceActuelle} x{sacADos.quantite}");
                sacADos.Vider();
                etatActuel = EtatPerso.Normal;
                break;
        }
    }



    /// <summary>
    /// 🔥 NOUVELLE MÉTHODE : Gère le déplacement avec contournement automatique des bâtiments
    /// </summary>
    private void DeplacementAvecContournement()
    {
        Vector3 directionPrincipale = (cible - transform.position).normalized;
        Vector3 directionFinale = directionPrincipale;

        // Détection des bâtiments devant le personnage
        GameObject batimentDevant = DetecterBatimentDevant(directionPrincipale);

        if (batimentDevant != null && batimentDevant != cibleObjet)
        {
            // Si on détecte un bâtiment et qu'on n'est pas déjà en contournement
            if (!enContournement)
            {
                directionContournement = CalculerDirectionContournement(batimentDevant, directionPrincipale);
                enContournement = true;
                timerContournement = 2f; // Durée du contournement
            }
        }

        // Si on est en contournement
        if (enContournement)
        {
            timerContournement -= Time.deltaTime;

            // Mélange la direction de contournement avec la direction principale
            float ratioContournement = Mathf.Clamp01(timerContournement / 2f);
            directionFinale = Vector3.Lerp(directionPrincipale, directionContournement, ratioContournement * forceContournement);

            // Arrêter le contournement si on a contourné assez longtemps ou si on n'a plus d'obstacle
            if (timerContournement <= 0f || DetecterBatimentDevant(directionPrincipale) == null)
            {
                enContournement = false;
                timerContournement = 0f;
            }
        }

        // BONUS VITESSE SI SUR CHEMIN
        float bonusVitesse = 1f;
        Collider2D pathCollider = Physics2D.OverlapPoint(transform.position, LayerMask.GetMask("Path"));

        if (pathCollider != null)
        {
            PathTile path = pathCollider.GetComponent<PathTile>();
            if (path != null)
            {
                bonusVitesse = path.GetSpeedMultiplier();
            }
        }

        // Application du bonus
        Vector3 nextPos = transform.position + directionFinale.normalized * (vitesse * bonusVitesse) * Time.deltaTime;


        // Vérification que la prochaine position est sur le sol
        if (Physics2D.OverlapCircle(nextPos, 0.1f, layerSol))
        {
            
            transform.position = nextPos;
            
        }
        else if (timer <= 0f)
        {
            ChoisirNouvelleCible();
        }
    }

    /// <summary>
    /// 🔥 NOUVELLE MÉTHODE : Détecte s'il y a un bâtiment devant le personnage
    /// </summary>
    private GameObject DetecterBatimentDevant(Vector3 direction)
    {
        // Lancer un raycast pour détecter les bâtiments
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, rayonDetection, layerBatiments);

        if (hit.collider != null)
        {
            return hit.collider.gameObject;
        }

        // Également vérifier avec un cercle pour une détection plus large
        Collider2D[] batiments = Physics2D.OverlapCircleAll(transform.position + direction * (rayonDetection * 0.7f), 0.5f, layerBatiments);

        if (batiments.Length > 0)
        {
            return batiments[0].gameObject;
        }

        return null;
    }

    /// <summary>
    /// 🔥 NOUVELLE MÉTHODE : Calcule la direction pour contourner un bâtiment
    /// </summary>
    private Vector3 CalculerDirectionContournement(GameObject batiment, Vector3 directionOriginale)
    {
        Vector3 versBatiment = (batiment.transform.position - transform.position).normalized;
        Vector3 versCible = (cible - transform.position).normalized;

        // Calculer deux directions perpendiculaires
        Vector3 droite = new Vector3(-versBatiment.y, versBatiment.x, 0);
        Vector3 gauche = new Vector3(versBatiment.y, -versBatiment.x, 0);

        // Choisir la direction qui nous rapproche le plus de la cible
        float dotDroite = Vector3.Dot(droite, versCible);
        float dotGauche = Vector3.Dot(gauche, versCible);

        Vector3 directionContournement = (dotDroite > dotGauche) ? droite : gauche;

        // Mélanger avec la direction originale pour un mouvement plus fluide
        return Vector3.Lerp(directionContournement, directionOriginale, 0.3f).normalized;
    }

    bool EvaluerBesoinsUrgents()
    {
        if (faim < 20f)
        {
            GameObject cible = TrouverBatimentPourBesoin(TypeBesoin.Faim);
            if (cible != null)
            {
                cibleObjet = cible;
                DeplacerVers(cible.transform.position);
                return true;
            }
        }

        if (soif < 20f)
        {
            GameObject cible = TrouverBatimentPourBesoin(TypeBesoin.Soif);
            if (cible != null)
            {
                cibleObjet = cible;
                DeplacerVers(cible.transform.position);
                return true;
            }
        }

        if (fatigue < 20f)
        {
            GameObject cible = TrouverBatimentPourBesoin(TypeBesoin.Fatigue);
            if (cible != null)
            {
                cibleObjet = cible;
                DeplacerVers(cible.transform.position);
                return true;
            }
        }

        return false;
    }

    public void DeplacerVers(Vector3 destination)
    {
        float distance = Vector3.Distance(transform.position, destination);

        // 👉 Si la distance est trop grande OU si pas de sol entre ici et la cible
        bool tropLoin = distance > 20f;
        bool cheminBloqué = !CheminPossible(destination);

        if (tropLoin || cheminBloqué)
        {
            GameObject portProche = TrouverPlusProcheParTag("Port");
            if (portProche != null)
            {
                cibleObjet = portProche;
                etatActuel = EtatPerso.AllerPort;
                destination = portProche.transform.position;
                Debug.Log($"{name} change de destination pour aller au port : {portProche.name}");
            }
        }
        else if (cheminBloqué)
        {
            // 👉 Tenter de trouver un point sur le sol en direction de la destination (mais légèrement décalé)
            Vector3 rive = TrouverDirectionRive(destination);

            if (rive != Vector3.zero)
            {
                cible = rive;
                Debug.Log($"{name} longe la rive vers {rive}");
            }
            else
            {
                Debug.LogWarning($"{name} ne trouve pas de rive praticable !");
                cible = transform.position; // reste sur place
            }
        }


        // 1. Chemins
        Collider2D cheminDepart = Physics2D.OverlapCircle(transform.position, 2f, LayerMask.GetMask("Path"));
        Collider2D cheminArrivee = Physics2D.OverlapCircle(destination, 2f, LayerMask.GetMask("Path"));

        if (cheminDepart != null && cheminArrivee != null)
        {
            cible = cheminArrivee.transform.position;
        }
        else
        {
            cible = destination;
        }

        timer = UnityEngine.Random.Range(2f, 4f);
        enContournement = false;
        timerContournement = 0f;
    }

    private bool CheminPossible(Vector3 destination)
    {
        Vector3 direction = (destination - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, destination);

        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, layerSol);

        return hit.collider != null;
    }

    private Vector3 TrouverDirectionRive(Vector3 destination)
    {
        Vector3 direction = (destination - transform.position).normalized;
        float angleStep = 15f;
        float rayon = 1.5f;

        for (float angle = -90f; angle <= 90f; angle += angleStep)
        {
            Vector3 directionTest = Quaternion.Euler(0, 0, angle) * direction;
            Vector3 pointTest = transform.position + directionTest * rayon;

            if (Physics2D.OverlapCircle(pointTest, 0.2f, layerSol))
            {
                return pointTest;
            }
        }

        return Vector3.zero; // rien trouvé
    }



    void ChoisirNouvelleCible()
    {
        int tentatives = 50;

        while (tentatives-- > 0)
        {
            Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
            Vector3 tentative = transform.position + (Vector3)(direction * UnityEngine.Random.Range(1f, 2f));
            tentative.z = 0;

            if (Physics2D.OverlapCircle(tentative, 2f, layerSol))
            {
                cible = tentative;
                timer = UnityEngine.Random.Range(2f, 4f);

                // 🔥 NOUVEAU : Réinitialiser le contournement
                enContournement = false;
                timerContournement = 0f;
                return;
            }
        }

        Debug.LogWarning($"{name} : impossible de trouver une nouvelle position sur le sol.");
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

    GameObject TrouverBatimentPourBesoin(TypeBesoin besoin)
    {
        GameObject plusProche = null;
        float distanceMin = float.MaxValue;

        BatimentInteractif[] batiments = FindObjectsOfType<BatimentInteractif>();

        foreach (BatimentInteractif b in batiments)
        {
            if (!b.estPlace || !b.regenereBesoin || b.typeBesoin != besoin || !b.EstDisponible())
                continue;


            float dist = Vector3.Distance(transform.position, b.transform.position);
            if (dist < distanceMin)
            {
                distanceMin = dist;
                plusProche = b.gameObject;
            }
        }

        return plusProche;
    }

    private void OnMouseDown()
    {
        if (PersonnageTooltipUI.Instance != null)
            PersonnageTooltipUI.Instance.ShowTooltip(this);
    }

    void GérerLogiqueSansMetier()
    {
        switch (etatActuel)
        {
            case EtatPerso.Normal:
                // Si sac plein, chercher un stockage
                if (sacADos.quantite >= sacADos.capaciteMax)
                {
                    GameObject stockage = TrouverPlusProcheParTag("Stockage");
                    if (stockage != null)
                    {
                        cibleObjet = stockage;
                        DeplacerVers(stockage.transform.position);
                        etatActuel = EtatPerso.AllerStockage;
                    }

                }
                else
                {
                    // Si le sac est vide, chercher la ressource la plus proche (arbre ou pierre)
                    if (string.IsNullOrEmpty(sacADos.ressourceActuelle))
                    {
                        cibleRessource = TrouverRessourceDisponible("Arbre", "Pierre");
                    }
                    else if (sacADos.ressourceActuelle == "Bois")
                    {
                        cibleRessource = TrouverRessourceDisponible("Arbre");
                    }
                    else if (sacADos.ressourceActuelle == "Pierre")
                    {
                        cibleRessource = TrouverRessourceDisponible("Pierre");
                    }

                    if (cibleRessource != null)
                    {
                        RessourceOccupationManager.Occuper(cibleRessource);
                        cibleObjet = cibleRessource;
                        DeplacerVers(cibleRessource.transform.position);
                        etatActuel = EtatPerso.Collecte;
                    }

                }
                break;

            case EtatPerso.Collecte:
                if (cibleRessource == null) { etatActuel = EtatPerso.Normal; return; }

                float distance = Vector3.Distance(transform.position, cibleRessource.transform.position);
                if (distance < 0.5f)
                {
                    timerCollecte = 5f;
                    etatActuel = EtatPerso.AttenteCollecte;
                }
                break;

            case EtatPerso.AttenteCollecte:
                timerCollecte -= Time.deltaTime;
                if (timerCollecte <= 0f)
                {
                    string type = cibleRessource.tag == "Arbre" ? "Bois" : "Pierre";
                    if (sacADos.PeutAjouter(type))
                    {
                        sacADos.Ajouter(type, 1);
                        RessourceOccupationManager.Liberer(cibleRessource);

                        if (cibleRessource.tag == "Arbre")
                        {
                            // Appelle GestionDesArbres pour couper et gérer la souche/repousse
                            GameObject treesManager = GameObject.Find("Trees");
                            if (treesManager != null)
                            {
                                treesManager.GetComponent<GestionDesArbres>().CouperArbre(cibleRessource.transform);
                            }
                            else
                            {
                                Debug.LogWarning("GestionDesArbres (Trees) n'a pas été trouvé dans la scène !");
                            }
                        }
                        else if (cibleRessource.tag == "Pierre")
                        {
                            // Appelle GestionDesRochers pour casser et gérer la souche/repousse
                            GameObject rocksManager = GameObject.Find("Rocks");
                            if (rocksManager != null)
                            {
                                rocksManager.GetComponent<GestionDesRochers>().CasserRocher(cibleRessource.transform);
                            }
                            else
                            {
                                Debug.LogWarning("GestionDesRochers (Rocks) n'a pas été trouvé dans la scène !");
                            }
                        }
                        else
                        {
                            // Pour toute autre ressource : comportement par défaut (facultatif)
                            Destroy(cibleRessource);
                        }
                    }
                    else
                    {
                        Debug.Log($"{name} ne peut pas prendre {type} car son sac contient {sacADos.ressourceActuelle}");
                    }

                    cibleRessource = null;
                    etatActuel = EtatPerso.Normal;
                }
                break;

            case EtatPerso.AllerStockage:
                if (cibleObjet == null) { etatActuel = EtatPerso.Normal; return; }

                float dist = Vector3.Distance(transform.position, cibleObjet.transform.position);
                if (dist < 0.5f)
                {
                    etatActuel = EtatPerso.DeposerRessource;
                }
                break;

            case EtatPerso.DeposerRessource:
                Debug.Log($"{name} a déposé : {sacADos.ressourceActuelle} x{sacADos.quantite}");
                sacADos.Vider();
                GameObject port = TrouverPlusProcheParTag("Port");
                if (port != null && UnityEngine.Random.Range(0, 20) == 0)
                {
                    Debug.Log("port");
                    cibleObjet = port;
                    DeplacerVers(port.transform.position);
                    etatActuel = EtatPerso.AllerPort; // 🔥 ou un nouvel état si tu préfères
                }
                else
                {
                    etatActuel = EtatPerso.Normal;
                }
                break;
            
            case EtatPerso.AllerPort:
                if (cibleObjet == null)
                {
                    Debug.Log("bah nan");
                    etatActuel = EtatPerso.Normal;
                    return;
                }

                float distance1 = Vector3.Distance(transform.position, cibleObjet.transform.position);
                if (distance1 < 0.5f)
                {
                    if (cibleObjet.CompareTag("Port"))
                    {
                        Debug.Log($"{name} est arrivé au port");
                        // Déclenchement d'une animation ou logique ici ?
                        etatActuel = EtatPerso.Normal;
                    }
                    else
                    {
                        etatActuel = EtatPerso.DeposerRessource;
                    }
                }
                break;


        }
    }

    GameObject TrouverRessourceDisponible(params string[] tags)
    {
        List<GameObject> candidates = new List<GameObject>();

        foreach (string tag in tags)
        {
            candidates.AddRange(GameObject.FindGameObjectsWithTag(tag));
        }

        return candidates
            .Where(obj => !RessourceOccupationManager.EstOccupe(obj))
            .OrderBy(obj => Vector3.Distance(transform.position, obj.transform.position))
            .FirstOrDefault();
    }

    public GameObject TrouverPlusProcheParTag(params string[] tags)
    {
        GameObject plusProche = null;
        float distanceMin = float.MaxValue;

        foreach (string tag in tags)
        {
            GameObject[] objets = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in objets)
            {
                BatimentInteractif batiment = obj.GetComponent<BatimentInteractif>();
                if (batiment != null && !batiment.estPlace) continue;
                if (obj == batiment.gameObject)
                {
                    if (batiment.estUnStockage)
                    {
                        string ressourceDuSac = sacADos.ressourceActuelle;
                        if (!string.IsNullOrEmpty(ressourceDuSac))
                        {
                            int quantiteDansStock = batiment.ObtenirQuantite(ressourceDuSac);
                            int espaceLibre = batiment.GetEspaceLibre(ressourceDuSac);

                            if (batiment.stock.Count < batiment.maxTypes)
                            {
                                if (quantiteDansStock != 0 && espaceLibre > 0)
                                {
                                    float dist = Vector3.Distance(transform.position, obj.transform.position);
                                    if (dist < distanceMin)
                                    {
                                        distanceMin = dist;
                                        plusProche = obj;
                                    }
                                }
                                if (quantiteDansStock == 0)
                                {
                                    float dist = Vector3.Distance(transform.position, obj.transform.position);
                                    if (dist < distanceMin)
                                    {
                                        distanceMin = dist;
                                        plusProche = obj;
                                    }
                                }
                            }
                            else if (batiment.stock.Count == batiment.maxTypes && quantiteDansStock != 0 && espaceLibre > 0)
                            {
                                float dist = Vector3.Distance(transform.position, obj.transform.position);
                                if (dist < distanceMin)
                                {
                                    distanceMin = dist;
                                    plusProche = obj;
                                }
                            }
                        }
                    }
                    else{
                        float dist = Vector3.Distance(transform.position, obj.transform.position);
                        if (dist < distanceMin)
                        {
                            distanceMin = dist;
                            plusProche = obj;
                        }
                        }
                }
                else
                {
                    float dist = Vector3.Distance(transform.position, obj.transform.position);
                    if (dist < distanceMin)
                    {
                        distanceMin = dist;
                        plusProche = obj;
                    }
                }
            }
        }

        return plusProche;
    }

    GameObject TrouverBatimentDeMetier()
    {
        BatimentInteractif[] tous = FindObjectsOfType<BatimentInteractif>();
        GameObject plusProche = null;
        float distanceMin = float.MaxValue;

        foreach (BatimentInteractif b in tous)
        {
            if (!b.estPlace || b.metierAssocie != metier) continue;


            float dist = Vector3.Distance(transform.position, b.transform.position);
            if (dist < distanceMin)
            {
                distanceMin = dist;
                plusProche = b.gameObject;
            }
        }

        return plusProche;
    }

    /// <summary>
    /// Cherche le stockage qui contient au moins la quantité demandée de la ressource spécifiée
    /// </summary>
    GameObject TrouverStockageAvecRessource(string ressource, int quantiteMin = 1)
    {
        GameObject[] stockages = GameObject.FindGameObjectsWithTag("Stockage");
        foreach (GameObject go in stockages)
        {
            BatimentInteractif stockage = go.GetComponent<BatimentInteractif>();
            if (stockage != null && stockage.ObtenirQuantite(ressource) >= quantiteMin)
            {
                return go;
            }
        }
        return null;
    }

    public void TerminerRegeneration()
    {
        enRegeneration = false;
        cibleObjet = null;
        etatActuel = EtatPerso.Normal;
        ChoisirNouvelleCible();
    }
    
    public void QuitterBatiment()
    {
        cibleObjet = null;
        cibleRessource = null;
        enRegeneration = false;

        etatActuel = EtatPerso.Normal;

        if (!EvaluerBesoinsUrgents())
        {
            Debug.Log("bonla cest ok");
            ChoisirNouvelleCible();
            
        }
    }




}