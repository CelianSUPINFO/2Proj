using UnityEngine;

public class GameSpeedController : MonoBehaviour
{
    // Liste des vitesses possibles (multiplicateurs de la vitesse du temps)
    private float[] speedLevels = { 1f, 2f, 3f, 5f, 10f };

    // Index de la vitesse actuelle dans le tableau ci-dessus
    private int currentSpeedIndex = 0;

    // Met le jeu en pause en bloquant le temps
    public void PauseGame()
    {
        Time.timeScale = 0f; // Arrête le temps dans le jeu
        Debug.Log("Jeu en pause");
    }

    // Redémarre le jeu à vitesse normale (x1)
    public void ResumeGame()
    {
        Time.timeScale = 1f;           // Remet le temps à la normale
        currentSpeedIndex = 0;         // Réinitialise l'index à la vitesse de base
        Debug.Log("Jeu en marche (x1)");
    }

    // Change la vitesse du jeu à la suivante dans la liste
    public void CycleSpeed()
    {
        // Passe à la vitesse suivante en boucle
        currentSpeedIndex = (currentSpeedIndex + 1) % speedLevels.Length;

        // Applique la nouvelle vitesse au temps du jeu
        Time.timeScale = speedLevels[currentSpeedIndex];

        // Affiche dans la console la nouvelle vitesse appliquée
        Debug.Log("Vitesse du jeu : x" + speedLevels[currentSpeedIndex]);
    }
}
