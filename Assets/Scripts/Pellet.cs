using UnityEngine;

public class Pellet : MonoBehaviour
{
    [SerializeField] private int points = 10;

    public int Points => points;

    public void Eat()
    {
        Eat(null);
    }

    public void Eat(PlayerController eater)
    {
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.AddScore(points);
            gameManager.PlayPelletEatSound(eater);
            gameManager.NotifyCollectibleEaten();
        }

        Destroy(gameObject);
    }
}
