using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class ZoomScrollRectAdvanced : MonoBehaviour
{
    [Header("Références")]
    public RectTransform content; // Assigné à NodesContent (le Content du ScrollRect)

    [Header("Zoom")]
    public float zoomStep = 0.1f;
    public float minZoom = 0.5f;
    public float maxZoom = 2.5f;

    [Header("Pan")]
    public bool enablePan = true;
    public float panSpeed = 1f;

    private ScrollRect scrollRect;
    private Vector3 initialScale;
    private Vector2 initialPivot;

    private Vector2 lastMousePosition;

    void Start()
    {
        scrollRect = GetComponent<ScrollRect>();

        if (content == null)
            content = scrollRect.content;

        initialScale = content.localScale;
        initialPivot = content.pivot;

        // Important : désactiver inertie pour le pan précis
        scrollRect.inertia = false;
    }

    void Update()
    {
        ZoomFromScrollWheel();
        HandlePan();
    }

    void ZoomFromScrollWheel()
    {
        if (!RectTransformUtility.RectangleContainsScreenPoint(
            scrollRect.viewport, Input.mousePosition, null))
            return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.01f)
            return;

        float currentScale = content.localScale.x;
        float targetScale = Mathf.Clamp(currentScale + scroll * zoomStep, minZoom, maxZoom);
        float scaleFactor = targetScale / currentScale;

        // Obtenir la position de la souris dans le Content
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(content, Input.mousePosition, null, out localPoint);

        // Calcule du nouveau pivot pour que la souris reste au même endroit visuellement
        Vector2 pivot = new Vector2(
            Mathf.InverseLerp(content.rect.xMin, content.rect.xMax, localPoint.x),
            Mathf.InverseLerp(content.rect.yMin, content.rect.yMax, localPoint.y)
        );

        // Appliquer pivot et mettre à jour l'échelle
        content.pivot = pivot;
        content.localScale = new Vector3(targetScale, targetScale, 1f);

        // Repositionnement forcé du layout si besoin
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    void HandlePan()
    {
        if (!enablePan)
            return;

        // clic droit ou clic molette
        if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            lastMousePosition = Input.mousePosition;

        if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            Vector2 delta = (Vector2)Input.mousePosition - lastMousePosition;

            scrollRect.horizontalNormalizedPosition -= delta.x / content.rect.width * panSpeed;
            scrollRect.verticalNormalizedPosition -= delta.y / content.rect.height * panSpeed;

            lastMousePosition = Input.mousePosition;
        }
    }

    public void ResetZoom()
    {
        content.localScale = initialScale;
        content.pivot = initialPivot;
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }
}
