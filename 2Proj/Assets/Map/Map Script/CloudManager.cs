using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[ExecuteInEditMode]
public class CloudBorderRing : MonoBehaviour
{
    [Header("Tilemap de destination")]
    public Tilemap targetTilemap;

    [Header("Nuages disponibles")]
    public List<CloudPattern> cloudPatterns;

    [Header("Centre de la map")]
    public Vector2Int center = Vector2Int.zero;

    [Header("Rayons de placement")]
    public float innerRadius = 15f;
    public float outerRadius = 30f;

    [Header("Placement")]
    [Range(0f, 1f)] public float density = 0.7f;

    private HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

    [ContextMenu("Générer le contour de nuages")]
    public void GenerateCloudRing()
    {
        if (targetTilemap == null || cloudPatterns == null || cloudPatterns.Count == 0)
        {
            Debug.LogWarning("Tilemap ou patterns de nuages manquants");
            return;
        }

        targetTilemap.ClearAllTiles();
        occupied.Clear();

        int rMax = Mathf.CeilToInt(outerRadius);

        for (int x = -rMax; x <= rMax; x++)
        {
            for (int y = -rMax; y <= rMax; y++)
            {
                Vector2 offset = new Vector2(x, y);
                float dist = offset.magnitude;

                if (dist >= innerRadius && dist <= outerRadius)
                {
                    if (Random.value > density) continue; // appliquer la densité

                    Vector2Int position = new Vector2Int(center.x + x, center.y + y);
                    CloudPattern pattern = cloudPatterns[Random.Range(0, cloudPatterns.Count)];

                    if (CanPlace(pattern, position))
                    {
                        PlacePattern(pattern, position);
                    }
                }
            }
        }

        Debug.Log("Contour de nuages généré");
    }

    bool CanPlace(CloudPattern pattern, Vector2Int centerPos)
    {
        int halfWidth = pattern.Width / 2;

        for (int i = 0; i < pattern.Width; i++)
        {
            int xOffset = i - halfWidth;
            if (pattern.Width % 2 == 0) xOffset++; // ajustement pour largeur paire

            Vector2Int pos = new Vector2Int(centerPos.x + xOffset, centerPos.y);
            if (occupied.Contains(pos))
                return false;
        }

        return true;
    }

    void PlacePattern(CloudPattern pattern, Vector2Int centerPos)
    {
        int halfWidth = pattern.Width / 2;

        for (int i = 0; i < pattern.Width; i++)
        {
            int xOffset = i - halfWidth;
            if (pattern.Width % 2 == 0) xOffset++;

            Vector3Int tilePos = new Vector3Int(centerPos.x + xOffset, centerPos.y, 0);
            targetTilemap.SetTile(tilePos, pattern.tiles[i]);
            occupied.Add(new Vector2Int(tilePos.x, tilePos.y));
        }
    }
}