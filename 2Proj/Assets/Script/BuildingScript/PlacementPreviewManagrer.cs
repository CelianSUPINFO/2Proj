using UnityEngine;
using UnityEngine.Tilemaps;


// Gère l'affichage d'une prévisualisation de placement sur une Tilemap,
// en montrant des cases vertes (OK) ou rouges (bloqué).
public class PlacementPreviewManager : MonoBehaviour
{
    [Header("Références Tilemap")]
    public Tilemap previewMap;       // La tilemap dédiée à l’aperçu
    public TileBase greenTile;       // Tuile verte si l’emplacement est libre
    public TileBase redTile;         // Tuile rouge si occupé

    [Header("Forme de l’objet à placer")]
    // Par défaut : un bâtiment de 2x2 cellules (modifiable selon les bâtiments)
    public Vector3Int[] occupiedCells = new Vector3Int[]
    {
        new Vector3Int(0, 0, 0),
        new Vector3Int(1, 0, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(1, 1, 0),
    };

 
    // Vérifie si on peut placer un bâtiment à la position du monde donnée.
    // Met à jour l’aperçu visuel (vert/rouge) en conséquence.
    public bool CanPlace(Vector3 worldPosition)
    {
        previewMap.ClearAllTiles(); // Réinitialise l’aperçu précédent

        Vector3Int origin = previewMap.WorldToCell(worldPosition); // Convertit en cellule grid

        bool canPlace = true;

        // Vérifie chaque cellule du bâtiment à partir de l’origine
        foreach (var offset in occupiedCells)
        {
            Vector3Int cell = origin + offset; // Calcule la position de la cellule à tester
            Vector3 cellWorld = previewMap.GetCellCenterWorld(cell); // Centre du monde réel de cette cellule

            // Teste s'il y a déjà un collider dans cette cellule
            Collider2D hit = Physics2D.OverlapBox(cellWorld, Vector2.one * 0.9f, 0f);

            if (hit != null)
            {
                previewMap.SetTile(cell, redTile); // Cellule occupée → rouge
                canPlace = false;
            }
            else
            {
                previewMap.SetTile(cell, greenTile); // Cellule libre → verte
            }
        }

        return canPlace;
    }

    
    // Nettoie tous les aperçus (retire les tuiles vertes/rouges).
    // À appeler après le placement ou l’annulation.
    public void ClearPreview()
    {
        previewMap.ClearAllTiles();
    }
}
