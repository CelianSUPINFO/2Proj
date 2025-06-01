using UnityEngine;
using System.Collections;

public class UIFader : MonoBehaviour
{
    public CanvasGroup LogoGroup;
    public CanvasGroup PlayButtonGroup;
    public float Delay = 1f;
    public float FadeDuration = 1.5f;

    void Start()
    {
        StartCoroutine(FadeElementsIn());
    }

    IEnumerator FadeElementsIn()
    {
        yield return new WaitForSeconds(Delay);

        float t = 0f;
        while (t < FadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(t / FadeDuration);
            LogoGroup.alpha = alpha;
            PlayButtonGroup.alpha = alpha;
            yield return null;
        }

        // S'assurer qu’ils sont pleinement visibles
        LogoGroup.alpha = 1f;
        PlayButtonGroup.alpha = 1f;
    }
}