using UnityEngine;
using UnityEngine.EventSystems;

public class BuildingPlacer : MonoBehaviour
{
    public GameObject[] buildingPrefabs;  // Liste des bâtiments disponibles
    private GameObject buildingToPlace;   // Bâtiment actuellement sélectionné
    private GameObject previewBuilding;   // Bâtiment en prévisualisation
    
    void Update()
    {
        if (buildingToPlace != null)
        {
            MovePreviewToMouse();
            
            if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
            {
                PlaceBuilding();
            }
        }
    }

    public void SelectBuilding(int index)
    {
            // Vérifie si buildingPrefabs est bien initialisé
        if (buildingPrefabs == null)
        {
            Debug.LogError("buildingPrefabs est null !");
            return;
        }

        // Vérifie si l'index est valide
        Debug.Log(buildingPrefabs.Length);
        

        // Affiche tous les éléments du tableau buildingPrefabs
        for (int i = 0; i < buildingPrefabs.Length; i++)
        {
            Debug.Log($"buildingPrefabs[{i}] : {buildingPrefabs[i]?.name ?? "NULL"}");
        }
        if (previewBuilding != null)
            Destroy(previewBuilding);

        
        buildingToPlace = buildingPrefabs[index];  
        previewBuilding = Instantiate(buildingToPlace);
        Collider collider = previewBuilding.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false; // Désactiver la collision
        }
        else
        {
            Debug.LogWarning($"⚠️ Aucun Collider trouvé sur {previewBuilding.name} !");
        }
        
        previewBuilding.GetComponent<Renderer>().material.color = new Color(1, 1, 1, 0.5f); // Semi-transparent
    }

    void MovePreviewToMouse(){
    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    if (Physics.Raycast(ray, out RaycastHit hit))
    {
        previewBuilding.transform.position = hit.point;

        // Vérifie si la zone est libre
        bool canPlace = !Physics.CheckSphere(hit.point, 1f); 

        Color color = canPlace ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
        previewBuilding.GetComponent<Renderer>().material.color = color;
    }
}


    void PlaceBuilding()
    {
        Instantiate(buildingToPlace, previewBuilding.transform.position, Quaternion.identity);
        Destroy(previewBuilding);
        buildingToPlace = null;
    }

    bool IsPointerOverUI()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }
}
