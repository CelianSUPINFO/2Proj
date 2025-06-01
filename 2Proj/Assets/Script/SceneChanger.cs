using UnityEngine;
using UnityEngine.SceneManagement; // Nécessaire pour changer de scène

public class SceneChanger : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
