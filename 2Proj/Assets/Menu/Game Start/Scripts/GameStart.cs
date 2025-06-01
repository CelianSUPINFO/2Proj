using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStart : MonoBehaviour
{
    public void LoadMenuScene()
    {
        SceneManager.LoadScene("Menu"); 
    }
}