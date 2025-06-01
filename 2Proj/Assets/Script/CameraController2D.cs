using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController2D : MonoBehaviour
{
    [Header("Zoom")]
    public float zoomSpeed = 8f;        // Vitesse de zoom de la molette
    public float minZoom = 3f;          // Zoom minimum (plus proche)
    public float maxZoom = 50f;         // Zoom maximum (plus éloigné)

    [Header("Déplacement")]
    public float panSpeed = 1f;         // Vitesse de déplacement (drag de la souris)
    private Vector3 lastMousePosition;  // Position de la souris lors du dernier clic

    [Header("Limites de déplacement")]
    public Vector2 minBounds; // Limite minimale du monde (coin bas gauche)
    public Vector2 maxBounds; // Limite maximale du monde (coin haut droit)

    void Update()
    {
        // On ignore les interactions si on survole l'UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        HandleZoom();    // Gère le zoom avec la molette
        HandlePanning(); // Gère le déplacement en maintenant le clic
    }

    void HandleZoom()
    {
        // Récupère le défilement de la molette de la souris
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            // Applique le zoom
            Camera.main.orthographicSize -= scroll * zoomSpeed;

            // Limite le zoom entre min et max
            Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, minZoom, maxZoom);
        }
    }

    void HandlePanning()
    {
        // Enregistre la position de la souris quand on commence à cliquer
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            lastMousePosition = Input.mousePosition;
        }

        // Si on maintient un clic gauche ou droit
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            // Calcule la différence entre la position actuelle et la précédente
            Vector3 delta = Input.mousePosition - lastMousePosition;

            // Transforme ce déplacement en mouvement de caméra, en inversant les axes
            Vector3 move = new Vector3(-delta.x, -delta.y, 0f) * panSpeed * Time.unscaledDeltaTime;
            Camera.main.transform.Translate(move);

            // Empêche la caméra de sortir des limites définies
            ClampCameraPosition();

            // Met à jour la dernière position connue de la souris
            lastMousePosition = Input.mousePosition;
        }
    }

    void ClampCameraPosition()
    {
        // Calcule la moitié de la largeur/hauteur de la vue selon le zoom actuel
        float camHeight = Camera.main.orthographicSize;
        float camWidth = camHeight * Camera.main.aspect;

        // Calcule les limites autorisées en tenant compte de la taille de l'écran
        float minX = minBounds.x + camWidth;
        float maxX = maxBounds.x - camWidth;
        float minY = minBounds.y + camHeight;
        float maxY = maxBounds.y - camHeight;

        // Applique les limites aux coordonnées de la caméra
        Vector3 clamped = Camera.main.transform.position;
        clamped.x = Mathf.Clamp(clamped.x, minX, maxX);
        clamped.y = Mathf.Clamp(clamped.y, minY, maxY);
        Camera.main.transform.position = clamped;
    }
}
