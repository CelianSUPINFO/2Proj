using UnityEngine;

public class BuildingEraser : MonoBehaviour
{
    [Header("Références")]
    public LayerMask buildingLayer; 

    public bool eraseMode = false;

    void Update()
    {
        if (eraseMode == false){
            Debug.Log("nan");
            return;
        } 

        HighlightHoveredBuilding();

        if (Input.GetMouseButtonDown(0))
        {
            TryEraseBuilding();
        }

        if (Input.GetMouseButtonDown(1))
        {
            SetEraseMode(false);
        }
    }

    public void ToggleEraseMode()
    {   
      
        eraseMode = !eraseMode;
        Debug.Log(eraseMode ? "Mode suppression activé" : "Mode suppression désactivé");

    }


    void SetEraseMode(bool active)
    {
        eraseMode = active;
        Debug.Log(eraseMode ? "Mode suppression activé" : "Mode suppression désactivé");
    }

    
    public bool IsEraseModeActive()
    {
        return eraseMode;
    }

    private void TryEraseBuilding()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos, buildingLayer);

        if (hit != null)
        {
            Destroy(hit.gameObject);
            Debug.Log("Bâtiment supprimé !");
        }
        else
        {
            Debug.Log("Aucun bâtiment ici à supprimer.");
        }
    }

    private void HighlightHoveredBuilding()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos, buildingLayer);

        if (hit != null)
        {
            SpriteRenderer sr = hit.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = new Color(1f, 0.5f, 0.5f, 1f); 
            }
        }
    }
}
