using UnityEngine;
using TMPro;

public class PersonnageTooltipUI : MonoBehaviour
{
    public static PersonnageTooltipUI Instance;

    [Header("Références UI")]
    public GameObject panel;
    public TMP_Text nomText;
    public TMP_Text vieText;
    public TMP_Text faimText;
    public TMP_Text soifText;
    public TMP_Text fatigueText;
    public TMP_Text metierText;
    public TMP_Text sacText;
    public TMP_Text outilText; 

    private PersonnageData personnageActuel;

    private void Awake()
    {
        Instance = this;
        HideTooltip();
    }

    public void ShowTooltip(PersonnageData data)
    {
        personnageActuel = data;
        panel.SetActive(true);
        MettreAJourAffichage(); // Affiche tout de suite à l'ouverture
    }

    public void HideTooltip()
    {
        panel.SetActive(false);
        personnageActuel = null;
    }

    private void Update()
    {
        // Clic hors personnage = fermer
        if (panel.activeSelf && Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray);
            if (hit.collider == null || hit.collider.GetComponent<PersonnageData>() == null)
            {
                HideTooltip();
            }
        }

        // Mise à jour en live
        if (panel.activeSelf && personnageActuel != null)
        {
            MettreAJourAffichage();
        }
    }

    private void MettreAJourAffichage()
    {
        nomText.text = personnageActuel.name;
        vieText.text = $"Vie : {Mathf.RoundToInt(personnageActuel.vie)}";
        faimText.text = $"Faim : {Mathf.RoundToInt(personnageActuel.faim)}";
        soifText.text = $"Soif : {Mathf.RoundToInt(personnageActuel.soif)}";
        fatigueText.text = $"Fatigue : {Mathf.RoundToInt(personnageActuel.fatigue)}";
        metierText.text = $"Métier : {personnageActuel.metier}";

        if (personnageActuel.sacADos.ressourceActuelle != null)
            sacText.text = $"Sac : {personnageActuel.sacADos.ressourceActuelle} x{personnageActuel.sacADos.quantite}";
        else
            sacText.text = "Sac : Vide";

        
         outilText.text = personnageActuel.aOutil ? "Outil : Oui" : "Outil : Non";
    }
}
