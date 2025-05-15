using UnityEngine;

public class PersonnageSpawner : MonoBehaviour
{
    public GameObject[] personnagesPrefabParAge; // 1 prefab par époque
    public Transform spawnZone;
    public float spawnInterval = 60f;

    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnPersonnage();
        }
    }

    void SpawnPersonnage()
    {
        GameAge currentAge = AgeManager.Instance.GetCurrentAge();
        GameObject prefab = personnagesPrefabParAge[(int)currentAge];

        Vector3 pos = spawnZone != null 
            ? spawnZone.position + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0)
            : Vector3.zero;

        Instantiate(prefab, pos, Quaternion.identity);
    }
}
