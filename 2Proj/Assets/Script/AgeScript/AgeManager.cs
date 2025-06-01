using UnityEngine;
using TMPro;

// Classe qui gère l'âge actuel du jeu et permet de passer à l'âge suivant
public class AgeManager : MonoBehaviour
{
    // Instance statique pour accéder facilement à AgeManager depuis d'autres scripts
    public static AgeManager Instance { get; private set; }

    // Âge actuel du jeu (défini dans l’inspecteur si besoin)
    [SerializeField] public GameAge currentAge = GameAge.StoneAge;

    // Référence au texte UI pour afficher l’âge à l’écran
    [SerializeField] private TMP_Text ageDisplayText; 

    // Méthode appelée dès que le script est chargé
    private void Awake()
    {
        // Singleton : si une autre instance existe déjà, on la détruit pour éviter les doublons
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Sinon, on définit cette instance comme la référence globale
        Instance = this;

        // On met à jour l'affichage avec l'âge actuel
        UpdateAgeDisplay();
    }

    // Renvoie l’âge actuel 
    public GameAge GetCurrentAge()
    {
        return currentAge;
    }

    // Passe à l’âge suivant si ce n’est pas déjà le dernier
    public void AdvanceToNextAge()
    {
        // Vérifie si l’âge actuel n’est pas le dernier dans l’énumération
        if ((int)currentAge < System.Enum.GetValues(typeof(GameAge)).Length - 1)
        {
            currentAge++; 
            Debug.Log("Nouvel âge : " + currentAge); 
            UpdateAgeDisplay(); // Met à jour le texte affiché
        }
        else
        {
            // Si on est déjà à l’âge max, on affiche un message
            Debug.Log("Tu es déjà à l’âge maximum.");
        }
    }

    // Met à jour le texte affiché sur l'UI
    private void UpdateAgeDisplay()
    {
        if (ageDisplayText != null)
            ageDisplayText.text = $"{GetDisplayName(currentAge)}"; // Affiche le nom lisible de l’âge
    }

    // Traduit en texte lisible pour l’utilisateur
    private string GetDisplayName(GameAge age)
    {
        return age switch
        {
            GameAge.StoneAge => "Âge de pierre",
            GameAge.AncientAge => "Âge antique",
            GameAge.MedievalAge => "Âge médiéval",
            GameAge.IndustrialAge => "Âge industriel",
            _ => age.ToString() // Par défaut, affiche le nom brut de l'enum
        };
    }
}
