using UnityEngine;
using UnityEngine.UI;

public class DayNightCycle : MonoBehaviour
{
    public Image overlayImage;
    public float cycleDuration = 60f;

    [Tooltip("Courbe personnalisée pour la luminosité")]
    public AnimationCurve lightCurve = AnimationCurve.EaseInOut(0, 0.05f, 1, 0.05f);

    [Tooltip("Couleur de base de la nuit")]
    public Color nightColor = new Color32(40, 50, 80, 255);

    [Tooltip("Couleur chaude pour l’aube/crépuscule")]
    public Gradient colorGradient;

    private float time;

    void Update()
    {
        time += Time.deltaTime;
        float cycleTime = (time % cycleDuration) / cycleDuration;

        float alpha = lightCurve.Evaluate(cycleTime);
        Color dynamicColor = colorGradient.Evaluate(cycleTime);
        dynamicColor.a = alpha;

        overlayImage.color = dynamicColor;
    }
}