using UnityEngine;

public class CameraController2D : MonoBehaviour
{
    [Header("Zoom")]
    public float zoomSpeed = 8f;
    public float minZoom = 3f;
    public float maxZoom = 50f;

    [Header("Déplacement")]
    public float panSpeed = 1f;
    private Vector3 lastMousePosition;

    [Header("Limites de déplacement")]
    public Vector2 minBounds; // coin bas gauche
    public Vector2 maxBounds; // coin haut droit

    void Update()
    {
        HandleZoom();
        HandlePanning();
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            Camera.main.orthographicSize -= scroll * zoomSpeed;
            Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, minZoom, maxZoom);
        }
    }

    void HandlePanning()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            Vector3 move = new Vector3(-delta.x, -delta.y, 0f) * panSpeed * Time.deltaTime;
            Camera.main.transform.Translate(move);
            ClampCameraPosition(); // ← empêche de sortir
            lastMousePosition = Input.mousePosition;
        }
    }

    void ClampCameraPosition()
    {
        float camHeight = Camera.main.orthographicSize;
        float camWidth = camHeight * Camera.main.aspect;

        float minX = minBounds.x + camWidth;
        float maxX = maxBounds.x - camWidth;
        float minY = minBounds.y + camHeight;
        float maxY = maxBounds.y - camHeight;

        Vector3 clamped = Camera.main.transform.position;
        clamped.x = Mathf.Clamp(clamped.x, minX, maxX);
        clamped.y = Mathf.Clamp(clamped.y, minY, maxY);
        Camera.main.transform.position = clamped;
    }
}
