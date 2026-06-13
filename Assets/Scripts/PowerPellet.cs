using UnityEngine;

public class PowerPellet : MonoBehaviour
{
    [SerializeField] private int points = 10;
    [SerializeField] private bool countsTowardLevel;
    [SerializeField] private float flashSpeed = 7f;
    [SerializeField] private float minAlpha = 0.35f;
    [SerializeField] private float maxAlpha = 1f;
    [SerializeField] private float pulseScaleAmount = 0.12f;

    private SpriteRenderer spriteRenderer;
    private Vector3 baseScale;

    public bool CountsTowardLevel => countsTowardLevel;

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
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.AddScore(points);
            gameManager.PlayPowerPelletSound();
            gameManager.ActivatePowerPellet();
            gameManager.NotifyPowerPelletEaten(this);
            if (countsTowardLevel)
            {
                gameManager.NotifyCollectibleEaten();
            }
        }

        Destroy(gameObject);
    }

    public void SetCountsTowardLevel(bool value)
    {
        countsTowardLevel = value;
    }
}
