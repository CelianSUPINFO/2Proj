using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[System.Serializable]
public class CloudPattern
{
    public List<TileBase> tiles = new List<TileBase>();  // Exemple : [left, mid, right]
    public int Width => tiles.Count;
}