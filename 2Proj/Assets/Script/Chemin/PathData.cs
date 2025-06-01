using UnityEngine;

[CreateAssetMenu(menuName = "Paths/PathData")]
public class PathData : ScriptableObject
{
    public GameAge age;              // âge requis pour utiliser ce chemin
    public GameObject prefab;       // prefab à placer
}
