using UnityEngine;

public class SimpleBuildingPlacer : MonoBehaviour
{
    [Header("Références")]
    public GameObject[] buildingPrefabs; // 💡 Liste des bâtiments disponibles
    public BuildingEraser eraser; // Référence à ton BuildingEraser

    [Header("Paramètres de placement")]
    public LayerMask placementObstaclesLayer;

    private GameObject buildingPrefab;
    private GameObject previewBuilding;
    private bool isPlacing = false;

    void Update()
    {
        if (isPlacing && previewBuilding != null)
        {
            if (eraser != null && eraser.IsEraseModeActive()== true){
                return; // On bloque la pose si on est en mode suppression
            }

            FollowMouse();

            // 🖱️ Clic gauche = placer
            if (Input.GetMouseButtonDown(0))
            {
                if (CanPlace())
                {
                    PlaceBuilding();
                }
                else
                {
                    Debug.Log("❌ Impossible de placer ici !");
                }
            }

            // 🖱️ Clic droit = annuler
            if (Input.GetMouseButtonDown(1))
            {
                CancelPlacement();
            }
        }
    }

    // 🏗️ Choisir un bâtiment via UI ou autre
    public void SelectBuilding(int index)
    {   
        if (eraser.IsEraseModeActive()== true){
                return; // On bloque la pose si on est en mode suppression
            }
        if (index < 0 || index >= buildingPrefabs.Length)
        {
            Debug.LogError("❌ Index de bâtiment invalide !");
            return;
        }

        buildingPrefab = buildingPrefabs[index];
        StartPlacing();
    }

    public void StartPlacing()
    {
        if (buildingPrefab == null)
        {
            Debug.LogError("❌ Aucun prefab assigné !");
            return;
        }

        if (previewBuilding != null)
        {
            Destroy(previewBuilding);
        }

        previewBuilding = Instantiate(buildingPrefab);

        Collider2D col = previewBuilding.GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        previewBuilding.layer = LayerMask.NameToLayer("Default");

        SpriteRenderer sr = previewBuilding.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = new Color(0f, 1f, 0f, 0.5f);
        }

        isPlacing = true;
    }

    void FollowMouse()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = 10f;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        Vector3 snappedPos = new Vector3(Mathf.Round(worldPos.x), Mathf.Round(worldPos.y), 0f);

        previewBuilding.transform.position = snappedPos;

        UpdatePreviewColor();
    }

    void UpdatePreviewColor()
    {
        SpriteRenderer sr = previewBuilding.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        sr.color = CanPlace()
            ? new Color(0f, 1f, 0f, 0.5f)
            : new Color(1f, 0f, 0f, 0.5f);
    }

    bool CanPlace()
    {
        Vector2 centerPos = previewBuilding.transform.position;
        Vector2 boxCenter = new Vector2(centerPos.x, centerPos.y + 1f);
        Vector2 boxSize = new Vector2(3f, 3f);

        Collider2D hit = Physics2D.OverlapBox(boxCenter, boxSize, 0f, placementObstaclesLayer);
        return hit == null;
    }

    void PlaceBuilding()
    {
        GameObject placed = Instantiate(buildingPrefab, previewBuilding.transform.position, Quaternion.identity);

        // ✅ Activer le collider
        Collider2D col = placed.GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = true;
        }

        // ✅ Changer la Layer sur "Obstacles"
        placed.layer = LayerMask.NameToLayer("Buildings");

        Destroy(previewBuilding);
        previewBuilding = null;
        isPlacing = false;

        Debug.Log("✅ Bâtiment placé sur la layer Buildings !");
    }

    void CancelPlacement()
    {
        if (previewBuilding != null)
        {
            Destroy(previewBuilding);
            previewBuilding = null;
            isPlacing = false;

            Debug.Log("❌ Placement annulé !");
        }
    }

    
}
