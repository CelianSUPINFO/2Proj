using UnityEngine;
using UnityEngine.EventSystems;

public class PathPlacementManager : MonoBehaviour
{
    public GameObject previewPath;
    public GameObject pathPrefab;
    public LayerMask placementObstaclesLayer;
    public LayerMask groundLayer;
    public int pathCost = 5;

    private bool isPlacing = false;

    public void StartPlacing(GameObject prefab)
    {
        if (previewPath != null) Destroy(previewPath);

        pathPrefab = prefab;
        previewPath = Instantiate(pathPrefab);
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

        Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        previewPath.transform.position = SnapToGrid(worldPos);

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
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return false;

        Vector2 centerPos = previewPath.transform.position;
        Vector2 boxCenter = new Vector2(centerPos.x, centerPos.y - 0.5f);
        Vector2 boxSize = new Vector2(1f, 1f);

        Collider2D obstacleHit = Physics2D.OverlapBox(boxCenter, boxSize, 0f, placementObstaclesLayer);
        if (obstacleHit != null) return false;

        Collider2D groundHit = Physics2D.OverlapBox(boxCenter, boxSize, 0f, groundLayer);
        if (groundHit == null) return false;

        return true;
    }

    void PlacePath()
    {


        GameObject placed = Instantiate(pathPrefab, previewPath.transform.position, Quaternion.identity);
        Collider2D col = placed.GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        placed.layer = LayerMask.NameToLayer("Path");

        Destroy(previewPath);
        previewPath = null;
        isPlacing = false;

        Debug.Log("Chemin placé !");
    }
}
