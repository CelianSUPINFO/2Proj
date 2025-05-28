using UnityEngine;

public class GameSpeedController : MonoBehaviour
{
    private float[] speedLevels = { 1f, 2f, 3f, 5f, 10f };
    private int currentSpeedIndex = 0;

    public void PauseGame()
    {
        Time.timeScale = 0f;
        Debug.Log("Jeu en pause");
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        currentSpeedIndex = 0; // Réinitialise la vitesse à x1
        Debug.Log("Jeu en marche (x1)");
    }

    public void CycleSpeed()
    {
        currentSpeedIndex = (currentSpeedIndex + 1) % speedLevels.Length;
        Time.timeScale = speedLevels[currentSpeedIndex];
        Debug.Log("Vitesse du jeu : x" + speedLevels[currentSpeedIndex]);
    }
}
