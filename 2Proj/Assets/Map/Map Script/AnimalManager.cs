using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class AnimalSpawner : MonoBehaviour
{
    public Tilemap groundTilemap; // Drag ton Tilemap ici dans l'inspecteur
    public GameObject animalPrefab; // Ton prefab animal
    public int spawnCount = 10; // Nombre d'animaux à faire spawn

    private List<Vector3> groundPositions = new List<Vector3>();


    void Start()
    {
        // Parcourir toutes les positions de la Tilemap "Ground"
        BoundsInt bounds = groundTilemap.cellBounds;
        TileBase[] allTiles = groundTilemap.GetTilesBlock(bounds);

        for (int x = 0; x < bounds.size.x; x++)
        {
            for (int y = 0; y < bounds.size.y; y++)
            {
                TileBase tile = allTiles[x + y * bounds.size.x];
                if (tile != null)
                {
                    Vector3Int cellPosition = new Vector3Int(x + bounds.x, y + bounds.y, 0);
                    Vector3 worldPosition = groundTilemap.CellToWorld(cellPosition) + groundTilemap.cellSize / 2;
                    groundPositions.Add(worldPosition);
                }
            }
        }

        // Spawn les animaux à des positions aléatoires
        for (int i = 0; i < spawnCount; i++)
        {
            if (groundPositions.Count == 0) break;

            Vector3 spawnPos = groundPositions[Random.Range(0, groundPositions.Count)];
            Instantiate(animalPrefab, spawnPos, Quaternion.identity);
        }
    }
}


