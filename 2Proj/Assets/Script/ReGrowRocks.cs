using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GestionDesRochers : MonoBehaviour
{
    public GameObject prefabSouchePierre; // Prefab de souche (ou "pierre vide")
    public float tempsRepousse = 45f; // Temps avant repousse

    private Dictionary<Transform, bool> rochersEtats = new Dictionary<Transform, bool>();
    private Dictionary<Transform, GameObject> souchesPierres = new Dictionary<Transform, GameObject>();

    void Start()
    {
        // Ajoute tous les enfants (rochers) dans le dictionnaire
        foreach (Transform rocher in transform)
        {
            rochersEtats.Add(rocher, false);
        }
    }

    public void CasserRocher(Transform rocher)
    {
        if (rochersEtats.ContainsKey(rocher) && !rochersEtats[rocher])
        {
            rochersEtats[rocher] = true;

            // Cache le rocher
            rocher.gameObject.SetActive(false);

            // Instancie la "souce de pierre" (pierre vide) à la position et rotation du rocher
            GameObject souchePierre = Instantiate(prefabSouchePierre, rocher.position, rocher.rotation, transform);

            // 🔥 Change le Sorting Layer pour être visible
            SpriteRenderer sr = souchePierre.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingLayerName = "Terrain"; // Remplace "Terrain" par le layer correct
            }
            foreach (SpriteRenderer childSr in souchePierre.GetComponentsInChildren<SpriteRenderer>())
            {
                childSr.sortingLayerName = "Terrain";
            }

            // Ajoute dans le dictionnaire
            souchesPierres.Add(rocher, souchePierre);

            // Lance la coroutine de repousse
            StartCoroutine(RepousseRocher(rocher));
        }
    }

    private IEnumerator RepousseRocher(Transform rocher)
    {
        yield return new WaitForSeconds(tempsRepousse);

        // Supprime la souche de pierre
        if (souchesPierres.ContainsKey(rocher))
        {
            Destroy(souchesPierres[rocher]);
            souchesPierres.Remove(rocher);
        }

        // Réactive le rocher
        rocher.gameObject.SetActive(true);
        rochersEtats[rocher] = false;
    }
}