using UnityEngine;
using TMPro; // Utilisation de TextMeshPro pour les textes UI

// Ce script permet d'afficher une fiche d'information (tooltip) pour un personnage quand on clique dessus
public class PersonnageTooltipUI : MonoBehaviour
{
    public static PersonnageTooltipUI Instance; // Singleton pour pouvoir accéder facilement à cette interface depuis d'autres scripts

    [Header("Références UI")]
    public GameObject panel;              // Le panneau qui contient toute l’UI
    public TMP_Text nomText;             // Texte du nom du personnage
    public TMP_Text vieText;             // Texte de la vie
    public TMP_Text faimText;            // Texte de la faim
    public TMP_Text soifText;            // Texte de la soif
    public TMP_Text fatigueText;         // Texte de la fatigue
    public TMP_Text metierText;          // Texte du métier
    public TMP_Text sacText;             // Texte du contenu du sac
    public TMP_Text outilText;           // Texte pour savoir si le perso a un outil

    private PersonnageData personnageActuel; // Référence vers le personnage actuellement affiché

    private void Awake()
    {
        Instance = this; // Initialise le singleton
        HideTooltip();   // Cache l’UI au démarrage
    }

    // Appelée quand on veut afficher les infos d’un personnage
    public void ShowTooltip(PersonnageData data)
    {
        personnageActuel = data;     // On garde une référence du personnage sélectionné
        panel.SetActive(true);       // On affiche le panneau UI
        MettreAJourAffichage();      // On affiche immédiatement les données
    }

    // Appelée pour cacher la fiche personnage
    public void HideTooltip()
    {
        panel.SetActive(false);      // Cache le panneau UI
        personnageActuel = null;     // On n'a plus de personnage sélectionné
    }

    private void Update()
    {
        // Si l’UI est active et qu’on clique avec la souris
        if (panel.activeSelf && Input.GetMouseButtonDown(0))
        {
            // On fait un raycast depuis la souris pour voir si on clique sur un personnage
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray);

            // Si on clique ailleurs que sur un personnage, on ferme la fiche
            if (hit.collider == null || hit.collider.GetComponent<PersonnageData>() == null)
            {
                HideTooltip();
            }
        }

        // Tant que l’UI est ouverte et qu’un personnage est sélectionné, on actualise les infos à l’écran
        if (panel.activeSelf && personnageActuel != null)
        {
            MettreAJourAffichage();
        }
    }

    // Met à jour toutes les infos à afficher dans la fiche personnage
    private void MettreAJourAffichage()
    {
        nomText.text = personnageActuel.name; // Affiche le nom du personnage
        vieText.text = $"Vie : {Mathf.RoundToInt(personnageActuel.vie)}"; // Affiche la vie arrondie
        faimText.text = $"Faim : {Mathf.RoundToInt(personnageActuel.faim)}"; // Idem pour la faim
        soifText.text = $"Soif : {Mathf.RoundToInt(personnageActuel.soif)}"; // Idem pour la soif
        fatigueText.text = $"Fatigue : {Mathf.RoundToInt(personnageActuel.fatigue)}"; // Idem pour la fatigue
        metierText.text = $"Métier : {personnageActuel.metier}"; // Affiche le métier (enum affiché en texte)

        // Si le sac contient quelque chose, on affiche la ressource + la quantité
        if (personnageActuel.sacADos.ressourceActuelle != null)
            sacText.text = $"Sac : {personnageActuel.sacADos.ressourceActuelle} x{personnageActuel.sacADos.quantite}";
        else
            sacText.text = "Sac : Vide"; // Sinon, on affiche que le sac est vide

        // Indique si le personnage a un outil ou non
        outilText.text = personnageActuel.aOutil ? "Outil : Oui" : "Outil : Non";
    }
}
