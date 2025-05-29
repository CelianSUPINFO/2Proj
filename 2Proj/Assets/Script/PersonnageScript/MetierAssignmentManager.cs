using System.Collections.Generic;
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

    public void EnregistrerPersonnage(PersonnageData perso)
    {
        if (!tousLesPersonnages.Contains(perso))
        {
            tousLesPersonnages.Add(perso);
        }
    }

    public void SupprimerPersonnage(PersonnageData perso)
    {
        Debug.Log("SupprimerPersonnage");
        tousLesPersonnages.Remove(perso);
        BatimentInteractif[] batiments = GameObject.FindObjectsOfType<BatimentInteractif>();
        foreach (var bat in batiments)
        {
            bat.GererMortTravailleur(perso);
        }
    }

    // NOUVELLE METHODE : SANS METIER ET SANS BATIMENT
    public PersonnageData TrouverPersonnageSansMetierEtSansBatiment()
    {
        PersonnageData[] tous = GameObject.FindObjectsOfType<PersonnageData>();
        foreach (var perso in tous)
        {
            if (perso.metier == JobType.Aucun)
            {
                Debug.Log($"[MetierAssignment] Candidat possible : {perso.name}");
                return perso;
            }
        }
        Debug.Log($"[MetierAssignment] Aucun personnage sans métier ET sans bâtiment trouvé !");
        return null;
    }

    // Garde aussi l'ancienne si tu veux
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
        return null;
    }

    public void TrouverDuJob(PersonnageData perso)
    {
        BatimentInteractif[] batiments = GameObject.FindObjectsOfType<BatimentInteractif>();
        foreach (var bat in batiments)
        {
            if (bat.metierAssocie != JobType.Aucun)
            {
                bat.candidatsAssignerAuBatiment(perso);
            }
        }
    }
}
