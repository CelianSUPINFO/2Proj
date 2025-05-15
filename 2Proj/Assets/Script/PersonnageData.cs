using UnityEngine;

public enum JobType
{
    None,
    Bûcheron,
    Fermier,
    Chercheur,
    Boulanger,
    Constructeur,
    Transporteur
}

public class PersonnageData : MonoBehaviour
{
    public string nom;
    public float vie = 100f;
    public float faim = 100f;
    public float soif = 100f;
    public float fatigue = 100f;
    public JobType metier = JobType.None;

    public float vitesse = 1.5f;

    private Vector3 cible;
    private float timer = 0f;


    private Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
        ChoisirNouvelleCible();
    }


    private void Update()
    {
        // Diminuer faim/soif/fatigue
        faim -= Time.deltaTime * 0.2f;
        soif -= Time.deltaTime * 0.25f;
        fatigue -= Time.deltaTime * 0.15f;

        // Mort si vide
        if (vie <= 0 || faim <= 0 || soif <= 0 || fatigue <= 0)
        {
            Destroy(gameObject);
            return;
        }

        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            ChoisirNouvelleCible();
        }

        // Déplacement vers la cible
        transform.position = Vector3.MoveTowards(transform.position, cible, vitesse * Time.deltaTime);
        Vector3 direction = (cible - transform.position).normalized;
        animator.SetFloat("DirectionX", direction.x);
        animator.SetFloat("DirectionY", direction.y);

    }

    void ChoisirNouvelleCible()
    {
        float x = Random.Range(-10f, 10f);
        float y = Random.Range(-5f, 5f);
        cible = new Vector3(x, y, 0);
        timer = Random.Range(3f, 6f);
    }
}
