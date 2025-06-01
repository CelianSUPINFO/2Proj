using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Ce script permet de faire apparaître automatiquement des personnages autour d’une maison.
// Il gère aussi le respawn s’ils meurent.

public class HouseSpawner : MonoBehaviour
{
    [Header("Configuration de spawn")]
    private bool estActif = false; // Est-ce que le système de spawn est activé ?
    public GameObject personnagePrefab; // Le prefab du personnage à instancier
    public int nombrePersonnagesMax = 2; // Nombre max de personnages autour de la maison
    public float delaiRespawn = 30f; // Temps avant de respawn un personnage mort

    [Header("Zone de spawn")]
    public float rayonSpawn = 1.5f; // Rayon dans lequel les personnages vont apparaître
    public LayerMask layerSol; // Le sol sur lequel ils doivent apparaître

    [Header("Debug")]
    public bool afficherDebug = true; // Pour afficher des messages dans la console

    private List<PersonnageData> personnagesVivants = new(); // Liste des personnages vivants
    private List<Coroutine> coroutinesRespawn = new(); // Liste des coroutines de respawn actives

    private bool spawnDejaLance = false; // Pour ne pas lancer plusieurs fois le spawn initial

    void Start()
    {
        if (!estActif) return; // Si désactivé, on ne fait rien

        if (personnagePrefab == null)
        {
            Debug.LogError($"[HouseSpawner] {name} : personnagePrefab non assigné !");
            return;
        }

        // Si aucun layerSol n'est défini, on utilise le layer "Ground"
        if (layerSol == 0)
        {
            layerSol = LayerMask.GetMask("Ground");
        }

        StartCoroutine(SpawnPersonnagesInitiaux()); // On lance le spawn initial
    }

    // Méthode appelée pour activer le spawner manuellement
    public void Activer()
    {
        if (estActif) return;

        estActif = true;
        StartCoroutine(SpawnPersonnagesInitiaux());
    }

    // Coroutine pour faire apparaître les personnages au démarrage
    private IEnumerator SpawnPersonnagesInitiaux()
    {
        if (spawnDejaLance) yield break;
        spawnDejaLance = true;

        yield return new WaitForSeconds(0.1f); // Petite pause avant de commencer

        for (int i = 0; i < nombrePersonnagesMax; i++)
        {
            SpawnPersonnage(); // On en fait apparaître un
            ResourceManager.Instance.Add(ResourceType.Search, 1); // Bonus de recherche
            yield return new WaitForSeconds(0.1f); // Pause entre chaque spawn
        }

        if (afficherDebug)
            Debug.Log($"[HouseSpawner] {name} : {nombrePersonnagesMax} personnages spawnés initialement");
    }

    // Fonction qui crée un nouveau personnage autour de la maison
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
            personnagesVivants.Add(personnageData); // On l’ajoute à la liste des vivants
            StartCoroutine(SurveillerPersonnage(personnageData)); // On surveille s’il meurt

            if (afficherDebug)
                Debug.Log($"[HouseSpawner] {name} : Nouveau personnage spawné - {personnageData.name}");
        }
        else
        {
            Debug.LogError($"[HouseSpawner] Le prefab {personnagePrefab.name} n'a pas de composant PersonnageData !");
            Destroy(nouveauPersonnage); // On supprime si pas le bon script
        }
    }

    // Coroutine qui attend que le personnage meurt
    private IEnumerator SurveillerPersonnage(PersonnageData personnage)
    {
        string nomMemo = personnage != null ? personnage.name : "Unknown";

        while (personnage != null && personnage.gameObject != null)
        {
            yield return new WaitForSeconds(0.5f);
        }

        OnPersonnageMort(nomMemo); // Quand il disparaît, on lance la suite
    }

    // Quand un personnage meurt, on prévoit son respawn
    private void OnPersonnageMort(string nomPersonnage)
    {
        personnagesVivants.RemoveAll(p => p == null); // Nettoyage de la liste

        if (afficherDebug)
            Debug.Log($"[HouseSpawner] {name} : Personnage mort détecté - {nomPersonnage}. Respawn dans {delaiRespawn}s");

        Coroutine coroutine = StartCoroutine(CoroutineRespawn());
        coroutinesRespawn.Add(coroutine);
    }

    // Coroutine qui attend X secondes avant de respawn un personnage
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

    // Cherche une position valide autour de la maison pour faire apparaître un personnage
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

            // Vérifie si la position est sur le sol et sans obstacle
            if (Physics2D.OverlapCircle(positionTest, 0.1f, layerSol))
            {
                Collider2D obstacle = Physics2D.OverlapCircle(positionTest, 0.3f, ~layerSol);
                if (obstacle == null)
                {
                    return positionTest;
                }
            }
        }

        // Si rien trouvé, on utilise le centre de la maison
        Debug.LogWarning($"[HouseSpawner] {name} : Aucune position de spawn trouvée, utilisation de la position de la maison");
        return centreSpawn;
    }

    // Permet de forcer l'apparition d’un personnage (ex : debug)
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

    // Nettoie toutes les coroutines en cours quand le bâtiment est détruit
    private void OnDestroy()
    {
        foreach (Coroutine coroutine in coroutinesRespawn)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }
        coroutinesRespawn.Clear();
    }

    // Affiche dans l'éditeur Unity un cercle jaune pour voir la zone de spawn
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rayonSpawn);
    }

    // Méthode pour afficher les infos du spawner dans la console (utile pour le debug)
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
