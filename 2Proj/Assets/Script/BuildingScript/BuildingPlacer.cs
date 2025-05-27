using UnityEngine;
using UnityEngine.EventSystems;

public class SimpleBuildingPlacer : MonoBehaviour
{
    [Header("Références")]
    public GameObject[] buildingPrefabs; 
    public BuildingEraser eraser;

    [Header("Paramètres de placement")]
    public LayerMask placementObstaclesLayer;

    private GameObject buildingPrefab;
    private GameObject previewBuilding;
    private bool isPlacing = false;
    private BuildingData selectedBuildingData;

    void Update()
    {
        if (isPlacing && previewBuilding != null)
        {
            if (eraser != null && eraser.IsEraseModeActive())
                return;

            FollowMouse();

            if (Input.GetMouseButtonDown(0))
            {
                if (CanPlace())
                {
                    PlaceBuilding();
                }
                else
                {
                    Debug.Log("Impossible de placer ici !");
                }
            }

            if (Input.GetMouseButtonDown(1))
            {
                CancelPlacement();
            }
        }
    }

    public void SelectBuilding(int index)
    {   
        if (eraser != null && eraser.IsEraseModeActive())
            return;

        if (index < 0 || index >= buildingPrefabs.Length)
        {
            Debug.LogError("Index de bâtiment invalide !");
            return;
        }

        buildingPrefab = buildingPrefabs[index];
        StartPlacing();
    }

    public void SelectBuildingByData(BuildingData data)
    {
        if (eraser != null && eraser.IsEraseModeActive())
            return;

        if (!ResourceManager.Instance.HasEnough(data.cost))
        {
            Debug.LogWarning("Pas assez de ressources pour ce bâtiment !");
            return;
        }

        buildingPrefab = data.prefab;
        selectedBuildingData = data;
        StartPlacing();
    }

    public void StartPlacing()
    {
        if (buildingPrefab == null)
        {
            Debug.LogError("Aucun prefab assigné !");
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
        // 1. Empêche si curseur sur l'UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return false;

        // 2. Calcul des positions pour la box
        Vector2 centerPos = previewBuilding.transform.position;
        Vector2 boxCenter = new Vector2(centerPos.x, centerPos.y - 0.5f);
        Vector2 boxSize = new Vector2(3f, 3f);

        // 3. Vérifie s’il y a un obstacle (eau, falaise, etc.)
        Collider2D obstacleHit = Physics2D.OverlapBox(boxCenter, boxSize, 0f, placementObstaclesLayer);
        if (obstacleHit != null)
            return false;

        // 4. Vérifie si on est bien sur du sol (Layer Ground)
        Collider2D groundHit = Physics2D.OverlapBox(boxCenter, boxSize, 0f, LayerMask.GetMask("Ground"));
        if (groundHit == null)
            return false;

        // 5. Autorisé !
        return true;
    }

    void PlaceBuilding()
    {
        if (!ResourceManager.Instance.Spend(selectedBuildingData.cost))
        {
            Debug.LogWarning("Ressources insuffisantes pour finaliser la construction !");
            return;
        }

        GameObject placed = Instantiate(buildingPrefab, previewBuilding.transform.position, Quaternion.identity);

        Collider2D col = placed.GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = true;
        }

        placed.layer = LayerMask.NameToLayer("Buildings");

        // Lien entre prefab et données
        Building buildingComponent = placed.GetComponent<Building>();
        if (buildingComponent != null)
        {
            buildingComponent.data = selectedBuildingData;
        }

        Destroy(previewBuilding);
        previewBuilding = null;
        isPlacing = false;

        Debug.Log("Bâtiment placé avec succès !");
    }

    void CancelPlacement()
    {
        if (previewBuilding != null)
        {
            Destroy(previewBuilding);
            previewBuilding = null;
            isPlacing = false;

            Debug.Log("Placement annulé !");
        }
    }
}
