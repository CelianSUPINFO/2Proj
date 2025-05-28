using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Composant à ajouter aux bâtiments de type "Maison" pour gérer le spawn automatique des personnages
/// </summary>
public class HouseSpawner : MonoBehaviour
{
    [Header("Configuration de spawn")]
    private bool estActif = false; // ❌ désactivé tant que le bâtiment n’est pas posé

    public GameObject personnagePrefab;
    public int nombrePersonnagesMax = 2;
    public float delaiRespawn = 30f; // 30 secondes
    
    [Header("Zone de spawn")]
    public float rayonSpawn = 1.5f; // Rayon autour de la maison pour spawner
    public LayerMask layerSol; // Layer du sol pour vérifier les positions valides
    
    [Header("Debug")]
    public bool afficherDebug = true;
    
    // Liste des personnages actuellement vivants liés à cette maison
    private List<PersonnageData> personnagesVivants = new List<PersonnageData>();
    
    // Coroutines de respawn en cours
    private List<Coroutine> coroutinesRespawn = new List<Coroutine>();

    void Start()
    {
        if (!estActif) return; // 🔒 Bloque le comportement si pas activé par le placer

        if (personnagePrefab == null)
        {
            Debug.LogError($"[HouseSpawner] {name} : personnagePrefab non assigné !");
            return;
        }

        if (layerSol == 0)
        {
            Debug.LogWarning($"[HouseSpawner] {name} : layerSol non défini, utilisation du layer 'Ground'");
            layerSol = LayerMask.GetMask("Ground");
        }

        StartCoroutine(SpawnPersonnagesInitiaux());
    }
    
    public void Activer()
    {
        if (estActif) return; // Évite de réactiver plusieurs fois

        estActif = true;
        StartCoroutine(SpawnPersonnagesInitiaux());
    }



    
    /// <summary>
    /// Spawn les personnages au début (avec un petit délai pour laisser le temps à Unity de s'initialiser)
    /// </summary>
    private bool spawnDejaLance = false;

    private IEnumerator SpawnPersonnagesInitiaux()
    {
        if (spawnDejaLance) yield break;
        spawnDejaLance = true;

        yield return new WaitForSeconds(0.1f);

        for (int i = 0; i < nombrePersonnagesMax; i++)
        {
            SpawnPersonnage();
            yield return new WaitForSeconds(0.1f);
        }

        if (afficherDebug)
            Debug.Log($"[HouseSpawner] {name} : {nombrePersonnagesMax} personnages spawnés initialement");
    }

    
    /// <summary>
    /// Spawne un nouveau personnage à une position valide autour de la maison
    /// </summary>
    private void SpawnPersonnage()
    {
        Vector3 positionSpawn = TrouverPositionSpawnValide();
        
        if (positionSpawn == Vector3.zero)
        {
            Debug.LogWarning($"[HouseSpawner] {name} : Impossible de trouver une position de spawn valide !");
            return;
        }
        
        // Créer le personnage
        GameObject nouveauPersonnage = Instantiate(personnagePrefab, positionSpawn, Quaternion.identity);
        PersonnageData personnageData = nouveauPersonnage.GetComponent<PersonnageData>();
        
        if (personnageData != null)
        {
            // Ajouter à notre liste de personnages vivants
            personnagesVivants.Add(personnageData);
            
            // S'abonner à l'événement de mort (nous devrons modifier PersonnageData pour cela)
            StartCoroutine(SurveillerPersonnage(personnageData));
            
            if (afficherDebug)
                Debug.Log($"[HouseSpawner] {name} : Nouveau personnage spawné - {personnageData.name}");
        }
        else
        {
            Debug.LogError($"[HouseSpawner] Le prefab {personnagePrefab.name} n'a pas de composant PersonnageData !");
            Destroy(nouveauPersonnage);
        }
    }
    
    /// <summary>
    /// Surveille un personnage et détecte sa mort
    /// </summary>
    private IEnumerator SurveillerPersonnage(PersonnageData personnage)
    {
        string nomMemo = personnage != null ? personnage.name : "Unknown";

        while (personnage != null && personnage.gameObject != null)
        {
            yield return new WaitForSeconds(0.5f);
        }

        // Appeler avec juste le nom, ne pas accéder à l'objet détruit
        OnPersonnageMort(nomMemo);
    }

  
  
    private void OnPersonnageMort(string nomPersonnage)
    {
        // Nettoyer la liste des vivants (si jamais la référence existe encore)
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

        // Nettoyer la liste des coroutines terminées
        coroutinesRespawn.RemoveAll(c => c == null); // supprime les nulls si la coroutine est finie
    }

    
    /// <summary>
    /// Trouve une position valide pour spawner un personnage autour de la maison
    /// </summary>
    private Vector3 TrouverPositionSpawnValide()
    {
        int tentatives = 50;
        Vector3 centreSpawn = transform.position;
        
        for (int i = 0; i < tentatives; i++)
        {
            // Générer une position aléatoire dans un cercle autour de la maison
            Vector2 directionAleatoire = Random.insideUnitCircle.normalized;
            float distanceAleatoire = Random.Range(0.5f, rayonSpawn);
            
            Vector3 positionTest = centreSpawn + (Vector3)(directionAleatoire * distanceAleatoire);
            positionTest.z = 0f; // S'assurer que Z = 0 pour la 2D
            
            // Vérifier que la position est sur le sol
            if (Physics2D.OverlapCircle(positionTest, 0.1f, layerSol))
            {
                // Vérifier qu'il n'y a pas d'obstacle (bâtiment, autre personnage, etc.)
                Collider2D obstacle = Physics2D.OverlapCircle(positionTest, 0.3f, ~layerSol);
                if (obstacle == null)
                {
                    return positionTest;
                }
            }
        }
        
        // Si aucune position trouvée, retourner la position de la maison elle-même
        Debug.LogWarning($"[HouseSpawner] {name} : Aucune position de spawn trouvée, utilisation de la position de la maison");
        return centreSpawn;
    }
    
    /// <summary>
    /// Méthode publique pour forcer un respawn (utile pour le debug ou des événements spéciaux)
    /// </summary>
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
    
    /// <summary>
    /// Nettoie les coroutines quand l'objet est détruit
    /// </summary>
    private void OnDestroy()
    {
        // Arrêter toutes les coroutines de respawn
        foreach (Coroutine coroutine in coroutinesRespawn)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }
        coroutinesRespawn.Clear();
    }
    
    /// <summary>
    /// Dessine la zone de spawn dans l'éditeur (pour le debug)
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            // Zone de spawn
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, rayonSpawn);
            
            // Positions des personnages vivants
            Gizmos.color = Color.blue;
            foreach (PersonnageData personnage in personnagesVivants)
            {
                if (personnage != null)
                {
                    Gizmos.DrawLine(transform.position, personnage.transform.position);
                }
            }
        }
        else
        {
            // En mode éditeur, juste montrer la zone de spawn
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, rayonSpawn);
        }
    }
    
    /// <summary>
    /// Informations de debug
    /// </summary>
    public void AfficherInfos()
    {
        Debug.Log($"[HouseSpawner] {name} :");
        Debug.Log($"  - Personnages vivants: {personnagesVivants.Count}/{nombrePersonnagesMax}");
        Debug.Log($"  - Coroutines de respawn actives: {coroutinesRespawn.Count}");
        
        for (int i = 0; i < personnagesVivants.Count; i++)
        {
            if (personnagesVivants[i] != null)
                Debug.Log($"    {i + 1}. {personnagesVivants[i].name}");
            else
                Debug.Log($"    {i + 1}. [MORT/NULL]");
        }
    }
}