using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class FadeController : MonoBehaviour
{
    public Image FadePanel;
    public float FadeDuration = 2f;

    void Start()
    {
        if (FadePanel != null)
        {
            StartCoroutine(FadeFromBlack());
        }
        else
        {
            Debug.LogError("FadePanel n'est pas assigné dans l'inspecteur !");
        }
    }

    IEnumerator FadeFromBlack()
    {
        Color color = FadePanel.color;

        float timer = 0f;
        while (timer < FadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / FadeDuration);
            color.a = alpha;
            FadePanel.color = color;
            yield return null;
        }

        color.a = 0f;
        FadePanel.color = color;
        FadePanel.gameObject.SetActive(false);
    }
    
    public void StartFadeOut()
    {
        StartCoroutine(FadeToBlack());
    }

    IEnumerator FadeToBlack()
    {
        FadePanel.gameObject.SetActive(true);
        Color color = FadePanel.color;
        color.a = 0f;
        FadePanel.color = color;

        float timer = 0f;
        while (timer < FadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, timer / FadeDuration);
            color.a = alpha;
            FadePanel.color = color;
            yield return null;
        }

        color.a = 1f;
        FadePanel.color = color;

        SceneManager.LoadScene("Menu");
    }
}