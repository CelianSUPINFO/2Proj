using UnityEngine;

public class GameExit : MonoBehaviour
{
    public void QuitGame()
    {
        Debug.Log("Quitter le jeu...");

#if UNITY_EDITOR
        // Arrête le mode Play dans l'éditeur
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Ferme l'application dans une build
        Application.Quit();
#endif
    }
}