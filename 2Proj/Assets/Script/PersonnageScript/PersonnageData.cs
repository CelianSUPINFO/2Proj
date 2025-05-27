using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public enum JobType
{
    Aucun,
    Bucheron,
    Fermier,
    Chercheur,
    Boulanger,
    Constructeur,
    Transporteur
}



[System.Serializable]
public class Backpack
{
    public string ressourceActuelle = null;
    public int quantite = 0;
    public int capaciteMax = 5;

    // Référence vers le personnage propriétaire pour les notifications
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

        // 🔥 NOUVEAU : Notifier le ResourceManager
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

    /// <summary>
    /// 🔥 NOUVELLE MÉTHODE : Obtenir l'espace libre dans le sac
    /// </summary>
    public int GetEspaceLibre()
    {
        return capaciteMax - quantite;
    }

    /// <summary>
    /// 🔥 NOUVELLE MÉTHODE : Vérifier si le sac est plein
    /// </summary>
    public bool EstPlein()
    {
        return quantite >= capaciteMax;
    }

    /// <summary>
    /// 🔥 NOUVELLE MÉTHODE : Vérifier si le sac est vide
    /// </summary>
    public bool EstVide()
    {
        return quantite <= 0 || string.IsNullOrEmpty(ressourceActuelle);
    }
}

public class PersonnageData : MonoBehaviour
{
    [HideInInspector]
    public bool enRegeneration = false;

    [Header("Stats")]
    public float vie = 100f;
    public float faim = 100f;
    public float soif = 100f;
    public float fatigue = 100f;

    [Header("Métier")]
    public JobType metier = JobType.Aucun;

    [Header("Sac à dos")]
    public Backpack sacADos = new Backpack();

    [Header("Déplacement")]
    public float vitesse = 1.5f;
    public LayerMask layerSol;

    private Vector3 cible;
    private float timer;
    public GameObject cibleObjet;

    private enum EtatPerso
    {
        Normal,
        Collecte,
        AttenteCollecte,
        AllerStockage,
        DeposerRessource
    }

    private EtatPerso etatActuel = EtatPerso.Normal;
    private GameObject cibleRessource;
    private float timerCollecte;


    // À ajouter dans la méthode Start() de PersonnageData :

    private void Start()
    {
        // 🔥 NOUVEAU : Initialiser le propriétaire du sac à dos
        sacADos.SetProprietaire(this);
        
        name = NomAleatoire.ObtenirNomUnique();
        transform.position = TrouverSolLePlusProche();
        ChoisirNouvelleCible();
    }

    // 🔥 NOUVELLE MÉTHODE : À ajouter dans PersonnageData pour la cohérence
    /// <summary>
    /// Méthode utilitaire pour notifier le ResourceManager quand ce personnage change
    /// </summary>
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
            vie -=  Time.deltaTime * 0.02f;
        }

        if (vie <= 0 || faim <= 0 || soif <= 0 || fatigue <= 0)
        {
            Debug.Log($"{name} est mort.");
            Destroy(gameObject);
            return;
        }

        EvaluerBesoinsUrgents();

        timer -= Time.deltaTime;
        Vector3 direction = (cible - transform.position).normalized;
        Vector3 nextPos = transform.position + direction * vitesse * Time.deltaTime;

        if (Physics2D.OverlapCircle(nextPos, 0.1f, layerSol))
        {
            transform.position = nextPos;
        }
        else if (timer <= 0f)
        {
            ChoisirNouvelleCible();
        }
        // Comportement pour les personnages sans métier
        if (metier == JobType.Aucun && !enRegeneration)
        {
            GérerLogiqueSansMetier();
        }

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

    void DeplacerVers(Vector3 destination)
    {
        cible = destination;
        timer = Random.Range(2f, 4f);
    }

    void ChoisirNouvelleCible()
    {
        int tentatives = 50;

        while (tentatives-- > 0)
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            Vector3 tentative = transform.position + (Vector3)(direction * Random.Range(1f, 2f));
            tentative.z = 0;

            if (Physics2D.OverlapCircle(tentative, 0.1f, layerSol))
            {
                cible = tentative;
                timer = Random.Range(2f, 4f);
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
            Vector2 direction = Random.insideUnitCircle.normalized;
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
            if (!b.regenereBesoin || b.typeBesoin != besoin || !b.EstDisponible())
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
                        Destroy(cibleRessource);
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
                etatActuel = EtatPerso.Normal;
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
    GameObject TrouverPlusProcheParTag(params string[] tags)
    {
        GameObject plusProche = null;
        float distanceMin = float.MaxValue;

        foreach (string tag in tags)
        {
            GameObject[] objets = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in objets)
            {
                float dist = Vector3.Distance(transform.position, obj.transform.position);
                if (dist < distanceMin)
                {
                    distanceMin = dist;
                    plusProche = obj;
                }
            }
        }

        return plusProche;
    }

}
