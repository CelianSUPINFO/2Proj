using UnityEngine;

public class PathTile : MonoBehaviour
{
    public GameAge cheminAge = GameAge.StoneAge;

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
