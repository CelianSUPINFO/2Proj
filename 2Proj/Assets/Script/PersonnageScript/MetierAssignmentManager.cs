using System.Collections.Generic;
using UnityEngine;

// Ce script permet de gérer les affectations de métiers pour les personnages.
// Il centralise tous les personnages existants et essaie de leur trouver un métier automatiquement.

public class MetierAssignmentManager : MonoBehaviour
{
    // Singleton : permet d’accéder facilement à cette classe depuis d’autres scripts
    public static MetierAssignmentManager Instance;

    // Liste qui contient tous les personnages enregistrés dans le jeu
    private List<PersonnageData> tousLesPersonnages = new List<PersonnageData>();

    private void Awake()
    {
        // Implémentation du Singleton
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject); // Empêche d’avoir plusieurs managers
    }

    // Cette méthode ajoute un personnage dans la liste s’il n’est pas déjà enregistré
    public void EnregistrerPersonnage(PersonnageData perso)
    {
        if (!tousLesPersonnages.Contains(perso))
        {
            tousLesPersonnages.Add(perso);
        }
    }

    // Cette méthode supprime un personnage de la liste quand il meurt
    // Et prévient les bâtiments qu’un de leurs travailleurs est mort
    public void SupprimerPersonnage(PersonnageData perso)
    {
        Debug.Log("SupprimerPersonnage");
        tousLesPersonnages.Remove(perso); // On l’enlève de la liste

        // On va chercher tous les bâtiments et on leur dit que le perso est mort
        BatimentInteractif[] batiments = GameObject.FindObjectsOfType<BatimentInteractif>();
        foreach (var bat in batiments)
        {
            bat.GererMortTravailleur(perso);
        }
    }

    // Nouvelle méthode qui cherche un personnage SANS métier ET SANS bâtiment (pour éviter les conflits)
    public PersonnageData TrouverPersonnageSansMetierEtSansBatiment()
    {
        PersonnageData[] tous = GameObject.FindObjectsOfType<PersonnageData>();

        foreach (var perso in tous)
        {
            if (perso.metier == JobType.Aucun)
            {
                Debug.Log($"[MetierAssignment] Candidat possible : {perso.name}");
                return perso; // Dès qu’on en trouve un, on le retourne
            }
        }

        Debug.Log("[MetierAssignment] Aucun personnage sans métier ET sans bâtiment trouvé !");
        return null;
    }

    //  Ancienne version qui ne regarde que le métier (utile si on veut ignorer les bâtiments)
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

        return null; // Aucun trouvé
    }

    // Cette méthode essaie d’assigner un travail à un personnage donné
    public void TrouverDuJob(PersonnageData perso)
    {
        // On regarde tous les bâtiments
        BatimentInteractif[] batiments = GameObject.FindObjectsOfType<BatimentInteractif>();

        foreach (var bat in batiments)
        {
            // On ne s’intéresse qu’aux bâtiments qui ont un métier associé
            if (bat.metierAssocie != JobType.Aucun)
            {
                // On propose le personnage au bâtiment
                bat.candidatsAssignerAuBatiment(perso);
            }
        }
    }
}
