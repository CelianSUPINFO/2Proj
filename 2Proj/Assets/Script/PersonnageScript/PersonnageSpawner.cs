using UnityEngine;
using System.Collections.Generic;

public class SpawnManager : MonoBehaviour
{
    public GameObject personnagePrefab;
    public List<Vector3> positionsDeSpawn = new List<Vector3>();
    public LayerMask layerSol;

    void Start()
    {
        foreach (Vector3 pos in positionsDeSpawn)
        {
            // Vérifie que le sol est bien là
            if (Physics2D.OverlapCircle(pos, 0.1f, layerSol))
            {
                Instantiate(personnagePrefab, pos, Quaternion.identity);
            }
            else
            {
                Debug.LogWarning($" Pas de sol à la position {pos}, aucun perso généré ici.");
            }
        }
    }
}
