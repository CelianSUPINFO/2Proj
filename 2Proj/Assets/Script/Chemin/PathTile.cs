using UnityEngine;

// Script attaché à chaque morceau de chemin pour donner un bonus de vitesse
public class PathTile : MonoBehaviour
{
    public GameAge cheminAge = GameAge.StoneAge; // Niveau d’âge du chemin (plus l’âge est avancé, plus c’est rapide)

    // Retourne le multiplicateur de vitesse selon l’âge du chemin
    public float GetSpeedMultiplier()
    {
        return cheminAge switch
        {
            GameAge.StoneAge => 1.2f,
            GameAge.AncientAge => 1.4f,
            GameAge.MedievalAge => 1.6f,
            GameAge.IndustrialAge => 1.8f,
            _ => 1f
        };
    }
}
