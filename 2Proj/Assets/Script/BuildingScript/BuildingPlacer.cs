using UnityEngine;
using UnityEngine.EventSystems;

public class SimpleBuildingPlacer : MonoBehaviour
{
    [Header("Références")]
    public GameObject[] buildingPrefabs;
    public BuildingEraser eraser;

    [Header("Spawn de personnages pour les maisons")]
    public GameObject personnagePrefab; // Prefab du personnage à spawner
    public LayerMask layerSol; // Layer du sol

    [Header("Paramètres de placement")]
    public LayerMask placementObstaclesLayer;

    [Header("🔬 Récompenses de recherche")]
    public int pointsRechercheParBatiment = 5; // Points gagnés par bâtiment placé

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


        // 3. Vérifie s'il y a un obstacle (eau, falaise, etc.)
        Collider2D obstacleHit = Physics2D.OverlapBox(boxCenter, boxSize, 0f, placementObstaclesLayer);
        if (obstacleHit != null)
            return false;

        // 4. Vérifie si on est bien sur du sol (Layer Ground)
        Collider2D groundHit = Physics2D.OverlapBox(boxCenter, boxSize, 0f, LayerMask.GetMask("Ground"));
        // Si c'est un port, on applique une règle spéciale
        // BatimentInteractif portTest = previewBuilding.GetComponent<BatimentInteractif>();
        // if (portTest != null && portTest.estUnPort)
        // {
        //     return EstPlacementValidePort(boxCenter, boxSize);
        // }

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

        // 🔥 NOUVEAU : Vérifier si c'est une maison et configurer le spawner
        ConfigurerMaisonSiNecessaire(placed);

        // 🔬 NOUVEAU : Gagner des points de recherche pour avoir placé un bâtiment
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.Add(ResourceType.Search, pointsRechercheParBatiment);
            Debug.Log($" +{pointsRechercheParBatiment} points de recherche pour construction de {selectedBuildingData?.buildingName ?? "bâtiment"}");
        }

        Destroy(previewBuilding);
        previewBuilding = null;
        isPlacing = false;

        Debug.Log("Bâtiment placé avec succès !");
    }

    /// <summary>
    /// 🔥 NOUVELLE MÉTHODE : Configure le spawner si le bâtiment est une maison
    /// </summary>
    private void ConfigurerMaisonSiNecessaire(GameObject batiment)
    {
        // Méthode 1 : Vérifier par le nom du bâtiment
        bool estUneMaison = false;

        if (selectedBuildingData != null)
        {
            string nomBatiment = selectedBuildingData.buildingName.ToLower();
            estUneMaison = nomBatiment.Contains("maison") ||
                          nomBatiment.Contains("house") ||
                          nomBatiment.Contains("habitation") ||
                          nomBatiment.Contains("cabane") ||
                          nomBatiment.Contains("logement");
        }



        // Méthode 3 : Vérifier si le bâtiment a déjà un HouseSpawner (au cas où il serait préconfiguré)
        HouseSpawner spawnerExistant = batiment.GetComponent<HouseSpawner>();
        if (spawnerExistant != null)
        {
            estUneMaison = true;
        }

        // Si c'est une maison, ajouter/configurer le spawner
        if (estUneMaison)
        {
            HouseSpawner spawner = spawnerExistant;

            if (spawner == null)
            {
                spawner = batiment.AddComponent<HouseSpawner>();
            }

            // Configurer les paramètres
            if (personnagePrefab != null)
            {
                var field = typeof(HouseSpawner).GetField("personnagePrefab");
                if (field != null) field.SetValue(spawner, personnagePrefab);
            }

            var layerField = typeof(HouseSpawner).GetField("layerSol");
            if (layerField != null) layerField.SetValue(spawner, layerSol);

            // ✅ Active le système de spawn maintenant que le bâtiment est placé
            spawner.Activer();

            Debug.Log($"✅ Maison activée : {selectedBuildingData?.buildingName ?? batiment.name}");
        }

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
    
    private bool EstPlacementValidePort(Vector2 boxCenter, Vector2 boxSize)
    {
        int total = 0, sol = 0, eau = 0;
        int resolution = 5;

        float stepX = boxSize.x / (resolution - 1);
        float stepY = boxSize.y / (resolution - 1);

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                Vector2 point = boxCenter + new Vector2(-boxSize.x / 2 + x * stepX, -boxSize.y / 2 + y * stepY);

                if (Physics2D.OverlapPoint(point, LayerMask.GetMask("Ground"))) sol++;
                else if (Physics2D.OverlapPoint(point, LayerMask.GetMask("Water"))) eau++;

                total++;
            }
        }

        float ratioSol = (float)sol / total;
        float ratioEau = (float)eau / total;

        return ratioSol >= 0.2f && ratioSol <= 0.6f ;
    }

}