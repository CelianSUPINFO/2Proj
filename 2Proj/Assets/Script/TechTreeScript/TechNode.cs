using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TechNode
{
    public string id;
    public string displayName;
    public Sprite icon;
    public int cost;
    public List<string> prerequisiteIds;
    public bool unlocked = false;
}
