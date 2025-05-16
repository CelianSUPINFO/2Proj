using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TechTree/TechTreeData")]
public class TechTreeData : ScriptableObject
{
    public List<TechNode> techNodes;
}
