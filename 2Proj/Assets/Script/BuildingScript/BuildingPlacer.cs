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
    [Header("Layer de suppression automatique")]
    public LayerMask removableObjectsLayer;


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

        bool canPlace = CanPlace();
        bool hasRemovable = HasRemovableObjectsUnderPreview();

        if (!canPlace)
            sr.color = new Color(1f, 0f, 0f, 0.5f); // Rouge
        else if (hasRemovable)
            sr.color = new Color(1f, 0.5f, 0f, 0.5f); // Orange
        else
            sr.color = new Color(0f, 1f, 0f, 0.5f); // Vert
    }
    private bool HasRemovableObjectsUnderPreview()
    {
        Vector2 centerPos = previewBuilding.transform.position;
        Vector2 boxCenter = new Vector2(centerPos.x, centerPos.y - 0.5f);
        Vector2 boxSize = new Vector2(3f, 3f);

        Collider2D[] overlaps = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, removableObjectsLayer);
        return overlaps.Length > 0;
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


        // Si c'est un port, on applique une règle spéciale
        BatimentInteractif portTest = previewBuilding.GetComponent<BatimentInteractif>();
        if (portTest != null && portTest.estUnPort)
        {
            return EstPlacementValidePort(boxCenter, boxSize);
        }


        // 3. Vérifie s'il y a un obstacle (eau, falaise, etc.)
        Collider2D obstacleHit = Physics2D.OverlapBox(boxCenter, boxSize, 0f, placementObstaclesLayer);
        if (obstacleHit != null)
            return false;

        if (!EstEntierementSurLeSol(boxCenter, boxSize))
        {
            Debug.Log("❌ Le bâtiment n’est pas entièrement sur le sol !");
            return false;
        }
        
     
        if (ContientDesPersonnagesSousBâtiment())
        {
            Debug.Log("Un personnage empêche le placement !");
            return false;
        }

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

        SupprimerObjetsSousBâtiment(previewBuilding.transform.position);

        GameObject placed = Instantiate(buildingPrefab, previewBuilding.transform.position, Quaternion.identity);

        Collider2D col = placed.GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = true;
        }

        placed.layer = LayerMask.NameToLayer("Buildings");

        // ✅ Marquer le bâtiment comme placé
        BatimentInteractif bat = placed.GetComponent<BatimentInteractif>();
        if (bat != null)
        {
            bat.estPlace = true;
        }

        // Lien entre prefab et données
        Building buildingComponent = placed.GetComponent<Building>();
        if (buildingComponent != null)
        {
            buildingComponent.data = selectedBuildingData;
        }
        BatimentInteractif interactif = placed.GetComponent<BatimentInteractif>();
        if (interactif != null)
        {
            interactif.data = selectedBuildingData;
        }


        // 🔥 Configurer la maison si nécessaire
        ConfigurerMaisonSiNecessaire(placed);

        // 🔬 Ajouter des points de recherche
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

        return ratioSol >= 0.2f && ratioSol <= 0.6f;
    }

    private void SupprimerObjetsSousBâtiment(Vector2 position)
    {
        Vector2 boxCenter = new Vector2(position.x, position.y - 0.5f);
        Vector2 boxSize = new Vector2(3f, 3f);

        Collider2D[] toRemove = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, removableObjectsLayer);
        foreach (var col in toRemove)
        {
            Destroy(col.gameObject);
            Debug.Log($"🧹 Objet supprimé : {col.gameObject.name}");
        }
    }

    private bool ContientDesPersonnagesSousBâtiment()
    {
        Vector2 centerPos = previewBuilding.transform.position;
        Vector2 boxCenter = new Vector2(centerPos.x, centerPos.y - 0.5f);
        Vector2 boxSize = new Vector2(3f, 3f);

        Collider2D[] overlaps = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f);

        foreach (var col in overlaps)
        {
            if (col.CompareTag("Personnage")) // ou un composant spécifique comme "Personnage"
            {
                return true;
            }
        }

        return false;
    }

    private bool EstEntierementSurLeSol(Vector2 boxCenter, Vector2 boxSize)
    {
        int resolution = 5;
        float stepX = boxSize.x / (resolution - 1);
        float stepY = boxSize.y / (resolution - 1);

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                Vector2 point = boxCenter + new Vector2(-boxSize.x / 2 + x * stepX, -boxSize.y / 2 + y * stepY);
                if (!Physics2D.OverlapPoint(point, LayerMask.GetMask("Ground")))
                {
                    return false;
                }
            }
        }

        return true;
    }




}