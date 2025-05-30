using UnityEngine;

public class BateauController : MonoBehaviour
{
    public Vector3 destination;
    public float vitesse = 2f;

    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, destination, vitesse * Time.deltaTime);
        if (Vector3.Distance(transform.position, destination) < 0.1f)
        {
            Destroy(gameObject);
        }
    }
}
