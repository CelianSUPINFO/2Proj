using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuNavigation : MonoBehaviour
{
    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("GameStart");
    }
}