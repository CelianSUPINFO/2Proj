using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MenuFadeController : MonoBehaviour
{
    public Image FadePanel;
    public float FadeDuration = 2f;

    void Start()
    {
        if (FadePanel != null)
        {
            StartCoroutine(FadeFromBlack());
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
}