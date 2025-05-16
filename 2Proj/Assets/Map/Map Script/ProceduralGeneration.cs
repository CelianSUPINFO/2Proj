using System.Data;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ProceduralGeneration : MonoBehaviour
{
    public int width = 100;
    public int height = 100;
    public float scale = 20f;
    public float islandFactor = 2f; 

    public Tilemap Tilemap;
    public TileBase waterTile;
    public TileBase landTile;

    void Start()
    {
        GenerateIsland();
    }

    void GenerateIsland()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float xCoord = (float)x / width * scale;
                float yCoord = (float)y / height * scale;
                float noise = Mathf.PerlinNoise(xCoord, yCoord);

                
                float distanceToCenter = Vector2.Distance(new Vector2(x, y), new Vector2(width / 2, height / 2)) / (width / islandFactor);
                noise -= distanceToCenter;

                if (noise > 0.2f)
                    Tilemap.SetTile(new Vector3Int(x, y, 0), landTile);
                else
                    Tilemap.SetTile(new Vector3Int(x, y, 0), waterTile);
            }
        }
    }
}


