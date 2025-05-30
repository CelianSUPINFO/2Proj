using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HouseSpawner : MonoBehaviour
{
    [Header("Configuration de spawn")]
    private bool estActif = false;

    public GameObject personnagePrefab;
    public int nombrePersonnagesMax = 2;
    public float delaiRespawn = 30f;

    [Header("Zone de spawn")]
    public float rayonSpawn = 1.5f;
    public LayerMask layerSol;

    [Header("Debug")]
    public bool afficherDebug = true;

    private List<PersonnageData> personnagesVivants = new List<PersonnageData>();
    private List<Coroutine> coroutinesRespawn = new List<Coroutine>();

    private bool spawnDejaLance = false;

    void Start()
    {
        if (!estActif) return;

        if (personnagePrefab == null)
        {
            Debug.LogError($"[HouseSpawner] {name} : personnagePrefab non assigné !");
            return;
        }

        if (layerSol == 0)
        {
            layerSol = LayerMask.GetMask("Ground");
        }

        StartCoroutine(SpawnPersonnagesInitiaux());
    }

    public void Activer()
    {
        if (estActif) return;

        estActif = true;
        StartCoroutine(SpawnPersonnagesInitiaux());
    }

    private IEnumerator SpawnPersonnagesInitiaux()
    {
        if (spawnDejaLance) yield break;
        spawnDejaLance = true;

        yield return new WaitForSeconds(0.1f);

        for (int i = 0; i < nombrePersonnagesMax; i++)
        {
            SpawnPersonnage();
            ResourceManager.Instance.Add(ResourceType.Search, 1);
            yield return new WaitForSeconds(0.1f);
        }

        if (afficherDebug)
            Debug.Log($"[HouseSpawner] {name} : {nombrePersonnagesMax} personnages spawnés initialement");
    }

    private void SpawnPersonnage()
    {
        Vector3 positionSpawn = TrouverPositionSpawnValide();

        if (positionSpawn == Vector3.zero)
        {
            Debug.LogWarning($"[HouseSpawner] {name} : Impossible de trouver une position de spawn valide !");
            return;
        }

        GameObject nouveauPersonnage = Instantiate(personnagePrefab, positionSpawn, Quaternion.identity);
        PersonnageData personnageData = nouveauPersonnage.GetComponent<PersonnageData>();

        if (personnageData != null)
        {
            personnagesVivants.Add(personnageData);
            StartCoroutine(SurveillerPersonnage(personnageData));
            // MetierAssignmentManager.Instance.TrouverDuJob(personnageData);



            if (afficherDebug)
                Debug.Log($"[HouseSpawner] {name} : Nouveau personnage spawné - {personnageData.name}");
        }
        else
        {
            Debug.LogError($"[HouseSpawner] Le prefab {personnagePrefab.name} n'a pas de composant PersonnageData !");
            Destroy(nouveauPersonnage);
        }
    }

    private IEnumerator SurveillerPersonnage(PersonnageData personnage)
    {
        string nomMemo = personnage != null ? personnage.name : "Unknown";

        while (personnage != null && personnage.gameObject != null)
        {
            yield return new WaitForSeconds(0.5f);
        }

        OnPersonnageMort(nomMemo);
    }

    private void OnPersonnageMort(string nomPersonnage)
    {
        personnagesVivants.RemoveAll(p => p == null);

        if (afficherDebug)
            Debug.Log($"[HouseSpawner] {name} : Personnage mort détecté - {nomPersonnage}. Respawn dans {delaiRespawn}s");

        Coroutine coroutine = StartCoroutine(CoroutineRespawn());
        coroutinesRespawn.Add(coroutine);
    }

    private IEnumerator CoroutineRespawn()
    {
        yield return new WaitForSeconds(delaiRespawn);

        if (personnagesVivants.Count < nombrePersonnagesMax)
        {
            SpawnPersonnage();

            if (afficherDebug)
                Debug.Log($"[HouseSpawner] {name} : Respawn effectué après {delaiRespawn}s");
        }

        coroutinesRespawn.RemoveAll(c => c == null);
    }

    private Vector3 TrouverPositionSpawnValide()
    {
        int tentatives = 50;
        Vector3 centreSpawn = transform.position;

        for (int i = 0; i < tentatives; i++)
        {
            Vector2 directionAleatoire = Random.insideUnitCircle.normalized;
            float distanceAleatoire = Random.Range(0.5f, rayonSpawn);

            Vector3 positionTest = centreSpawn + (Vector3)(directionAleatoire * distanceAleatoire);
            positionTest.z = 0f;

            if (Physics2D.OverlapCircle(positionTest, 0.1f, layerSol))
            {
                Collider2D obstacle = Physics2D.OverlapCircle(positionTest, 0.3f, ~layerSol);
                if (obstacle == null)
                {
                    return positionTest;
                }
            }
        }

        Debug.LogWarning($"[HouseSpawner] {name} : Aucune position de spawn trouvée, utilisation de la position de la maison");
        return centreSpawn;
    }

    public void ForcerRespawn()
    {
        if (personnagesVivants.Count < nombrePersonnagesMax)
        {
            SpawnPersonnage();
        }
        else
        {
            Debug.Log($"[HouseSpawner] {name} : Nombre maximum de personnages déjà atteint ({nombrePersonnagesMax})");
        }
    }

    private void OnDestroy()
    {
        foreach (Coroutine coroutine in coroutinesRespawn)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }
        coroutinesRespawn.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rayonSpawn);
    }

    public void AfficherInfos()
    {
        Debug.Log($"[HouseSpawner] {name} :");
        Debug.Log($"  - Personnages vivants: {personnagesVivants.Count}/{nombrePersonnagesMax}");
        Debug.Log($"  - Coroutines de respawn actives: {coroutinesRespawn.Count}");

        for (int i = 0; i < personnagesVivants.Count; i++)
        {
            if (personnagesVivants[i] != null)
                Debug.Log($"    {i + 1}. {personnagesVivants[i].name} - {personnagesVivants[i].metier}");
            else
                Debug.Log($"    {i + 1}. [MORT/NULL]");
        }
    }
}
