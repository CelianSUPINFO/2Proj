using UnityEngine;
using UnityEngine.Tilemaps;

public class PlacementPreviewManager : MonoBehaviour
{
    public Tilemap previewMap;
    public TileBase greenTile;
    public TileBase redTile;

    // Par défaut un bâtiment de 2x2 (tu peux l'adapter dynamiquement)
    public Vector3Int[] occupiedCells = new Vector3Int[]
    {
        new Vector3Int(0, 0, 0),
        new Vector3Int(1, 0, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(1, 1, 0),
    };

    public bool CanPlace(Vector3 worldPosition)
    {
        previewMap.ClearAllTiles();
        Vector3Int origin = previewMap.WorldToCell(worldPosition);

        bool canPlace = true;

        foreach (var offset in occupiedCells)
        {
            Vector3Int cell = origin + offset;
            Vector3 cellWorld = previewMap.GetCellCenterWorld(cell);
            Collider2D hit = Physics2D.OverlapBox(cellWorld, Vector2.one * 0.9f, 0f);

            if (hit != null)
            {
                previewMap.SetTile(cell, redTile);
                canPlace = false;
            }
            else
            {
                previewMap.SetTile(cell, greenTile);
            }
        }

        return canPlace;
    }

    public void ClearPreview()
    {
        previewMap.ClearAllTiles();
    }
}
