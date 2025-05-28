using UnityEngine;

public class SolTesteur : MonoBehaviour
{
    public LayerMask layerSol;
    public float rayonTest = 0.1f;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Input.mousePosition;

            // Ce Z corrige la distance entre la caméra (Z -10) et la scène (Z 0)
            mousePos.z = Mathf.Abs(Camera.main.transform.position.z);

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            worldPos.z = 0;

            Collider2D hit = Physics2D.OverlapCircle(worldPos, 0.1f, layerSol);
            Debug.DrawLine(worldPos + Vector3.up * 0.2f, worldPos + Vector3.down * 0.2f, Color.green, 2f);

            if (hit != null)
            {
                Debug.Log($"✅ SOL détecté à {worldPos}, sur {hit.gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"❌ PAS de sol à {worldPos}");
            }
        }
    }

}
