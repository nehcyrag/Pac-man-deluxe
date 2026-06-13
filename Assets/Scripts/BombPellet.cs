using UnityEngine;

public class BombPellet : MonoBehaviour
{
    [SerializeField] private int points = 10;
    [SerializeField] private float flashSpeed = 7f;
    [SerializeField] private float minAlpha = 0.35f;
    [SerializeField] private float maxAlpha = 1f;
    [SerializeField] private float pulseScaleAmount = 0.12f;

    private SpriteRenderer spriteRenderer;
    private Vector3 baseScale;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
    }

    private void Update()
    {
        float pulse = (Mathf.Sin(Time.time * flashSpeed) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, pulse);

        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }

        transform.localScale = baseScale * (1f + pulse * pulseScaleAmount);
    }

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
            if (eater != null)
            {
                eater.GrantBombAmmo();
            }
            else
            {
                gameManager.GrantPlayerBombAmmo();
            }

            gameManager.NotifyBombPelletEaten(this);
        }

        Destroy(gameObject);
    }
}
