using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MetierAssignmentManager : MonoBehaviour
{
    public static MetierAssignmentManager Instance;

    private List<PersonnageData> tousLesPersonnages = new List<PersonnageData>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Appelé par chaque personnage à sa création pour s’enregistrer
    /// </summary>
    public void EnregistrerPersonnage(PersonnageData perso)
    {
        if (!tousLesPersonnages.Contains(perso))
        {
            tousLesPersonnages.Add(perso);
        }
    }

    /// <summary>
    /// Appelé quand un personnage meurt
    /// </summary>
    public void SupprimerPersonnage(PersonnageData perso)
    {
        tousLesPersonnages.Remove(perso);

        // Préviens tous les bâtiments métier de la disparition de ce travailleur
        BatimentInteractif[] batiments = GameObject.FindObjectsOfType<BatimentInteractif>();
        foreach (var bat in batiments)
        {
            bat.GererMortTravailleur(perso);
        }
    }

    /// <summary>
    /// Retourne un personnage sans métier (ou null si aucun disponible)
    /// </summary>
    public PersonnageData TrouverPersonnageSansMetier()
    {
        PersonnageData[] tous = GameObject.FindObjectsOfType<PersonnageData>();
        foreach (var perso in tous)
        {
            if (perso.metier == JobType.Aucun)
            {
                return perso;
            }
        }
        Debug.Log($"[MetierAssignment] Aucun personnage sans métier trouvé !");
        return null;
    }

}
