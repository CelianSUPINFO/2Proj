using UnityEngine;

// Script qui contrôle le déplacement d’un bateau vers une destination
public class BateauController : MonoBehaviour
{
    // Position cible vers laquelle le bateau doit se déplacer
    public Vector3 destination;

    // Vitesse de déplacement du bateau
    public float vitesse = 2f;

    // Méthode appelée à chaque frame
    void Update()
    {
        // Déplace le bateau progressivement vers la destination
        transform.position = Vector3.MoveTowards(transform.position, destination, vitesse * Time.deltaTime);

        // Si le bateau est suffisamment proche de la destination 
        if (Vector3.Distance(transform.position, destination) < 0.1f)
        {
            // On le détruit 
            Destroy(gameObject);
        }
    }
}
