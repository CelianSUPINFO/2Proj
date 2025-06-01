using UnityEngine;

// ScriptableObject contenant les infos d’un chemin (préfab + âge requis)
[CreateAssetMenu(menuName = "Paths/PathData")]
public class PathData : ScriptableObject
{
    public GameAge age;           // Âge requis pour débloquer ce chemin
    public GameObject prefab;    // Le préfab à placer quand on construit le chemin
}

