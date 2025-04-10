using UnityEngine;

public class CameraController2D : MonoBehaviour
{
    [Header("Zoom")]
    public float zoomSpeed = 5f;
    public float minZoom = 3f;
    public float maxZoom = 15f;

    [Header("Déplacement")]
    public float panSpeed = 0.5f;
    private Vector3 lastMousePosition;

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
        // Bouton du milieu ou clic droit (button 2 ou 1)
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            Vector3 move = new Vector3(-delta.x, -delta.y, 0f) * panSpeed * Time.deltaTime;
            Camera.main.transform.Translate(move);
            lastMousePosition = Input.mousePosition;
        }
    }
}
