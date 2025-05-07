using UnityEngine;

public class BuildingEraser : MonoBehaviour
{
    [Header("Références")]
    public LayerMask buildingLayer; // 🧱 Layer des bâtiments à supprimer

    public bool eraseMode = false;

    void Update()
    {
        if (eraseMode == false){
            Debug.Log("nan");
            return;
        } 

        HighlightHoveredBuilding();

        // 🖱️ Clic gauche = supprimer
        if (Input.GetMouseButtonDown(0))
        {
            TryEraseBuilding();
        }

        // 🖱️ Clic droit = désactiver le mode suppression
        if (Input.GetMouseButtonDown(1))
        {
            SetEraseMode(false);
        }
    }

    /// <summary>
    /// Active ou désactive le mode suppression via un bouton UI
    /// </summary>
    public void ToggleEraseMode()
    {   
        // return eraseMode = !eraseMode;
        eraseMode = !eraseMode;
        Debug.Log(eraseMode ? "🧽 Mode suppression activé" : "🧼 Mode suppression désactivé");

    }

    /// <summary>
    /// Active ou désactive le mode suppression manuellement
    /// </summary>
    void SetEraseMode(bool active)
    {
        eraseMode = active;
        Debug.Log(eraseMode ? "🧽 Mode suppression activé" : "🧼 Mode suppression désactivé");
    }

    /// <summary>
    /// Retourne true si le mode suppression est actif
    /// </summary>
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
            Debug.Log("🗑️ Bâtiment supprimé !");
        }
        else
        {
            Debug.Log("❌ Aucun bâtiment ici à supprimer.");
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
                sr.color = new Color(1f, 0.5f, 0.5f, 1f); // couleur rouge clair
            }
        }
    }
}
