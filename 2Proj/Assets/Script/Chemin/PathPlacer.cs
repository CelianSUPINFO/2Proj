using UnityEngine;
using UnityEngine.EventSystems;

// Script qui permet de placer des chemins dans le monde (clic souris)
public class PathPlacementManager : MonoBehaviour
{
    public GameObject previewPath;       // Objet temporaire pour l’aperçu
    public GameObject pathPrefab;        // Préfab du chemin à instancier
    public LayerMask placementObstaclesLayer; // Empêche de construire sur certains objets
    public LayerMask groundLayer;        // Le chemin doit être placé sur du sol
    public int pathCost = 5;             // Coût (non utilisé ici)

    private bool isPlacing = false;

    public void StartPlacing(GameObject prefab)
    {
        if (previewPath != null) Destroy(previewPath);

        pathPrefab = prefab;
        previewPath = Instantiate(pathPrefab);

        // On désactive le collider du preview pour ne pas bloquer la pose
        Collider2D col = previewPath.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        isPlacing = true;
    }

    public void StopPlacing()
    {
        if (previewPath != null) Destroy(previewPath);
        previewPath = null;
        isPlacing = false;
    }

    void Update()
    {
        if (!isPlacing || previewPath == null) return;

        // Le preview suit la souris en s’alignant à la grille
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        previewPath.transform.position = SnapToGrid(worldPos);

        // Clique gauche → on essaie de placer
        if (Input.GetMouseButtonDown(0) && CanPlace())
        {
            PlacePath();
        }
    }

    Vector2 SnapToGrid(Vector2 pos)
    {
        return new Vector2(Mathf.Round(pos.x), Mathf.Round(pos.y));
    }

    bool CanPlace()
    {
        // Empêche si on clique sur un élément de l'UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return false;

        Vector2 centerPos = previewPath.transform.position;
        Vector2 boxCenter = new Vector2(centerPos.x, centerPos.y - 0.5f);
        Vector2 boxSize = new Vector2(1f, 1f);

        // Vérifie qu’il n’y a pas d’obstacle
        if (Physics2D.OverlapBox(boxCenter, boxSize, 0f, placementObstaclesLayer)) return false;

        // Vérifie qu’on est bien sur du sol
        if (!Physics2D.OverlapBox(boxCenter, boxSize, 0f, groundLayer)) return false;

        return true;
    }

    void PlacePath()
    {
        // Instancie le chemin à l’endroit validé
        GameObject placed = Instantiate(pathPrefab, previewPath.transform.position, Quaternion.identity);
        Collider2D col = placed.GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        // On place sur le layer "Path"
        placed.layer = LayerMask.NameToLayer("Path");

        Destroy(previewPath);
        previewPath = null;
        isPlacing = false;

        Debug.Log("Chemin placé !");
    }
}
