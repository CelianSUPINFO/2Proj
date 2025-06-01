using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MusicVolume : MonoBehaviour
{
    public AudioSource musicSource;
    private Slider volumeSlider;

    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (scene.name == "Menu") // ou ton vrai nom de scène
        {
            // Essaye de trouver le Slider dans la hiérarchie
            volumeSlider = GameObject.Find("Slider")?.GetComponent<Slider>();

            if (volumeSlider != null && musicSource != null)
            {
                volumeSlider.value = musicSource.volume;
                volumeSlider.onValueChanged.AddListener(SetVolume);
            }
        }
    }

    public void SetVolume(float value)
    {
        if (musicSource != null)
            musicSource.volume = value;
    }
}