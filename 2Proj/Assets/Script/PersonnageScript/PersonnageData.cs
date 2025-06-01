// Fichier completement commenté pour expliquer le comportement du personnage dans un jeu de gestion/simulation

using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;

// Liste des métiers disponibles pour les personnages
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

// Classe représentant le sac à dos d’un personnage, qui contient des ressources
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

    // Vérifie si on peut ajouter une ressource au sac (soit vide soit déjà cette ressource)
    public bool PeutAjouter(string ressource)
    {
        bool vide = string.IsNullOrEmpty(ressourceActuelle);
        bool peut = vide || ressourceActuelle == ressource;

        if (!peut)
            Debug.LogWarning($"[BACKPACK] Incompatible: a déjà {ressourceActuelle ?? "rien"}, tenté {ressource}");

        return peut;
    }

    // Ajoute une certaine quantité d'une ressource
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

        if (quantiteAvant != quantite)
        {
            Debug.Log($"[Sac {proprietaire?.name ?? "Unknown"}] {ressource}: {quantiteAvant} → {quantite}");
            ResourceManager.Instance?.NotifyResourceChanged(); // Notifie qu'une ressource a changé
        }

        return true;
    }

    // Vide complètement le sac
    public void Vider()
    {
        bool hadResources = quantite > 0;
        string oldResource = ressourceActuelle;

        ressourceActuelle = null;
        quantite = 0;

        if (hadResources)
        {
            Debug.Log($"[Sac {proprietaire?.name ?? "Unknown"}] Vidé (contenait {oldResource})");
            ResourceManager.Instance?.NotifyResourceChanged();
        }
    }

    // Retire une certaine quantité
    public int Retirer(int quantiteVoulue)
    {
        if (quantite <= 0 || quantiteVoulue <= 0) return 0;

        int quantiteAvant = quantite;
        int aRetirer = Mathf.Min(quantiteVoulue, quantite);
        quantite -= aRetirer;

        if (quantite <= 0)
        {
            Vider(); // On vide complètement
        }
        else
        {
            Debug.Log($"[Sac {proprietaire?.name ?? "Unknown"}] {ressourceActuelle}: {quantiteAvant} → {quantite}");
            ResourceManager.Instance?.NotifyResourceChanged();
        }

        return aRetirer;
    }

    public int GetEspaceLibre() => capaciteMax - quantite; // Retourne l’espace libre
    public bool EstPlein() => quantite >= capaciteMax;
    public bool EstVide() => quantite <= 0 || string.IsNullOrEmpty(ressourceActuelle);
}

// Comportement global du personnage
public class PersonnageData : MonoBehaviour
{
    public BatimentInteractif batimentAssigné; // Bâtiment associé
    public bool aOutil = false;
    [HideInInspector] public bool enRegeneration = false; // Si le perso est en pause

    // Statistiques de survie du personnage
    public float vie = 100f, faim = 100f, soif = 100f, fatigue = 100f;
    public JobType metier = JobType.Aucun; // Métier assigné
    public Backpack sacADos = new Backpack(); // Sac à dos du personnage

    // Déplacement
    public float vitesse = 1.5f;
    public LayerMask layerSol;
    public LayerMask layerBatiments;
    public float rayonDetection = 1.2f;
    public float forceContournement = 2f; // Force avec laquelle le personnage va éviter les obstacles

    private Vector3 cible; // Position actuelle visée
    private float timer;
    public GameObject cibleObjet; // Objet visé (ressource, bâtiment...)
    public static event Action<PersonnageData> OnPersonnageMort; // Événement lors de la mort du personnage

    private Vector3 directionContournement = Vector3.zero; // Direction de contournement actuelle
    private float timerContournement = 0f;
    private bool enContournement = false;

    // États possibles du personnage
    private enum EtatPerso { Normal, Collecte, AttenteCollecte, AllerStockage, DeposerRessource, AllerPort }
    private EtatPerso etatActuel = EtatPerso.Normal;

    private GameObject cibleRessource; // Ressource actuelle ciblée
    private float timerCollecte; // Timer d’attente pendant la collecte

    Animator anim; // Référence vers l’Animator
    private Vector3 positionPrecedente; // Position précédente pour calculer la vitesse

    private void Start()
    {
        anim = GetComponent<Animator>();
        layerBatiments = LayerMask.GetMask("Buildings"); // On force le layer à "Buildings"
        sacADos.SetProprietaire(this); // Donne au sac la référence vers son propriétaire
        name = NomAleatoire.ObtenirNomUnique(); // Donne un nom unique au personnage
        transform.position = TrouverSolLePlusProche(); // Place le perso sur un sol valide
        ChoisirNouvelleCible(); // Donne une cible aléatoire pour commencer
        MetierAssignmentManager.Instance?.EnregistrerPersonnage(this); // Ajoute au gestionnaire de métiers
    }

    // Appelé pour notifier une mise à jour de ressource
    public void NotifyResourceChanged()
    {
        ResourceManager.Instance?.NotifyResourceChanged();
    }

    private void Update()
    {
        // Si le perso n’est pas en pause (ex : régénération), il perd petit à petit ses besoins
        if (!enRegeneration)
        {
            faim -= Time.deltaTime * 0.1f;
            soif -= Time.deltaTime * 0.15f;
            fatigue -= Time.deltaTime * 0.05f;
            vie -= Time.deltaTime * 0.02f;
        }

        // Si un besoin vital atteint 0, le perso meurt
        if (vie <= 0 || faim <= 0 || soif <= 0 || fatigue <= 0)
        {
            Debug.Log($"{name} est mort.");
            if (this.metier != JobType.Aucun)
            {
                MetierAssignmentManager.Instance.SupprimerPersonnage(this); // Supprime du gestionnaire
            }
            OnPersonnageMort?.Invoke(this); // Déclenche l’événement de mort
            Destroy(gameObject); // Supprime le GameObject
            return;
        }

        EvaluerBesoinsUrgents(); // Cherche à satisfaire les besoins en priorité
        timer -= Time.deltaTime; // Diminue le timer de déplacement

        DeplacementAvecContournement(); // Déplacement intelligent avec évitement

        // Calcule la vitesse de déplacement pour l’animation
        Vector2 velocity = (transform.position - positionPrecedente) / Time.deltaTime;
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
        positionPrecedente = transform.position; // Met à jour pour la prochaine frame

        // Logique différente selon qu’il a un métier ou non
        if (!enRegeneration && metier == JobType.Aucun)
        {
            GérerLogiqueSansMetier(); // Va collecter des ressources
        }
        else
        {
            GérerLogiqueAllerBatimentDeMetier(); // Va au bâtiment de son métier
        }
    }


    // Assigne un bâtiment et un métier au personnage
    public void AssignerAuBatiment(BatimentInteractif nouveauBatiment, JobType nouveauMetier)
    {
        batimentAssigné = nouveauBatiment; // On mémorise le bâtiment
        AssignerMetier(nouveauMetier); // On attribue le métier au personnage
        Debug.Log($"{name} est maintenant affecté au bâtiment {nouveauBatiment?.name ?? "NULL"} comme {nouveauMetier}");
    }

    // Réinitialise l'état du personnage avec son nouveau métier
    public void AssignerMetier(JobType nouveauMetier)
    {
        metier = nouveauMetier; // On définit le métier
        etatActuel = EtatPerso.Normal; // On remet à l'état initial
        cibleObjet = null; // Réinitialise la cible
        cibleRessource = null;
        timerCollecte = 0f;
        DeplacerVers(transform.position); // Arrête le déplacement
        Debug.Log($"{name} a reçu le métier {metier}, état réinitialisé");
    }

    // Gère le comportement d’un personnage qui a un métier
    void GérerLogiqueAllerBatimentDeMetier()
    {
        switch (etatActuel)
        {
            case EtatPerso.Normal:
                // Si le sac est plein → aller vers un bâtiment de stockage
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
                    // Cherche le bâtiment lié au métier
                    GameObject batiment = TrouverBatimentDeMetier();
                    if (batiment != null)
                    {
                        BatimentInteractif batimentInteractif = batiment.GetComponent<BatimentInteractif>();

                        if (batimentInteractif != null && batimentInteractif.metierAssocie == metier)
                        {
                            // Récupère les infos de production liées à ce métier
                            var prodInfo = batimentInteractif.metierProductions.Find(p => p.metier == metier);

                            if (prodInfo != null)
                            {
                                bool estTransformation = prodInfo.transformation;

                                // Si ce n'est pas une transformation et que le sac contient une autre ressource → stocker
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

                                // Si c'est une transformation mais qu'il manque la bonne ressource → aller en chercher
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

                            // Sinon, aller directement au bâtiment
                            cibleObjet = batiment;
                            DeplacerVers(batiment.transform.position);
                        }
                    }
                }
                break;

            case EtatPerso.AllerStockage:
                // Quand le personnage atteint le stockage
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
                // Dépose tout le contenu du sac
                Debug.Log($"{name} a déposé : {sacADos.ressourceActuelle} x{sacADos.quantite}");
                sacADos.Vider();
                etatActuel = EtatPerso.Normal;
                break;
        }
    }

    // Gestion du déplacement avec contournement d'obstacles (ex : bâtiments)
    private void DeplacementAvecContournement()
    {
        Vector3 directionPrincipale = (cible - transform.position).normalized;
        Vector3 directionFinale = directionPrincipale;

        // Vérifie s’il y a un bâtiment en face
        GameObject batimentDevant = DetecterBatimentDevant(directionPrincipale);

        if (batimentDevant != null && batimentDevant != cibleObjet)
        {
            // Active le contournement s’il n’est pas déjà actif
            if (!enContournement)
            {
                directionContournement = CalculerDirectionContournement(batimentDevant, directionPrincipale);
                enContournement = true;
                timerContournement = 2f;
            }
        }

        // Si on est en phase de contournement
        if (enContournement)
        {
            timerContournement -= Time.deltaTime;

            // Mélange la direction principale avec celle de contournement
            float ratioContournement = Mathf.Clamp01(timerContournement / 2f);
            directionFinale = Vector3.Lerp(directionPrincipale, directionContournement, ratioContournement * forceContournement);

            // Stoppe le contournement si l’obstacle a été évité
            if (timerContournement <= 0f || DetecterBatimentDevant(directionPrincipale) == null)
            {
                enContournement = false;
                timerContournement = 0f;
            }
        }

        // Vérifie s’il y a un bonus de vitesse grâce à un chemin (path)
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

        // Calcul de la prochaine position du personnage
        Vector3 nextPos = transform.position + directionFinale.normalized * (vitesse * bonusVitesse) * Time.deltaTime;

        // Déplace le perso uniquement si la prochaine position est sur un sol valide
        if (Physics2D.OverlapCircle(nextPos, 0.1f, layerSol))
        {
            transform.position = nextPos;
        }
        else if (timer <= 0f)
        {
            // Si bloqué, on redéfinit une nouvelle cible aléatoire
            ChoisirNouvelleCible();
        }
    }


    /// <summary>
    /// Détecte s'il y a un bâtiment devant le personnage dans une certaine direction
    /// </summary>
    private GameObject DetecterBatimentDevant(Vector3 direction)
    {
        // On lance un raycast dans la direction donnée, limité par une distance (rayonDetection)
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, rayonDetection, layerBatiments);

        // Si un bâtiment est détecté, on le retourne
        if (hit.collider != null)
        {
            return hit.collider.gameObject;
        }

        // Si le raycast n’a rien détecté, on fait une détection avec un cercle pour une zone plus large
        Collider2D[] batiments = Physics2D.OverlapCircleAll(transform.position + direction * (rayonDetection * 0.7f), 0.5f, layerBatiments);

        // S’il y a au moins un bâtiment détecté avec le cercle, on retourne le premier
        if (batiments.Length > 0)
        {
            return batiments[0].gameObject;
        }

        // Aucun bâtiment détecté
        return null;
    }

    /// <summary>
    /// Calcule une direction alternative pour contourner un bâtiment détecté
    /// </summary>
    private Vector3 CalculerDirectionContournement(GameObject batiment, Vector3 directionOriginale)
    {
        // Direction vers le bâtiment
        Vector3 versBatiment = (batiment.transform.position - transform.position).normalized;

        // Direction vers la cible finale
        Vector3 versCible = (cible - transform.position).normalized;

        // On génère deux vecteurs perpendiculaires à la direction vers le bâtiment
        Vector3 droite = new Vector3(-versBatiment.y, versBatiment.x, 0);
        Vector3 gauche = new Vector3(versBatiment.y, -versBatiment.x, 0);

        // On mesure lequel des deux vecteurs nous rapproche le plus de la cible
        float dotDroite = Vector3.Dot(droite, versCible);
        float dotGauche = Vector3.Dot(gauche, versCible);

        // On choisit celui qui est le plus "aligné" avec la cible
        Vector3 directionContournement = (dotDroite > dotGauche) ? droite : gauche;

        // On mélange la direction choisie avec la direction de base pour éviter un virage trop brusque
        return Vector3.Lerp(directionContournement, directionOriginale, 0.3f).normalized;
    }

    /// <summary>
    /// Vérifie si un besoin vital est critique et tente de le satisfaire
    /// </summary>
    bool EvaluerBesoinsUrgents()
    {
        // Si la faim est critique, cherche un bâtiment qui permet de manger
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

        // Si la soif est critique, cherche un bâtiment pour boire
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

        // Si la fatigue est critique, cherche un bâtiment pour se reposer
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

        // Aucun besoin urgent détecté
        return false;
    }

    /// <summary>
    /// Donne une nouvelle destination au personnage, avec vérification du chemin
    /// </summary>
    public void DeplacerVers(Vector3 destination)
    {
        float distance = Vector3.Distance(transform.position, destination);

        // Si la destination est trop loin ou le chemin bloqué, on tente de passer par un port
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
            // Si bloqué mais pas trop loin, on tente de suivre la rive (bord de carte)
            Vector3 rive = TrouverDirectionRive(destination);

            if (rive != Vector3.zero)
            {
                cible = rive;
                Debug.Log($"{name} longe la rive vers {rive}");
            }
            else
            {
                Debug.LogWarning($"{name} ne trouve pas de rive praticable !");
                cible = transform.position;
            }
        }

        // Si les deux points sont proches d’un chemin, on ajuste la cible pour suivre les chemins
        Collider2D cheminDepart = Physics2D.OverlapCircle(transform.position, 2f, LayerMask.GetMask("Path"));
        Collider2D cheminArrivee = Physics2D.OverlapCircle(destination, 2f, LayerMask.GetMask("Path"));

        if (cheminDepart != null && cheminArrivee != null)
        {
            // Ajuste la cible à l’arrivée du chemin
            cible = cheminArrivee.transform.position;
        }
        else
        {
            // Sinon on garde la cible originale
            cible = destination;
        }

        // Initialise le timer de déplacement et réinitialise le contournement
        timer = UnityEngine.Random.Range(2f, 4f);
        enContournement = false;
        timerContournement = 0f;
    }

    /// <summary>
    /// Vérifie si un chemin est libre entre la position actuelle et la destination
    /// </summary>
    private bool CheminPossible(Vector3 destination)
    {
        Vector3 direction = (destination - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, destination);

        // On lance un raycast vers la destination en ne touchant que le sol
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, layerSol);

        return hit.collider != null;
    }

    /// <summary>
    /// Si on est bloqué, tente de trouver un point sur le sol en longeant un angle depuis la direction initiale
    /// </summary>
    private Vector3 TrouverDirectionRive(Vector3 destination)
    {
        Vector3 direction = (destination - transform.position).normalized;
        float angleStep = 15f; // pas de rotation à tester (en degrés)
        float rayon = 1.5f; // distance à tester autour du personnage

        // On teste dans un éventail d'angles autour de la direction initiale
        for (float angle = -90f; angle <= 90f; angle += angleStep)
        {
            Vector3 directionTest = Quaternion.Euler(0, 0, angle) * direction;
            Vector3 pointTest = transform.position + directionTest * rayon;

            // Si le point est sur un sol valide, on le retourne
            if (Physics2D.OverlapCircle(pointTest, 0.2f, layerSol))
            {
                return pointTest;
            }
        }

        // Aucun point praticable trouvé
        return Vector3.zero;
    }


    // Récupère une nouvelle position sur le sol de manière aléatoire
    void ChoisirNouvelleCible()
    {
        int tentatives = 50; // Limite le nombre d'essais pour éviter une boucle infinie

        while (tentatives-- > 0)
        {
            // Génère une direction aléatoire autour du personnage
            Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
            Vector3 tentative = transform.position + (Vector3)(direction * UnityEngine.Random.Range(1f, 2f));
            tentative.z = 0; // Assure qu'on reste en 2D

            // Vérifie si la position générée est sur le sol
            if (Physics2D.OverlapCircle(tentative, 2f, layerSol))
            {
                cible = tentative;
                timer = UnityEngine.Random.Range(2f, 4f);

                // Réinitialisation du contournement d'obstacles
                enContournement = false;
                timerContournement = 0f;
                return;
            }
        }

        Debug.LogWarning($"{name} : impossible de trouver une nouvelle position sur le sol.");
    }

    // Recherche une position proche sur le sol si celle actuelle n'est pas valide
    Vector3 TrouverSolLePlusProche()
    {
        float rayon = 0.5f;
        int essais = 100;

        for (int i = 0; i < essais; i++)
        {
            // Tourne en cercle autour de la position actuelle
            Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
            Vector3 testPos = transform.position + (Vector3)(direction * rayon);

            if (Physics2D.OverlapCircle(testPos, 0.1f, layerSol))
                return testPos;

            rayon += 0.1f;
        }

        return transform.position; // Si aucun sol trouvé, reste en place
    }

    // Trouve le bâtiment actif le plus proche qui peut répondre à un besoin (faim, soif, fatigue)
    GameObject TrouverBatimentPourBesoin(TypeBesoin besoin)
    {
        GameObject plusProche = null;
        float distanceMin = float.MaxValue;

        BatimentInteractif[] batiments = FindObjectsOfType<BatimentInteractif>();

        foreach (BatimentInteractif b in batiments)
        {
            // On filtre uniquement ceux qui sont placés, qui régénèrent des besoins, qui correspondent au besoin actuel, et qui sont dispo
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

    // Affiche un tooltip au clic sur le personnage
    private void OnMouseDown()
    {
        if (PersonnageTooltipUI.Instance != null)
            PersonnageTooltipUI.Instance.ShowTooltip(this);
    }

    // Gère la logique d’un personnage sans métier attribué (récolte libre)
    void GérerLogiqueSansMetier()
    {
        switch (etatActuel)
        {
            case EtatPerso.Normal:
                // Si le sac est plein, chercher un bâtiment de stockage
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
                    // Choisir une ressource selon ce qu'on transporte
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
                if (cibleRessource == null)
                {
                    etatActuel = EtatPerso.Normal;
                    return;
                }

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
                if (cibleObjet == null)
                {
                    etatActuel = EtatPerso.Normal;
                    return;
                }

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
                    cibleObjet = port;
                    DeplacerVers(port.transform.position);
                    etatActuel = EtatPerso.AllerPort;
                }
                else
                {
                    etatActuel = EtatPerso.Normal;
                }
                break;

            case EtatPerso.AllerPort:
                if (cibleObjet == null)
                {
                    etatActuel = EtatPerso.Normal;
                    return;
                }

                float distance1 = Vector3.Distance(transform.position, cibleObjet.transform.position);
                if (distance1 < 0.5f)
                {
                    if (cibleObjet.CompareTag("Port"))
                    {
                        Debug.Log($"{name} est arrivé au port");
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

    // Trouve une ressource non occupée parmi plusieurs types (ex: Arbre, Pierre)
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

    // Recherche un objet le plus proche par tag, avec logique spéciale si c'est un stockage
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

    // Trouve le bâtiment correspondant au métier du personnage
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

    // Cherche un stockage contenant au moins une certaine quantité d'une ressource
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

    // Le personnage sort d'un bâtiment après récupération d'un besoin
    public void TerminerRegeneration()
    {
        enRegeneration = false;
        cibleObjet = null;
        etatActuel = EtatPerso.Normal;
        ChoisirNouvelleCible();
    }

    // Le personnage quitte un bâtiment, potentiellement pour aller vers une autre cible
    public void QuitterBatiment()
    {
        cibleObjet = null;
        cibleRessource = null;
        enRegeneration = false;
        etatActuel = EtatPerso.Normal;

        if (!EvaluerBesoinsUrgents())
        {
            ChoisirNouvelleCible();
        }
    }
}