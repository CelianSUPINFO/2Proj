using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GestionDesArbres : MonoBehaviour
{
    public GameObject prefabSouche; 
    public float tempsRepousse = 120f;
    private Dictionary<Transform, bool> arbresEtats = new Dictionary<Transform, bool>();
    private Dictionary<Transform, GameObject> souches = new Dictionary<Transform, GameObject>();

    void Start()
    {
        // Ajoute tous les enfants (arbres) dans le dictionnaire
        foreach (Transform arbre in transform)
        {
            arbresEtats.Add(arbre, false);
        }
    }

    public void CouperArbre(Transform arbre)
    {
        if (arbresEtats.ContainsKey(arbre) && !arbresEtats[arbre])
        {
            arbresEtats[arbre] = true;

            // Cache l'arbre
            arbre.gameObject.SetActive(false);

            // Instancie la souche à la position et rotation de l'arbre
            GameObject souche = Instantiate(prefabSouche, arbre.position, arbre.rotation, transform);

            // 🔥 Correction : change le Sorting Layer de la souche
            SpriteRenderer sr = souche.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingLayerName = "Terrain"; // Remplace "Terrain" par le nom exact de ton Sorting Layer
            }

            // 🔥 Si la souche a des enfants (par ex : plusieurs sprites), change le Sorting Layer sur tous
            foreach (SpriteRenderer childSr in souche.GetComponentsInChildren<SpriteRenderer>())
            {
                childSr.sortingLayerName = "Terrain";
            }

            // Ajoute dans le dictionnaire
            souches.Add(arbre, souche);

            // Lance la coroutine de repousse
            StartCoroutine(RepousseArbre(arbre));
        }
    }

    private IEnumerator RepousseArbre(Transform arbre)
    {
        yield return new WaitForSeconds(tempsRepousse);

        // Supprime la souche
        if (souches.ContainsKey(arbre))
        {
            Destroy(souches[arbre]);
            souches.Remove(arbre);
        }

        // Réactive l'arbre
        arbre.gameObject.SetActive(true);
        arbresEtats[arbre] = false;
    }
}