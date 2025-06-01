using UnityEngine;
using UnityEngine.EventSystems;


// Permet de sélectionner, prévisualiser et placer des bâtiments dans la scène, 
// tout en gérant la suppression automatique des objets sous le bâtiment, 
// l’ajout de points de recherche et la configuration des maisons.
public class SimpleBuildingPlacer : MonoBehaviour
{
    [Header("Références")]
    public GameObject[] buildingPrefabs;       // Tableau de prefabs de bâtiments disponibles
    public BuildingEraser eraser;              // Référence au script de suppression de bâtiments

    [Header("Spawn de personnages pour les maisons")]
    public GameObject personnagePrefab;        // Prefab du personnage à instancier pour les maisons
    public LayerMask layerSol;                 // Layer qui définit où se trouve le sol (pour spawn des personnages)

    [Header("Paramètres de placement")]
    public LayerMask placementObstaclesLayer;  // Layer pour détecter les obstacles empêchant le placement

    [Header("Récompenses de recherche")]
    public int pointsRechercheParBatiment = 5; // Nombre de points de recherche obtenus à chaque construction

    // Variables internes
    private GameObject buildingPrefab;         // Prefab du bâtiment actuellement sélectionné
    private GameObject previewBuilding;        // Instance de prévisualisation (demi-transparente)
    private bool isPlacing = false;            // Indique si on est en cours de placement
    private BuildingData selectedBuildingData; // Les données (ScriptableObject) du bâtiment choisi

    [Header("Layer de suppression automatique")]
    public LayerMask removableObjectsLayer;    // Layer des objets à supprimer sous le bâtiment

    void Update()
    {
        // Si on n’est pas en mode placement ou qu’il n’y a pas de prévisualisation, on sort
        if (isPlacing && previewBuilding != null)
        {
            // Si le mode suppression (eraser) est actif, on ne fait pas de placement
            if (eraser != null && eraser.IsEraseModeActive())
                return;

            // Fait suivre la souris à la prévisualisation
            FollowMouse();

            // Clic gauche : tenter de placer le bâtiment
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

            // Clic droit : annuler le placement en cours
            if (Input.GetMouseButtonDown(1))
            {
                CancelPlacement();
            }
        }
    }

    // Sélectionne un bâtiment parmi la liste de prefabs (par index).
    public void SelectBuilding(int index)
    {
        // Si le mode suppression est actif, on n’ouvre pas le placement
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


    // Sélectionne un bâtiment en fonction de son BuildingData (ScriptableObject), 
    // vérifie si assez de ressources sont disponibles.
    public void SelectBuildingByData(BuildingData data)
    {
        if (eraser != null && eraser.IsEraseModeActive())
            return;

        // Vérifie si le joueur a assez de ressources pour construire
        if (!ResourceManager.Instance.HasEnough(data.cost))
        {
            Debug.LogWarning("Pas assez de ressources pour ce bâtiment !");
            return;
        }

        buildingPrefab = data.prefab;
        selectedBuildingData = data;
        StartPlacing();
    }

    
    // Initialise l’instance de prévisualisation (previewBuilding) et passe en mode placement.
    public void StartPlacing()
    {
        if (buildingPrefab == null)
        {
            Debug.LogError("Aucun prefab assigné !");
            return;
        }

        // Si une prévisualisation existe déjà, on la détruit pour en recréer une nouvelle
        if (previewBuilding != null)
        {
            Destroy(previewBuilding);
        }

        // Instancie la prévisualisation du bâtiment
        previewBuilding = Instantiate(buildingPrefab);

        // Désactive le collider de la prévisualisation pour ne pas interférer
        Collider2D col = previewBuilding.GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Met la prévisualisation sur le layer par défaut (pour éviter les collisions)
        previewBuilding.layer = LayerMask.NameToLayer("Default");

        // Rend la prévisualisation semi-transparente en vert
        SpriteRenderer sr = previewBuilding.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = new Color(0f, 1f, 0f, 0.5f);
        }

        isPlacing = true;
    }


    // Fait suivre la position de la souris à la prévisualisation, en arrondissant aux coordonnées entières.
    // Met à jour la couleur en fonction de la validité du placement.
    void FollowMouse()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = 10f; // Distance arbitraire de la caméra
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        Vector3 snappedPos = new Vector3(Mathf.Round(worldPos.x), Mathf.Round(worldPos.y), 0f);

        previewBuilding.transform.position = snappedPos;
        UpdatePreviewColor();
    }


    // Met à jour la couleur de la prévisualisation :
    // - Rouge si on ne peut pas placer
    // - Orange si des objets supprimables sont présents
    // - Vert sinon
    void UpdatePreviewColor()
    {
        SpriteRenderer sr = previewBuilding.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        bool canPlace = CanPlace();
        bool hasRemovable = HasRemovableObjectsUnderPreview();

        if (!canPlace)
            sr.color = new Color(1f, 0f, 0f, 0.5f);   // Rouge = interdiction
        else if (hasRemovable)
            sr.color = new Color(1f, 0.5f, 0f, 0.5f); // Orange = on peut placer mais il y a des objets à supprimer
        else
            sr.color = new Color(0f, 1f, 0f, 0.5f);   // Vert = placement autorisé
    }

    // Vérifie la présence d’objets supprimables sous la prévisualisation (ex : herbe, rocher).
    private bool HasRemovableObjectsUnderPreview()
    {
        Vector2 centerPos = previewBuilding.transform.position;
        Vector2 boxCenter = new Vector2(centerPos.x, centerPos.y - 0.5f);
        Vector2 boxSize = new Vector2(3f, 3f);

        Collider2D[] overlaps = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, removableObjectsLayer);
        return overlaps.Length > 0;
    }


    // Vérifie si la position actuelle de previewBuilding est valide pour y placer un bâtiment.
    bool CanPlace()
    {
        // 1. Empêche si la souris est au-dessus d’un élément UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return false;

        // 2. Détermine la zone de test pour le bâtiment (boîte 3x3 centrée)
        Vector2 centerPos = previewBuilding.transform.position;
        Vector2 boxCenter = new Vector2(centerPos.x, centerPos.y - 0.5f);
        Vector2 boxSize = new Vector2(3f, 3f);

        // 3. Si c’est un port, on utilise une règle spécifique (ratio sol/eau)
        BatimentInteractif portTest = previewBuilding.GetComponent<BatimentInteractif>();
        if (portTest != null && portTest.estUnPort)
        {
            return EstPlacementValidePort(boxCenter, boxSize);
        }

        // 4. Vérifie la présence d’un obstacle (layer water, falaise, etc.)
        Collider2D obstacleHit = Physics2D.OverlapBox(boxCenter, boxSize, 0f, placementObstaclesLayer);
        if (obstacleHit != null)
            return false;

        // 5. Vérifie que l’intégralité du bâtiment est sur le sol
        if (!EstEntierementSurLeSol(boxCenter, boxSize))
        {
            Debug.Log("Le bâtiment n’est pas entièrement sur le sol !");
            return false;
        }

        // 6. Vérifie qu’aucun personnage ne se trouve sous le bâtiment
        if (ContientDesPersonnagesSousBâtiment())
        {
            Debug.Log("Un personnage empêche le placement !");
            return false;
        }

        // 7. Tout est bon : on peut placer
        return true;
    }

    // Instancie le bâtiment définitivement dans la scène, dépense les ressources,
    // supprime les objets sous le bâtiment, configure la maison si besoin
    // et ajoute les points de recherche.
    void PlaceBuilding()
    {
        // Vérifie une dernière fois si le player peut dépenser les ressources
        if (!ResourceManager.Instance.Spend(selectedBuildingData.cost))
        {
            Debug.LogWarning("Ressources insuffisantes pour finaliser la construction !");
            return;
        }

        // Supprime les objets supprimables sous la zone de placement
        SupprimerObjetsSousBâtiment(previewBuilding.transform.position);

        // Instancie le bâtiment à la position de la prévisualisation
        GameObject placed = Instantiate(buildingPrefab, previewBuilding.transform.position, Quaternion.identity);

        // Réactive le collider
        Collider2D col = placed.GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = true;
        }

        // Place le nouvel objet sur le layer "Buildings"
        placed.layer = LayerMask.NameToLayer("Buildings");

        // Marque le BatimentInteractif comme placé pour déclencher ses logiques
        BatimentInteractif bat = placed.GetComponent<BatimentInteractif>();
        if (bat != null)
        {
            bat.estPlace = true;
        }

        // Lie le composant Building aux données sélectionnées
        Building buildingComponent = placed.GetComponent<Building>();
        if (buildingComponent != null)
        {
            buildingComponent.data = selectedBuildingData;
        }
        // Lie aussi les données sur le BatimentInteractif
        BatimentInteractif interactif = placed.GetComponent<BatimentInteractif>();
        if (interactif != null)
        {
            interactif.data = selectedBuildingData;
        }

        // Configure la maison si le bâtiment est une maison
        ConfigurerMaisonSiNecessaire(placed);

        // Ajoute les points de recherche au ResourceManager
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.Add(ResourceType.Search, pointsRechercheParBatiment);
            Debug.Log($"+{pointsRechercheParBatiment} points de recherche pour construction de {selectedBuildingData?.buildingName ?? "bâtiment"}");
        }

        // Detruit la prévisualisation et sort du mode placement
        Destroy(previewBuilding);
        previewBuilding = null;
        isPlacing = false;

        Debug.Log("Bâtiment placé avec succès !");
    }

    // Configure un HouseSpawner si le bâtiment est reconnu comme une maison.
    // Utilise plusieurs méthodes pour détecter une "maison" : 
    // - Vérifie le nom (contains "maison", "house", "cabane", etc.)
    // - Vérifie s’il y a déjà un HouseSpawner
    private void ConfigurerMaisonSiNecessaire(GameObject batiment)
    {
        bool estUneMaison = false;

        // Méthode 1 : Vérifier par le nom du bâtiment (minuscules pour simplifier la comparaison)
        if (selectedBuildingData != null)
        {
            string nomBatiment = selectedBuildingData.buildingName.ToLower();
            estUneMaison = nomBatiment.Contains("maison") ||
                          nomBatiment.Contains("house") ||
                          nomBatiment.Contains("habitation") ||
                          nomBatiment.Contains("cabane") ||
                          nomBatiment.Contains("logement");
        }

        // Méthode 2 : Vérifier si un HouseSpawner existe déjà
        HouseSpawner spawnerExistant = batiment.GetComponent<HouseSpawner>();
        if (spawnerExistant != null)
        {
            estUneMaison = true;
        }

        // Si c’est bien une maison, on ajoute ou configure le HouseSpawner
        if (estUneMaison)
        {
            HouseSpawner spawner = spawnerExistant;

            // Si aucun spawner n'existe, on l'ajoute dynamiquement
            if (spawner == null)
            {
                spawner = batiment.AddComponent<HouseSpawner>();
            }

            // Configure le prefab de personnage et la layer sol via reflection
            if (personnagePrefab != null)
            {
                var field = typeof(HouseSpawner).GetField("personnagePrefab");
                if (field != null) field.SetValue(spawner, personnagePrefab);
            }

            var layerField = typeof(HouseSpawner).GetField("layerSol");
            if (layerField != null) layerField.SetValue(spawner, layerSol);

            // Active le système de spawn de la maison
            spawner.Activer();

            Debug.Log($"Maison activée : {selectedBuildingData?.buildingName ?? batiment.name}");
        }
    }

    // Annule le placement en cours : détruit la prévisualisation et réinitialise les variables.
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

    // Vérifie si un emplacement est valide pour un port en calculant un ratio de cases sur l’eau et sur le sol.
    // Exige un ratio de 20% à 60% de sol pour être considéré comme valide pour le port.
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

                // Incrémente sol ou eau selon les layers
                if (Physics2D.OverlapPoint(point, LayerMask.GetMask("Ground"))) sol++;
                else if (Physics2D.OverlapPoint(point, LayerMask.GetMask("Water"))) eau++;

                total++;
            }
        }

        float ratioSol = (float)sol / total;
        float ratioEau = (float)eau / total;

        // On demande 20% ≤ sol ≤ 60% pour être un port valable
        return ratioSol >= 0.2f && ratioSol <= 0.6f;
    }

    // Supprime tous les objets (colliders) du layer removableObjectsLayer sous le futur bâtiment.
    private void SupprimerObjetsSousBâtiment(Vector2 position)
    {
        Vector2 boxCenter = new Vector2(position.x, position.y - 0.5f);
        Vector2 boxSize = new Vector2(3f, 3f);

        Collider2D[] toRemove = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, removableObjectsLayer);
        foreach (var col in toRemove)
        {
            Destroy(col.gameObject);
            Debug.Log($"Objet supprimé : {col.gameObject.name}");
        }
    }

    // Vérifie si un personnage (tag "Personnage") se trouve sous la prévisualisation.
    // Empêche le placement s’il y a un personnage en dessous.
    private bool ContientDesPersonnagesSousBâtiment()
    {
        Vector2 centerPos = previewBuilding.transform.position;
        Vector2 boxCenter = new Vector2(centerPos.x, centerPos.y - 0.5f);
        Vector2 boxSize = new Vector2(3f, 3f);

        Collider2D[] overlaps = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f);

        foreach (var col in overlaps)
        {
            if (col.CompareTag("Personnage"))
            {
                return true;
            }
        }

        return false;
    }


    // Vérifie si chaque point  de la boîte du bâtiment recouvre du sol (layer "Ground").
    // Retourne false si une des cases n’est pas sur le layer sol.

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
