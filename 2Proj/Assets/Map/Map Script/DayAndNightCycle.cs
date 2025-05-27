using UnityEngine;
using UnityEngine.UI;

public class DayNightCycle : MonoBehaviour
{
    public Image overlayImage;  // Référence à l’image UI
    public float cycleDuration = 60f;  // Durée complète du cycle (en secondes)
    public AnimationCurve alphaCurve;  // Courbe d’alpha (opacité)
    public Gradient colorGradient;  // Couleurs du cycle

    private float timer = 0f;

    void Start()
    {
        if (overlayImage == null)
            Debug.LogError("Overlay Image non assignée !");
    }

    void Update()
    {
        timer += Time.deltaTime;
        float cycleTime = (timer % cycleDuration) / cycleDuration;

        // Alpha et couleur
        float alpha = alphaCurve.Evaluate(cycleTime);
        Color color = colorGradient.Evaluate(cycleTime);
        color.a = alpha;

        overlayImage.color = color;
    }
}