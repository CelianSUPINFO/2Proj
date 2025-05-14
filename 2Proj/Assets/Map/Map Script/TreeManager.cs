using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class TreePlacerEditor : EditorWindow
{
    private Tilemap tilemap;
    private TilemapRenderer tilemapRenderer;

    private List<TileBase> tiles = new List<TileBase>();
    private List<GameObject> prefabs = new List<GameObject>();

    [MenuItem("Tools/Tree Placer")]
    public static void ShowWindow()
    {
        GetWindow<TreePlacerEditor>("Tree Placer");
    }

    void OnGUI()
    {
        GUILayout.Label("Tree Placer Tool", EditorStyles.boldLabel);

        tilemap = (Tilemap)EditorGUILayout.ObjectField("Tilemap", tilemap, typeof(Tilemap), true);
        tilemapRenderer = (TilemapRenderer)EditorGUILayout.ObjectField("Tilemap Renderer", tilemapRenderer, typeof(TilemapRenderer), true);

        int count = Mathf.Max(1, EditorGUILayout.IntField("Types d'arbres", tiles.Count));
        while (tiles.Count < count) { tiles.Add(null); prefabs.Add(null); }
        while (tiles.Count > count) { tiles.RemoveAt(tiles.Count - 1); prefabs.RemoveAt(prefabs.Count - 1); }

        for (int i = 0; i < count; i++)
        {
            tiles[i] = (TileBase)EditorGUILayout.ObjectField($"Tile {i + 1}", tiles[i], typeof(TileBase), false);
            prefabs[i] = (GameObject)EditorGUILayout.ObjectField($"Prefab {i + 1}", prefabs[i], typeof(GameObject), false);
        }

        if (GUILayout.Button("Remplacer les tiles par des arbres"))
        {
            ReplaceTilesWithPrefabs();
        }
    }

    void ReplaceTilesWithPrefabs()
    {
        if (!tilemap || !tilemapRenderer) return;

        Undo.RegisterFullObjectHierarchyUndo(tilemap.gameObject, "Place Trees");

       
        GameObject parent = GameObject.Find("Trees");
        if (parent == null)
        {
            parent = new GameObject("Trees");
            Undo.RegisterCreatedObjectUndo(parent, "Create Trees Group");
        }

        BoundsInt bounds = tilemap.cellBounds;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cellPos = new Vector3Int(x, y, 0);
                TileBase currentTile = tilemap.GetTile(cellPos);

                if (currentTile == null) continue;

                for (int i = 0; i < tiles.Count; i++)
                {
                    if (tiles[i] == currentTile && prefabs[i] != null)
                    {
                        Vector3 worldPos = tilemap.CellToWorld(cellPos) + new Vector3(0.5f, 0.5f, 0);
                        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[i]);
                        go.transform.position = worldPos;
                        go.transform.SetParent(parent.transform); 
                        Undo.RegisterCreatedObjectUndo(go, "Place Tree");

                     
                        foreach (var renderer in go.GetComponentsInChildren<SpriteRenderer>())
                        {
                            renderer.sortingLayerID = tilemapRenderer.sortingLayerID;
                            renderer.sortingOrder = -(cellPos.y * 10); 
                        }

                        break;
                    }
                }
            }
        }

        tilemap.ClearAllTiles();
        Debug.Log("Tous les arbres ont été placés sous le GameObject 'Trees'.");
    }
}
