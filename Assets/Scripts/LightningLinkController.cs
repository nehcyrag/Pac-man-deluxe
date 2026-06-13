using UnityEngine;

public class LightningLinkController : MonoBehaviour
{
    [SerializeField] private float duration = 6f;
    [SerializeField] private float damageWidth = 0.18f;
    [SerializeField] private float endpointSafeDistance = 0.55f;
    [SerializeField] private float lineWidth = 0.16f;
    [SerializeField] private float flickerSpeed = 22f;

    private PlayerController playerOne;
    private PlayerController playerTwo;
    private LineRenderer lineRenderer;
    private float timer;

    public void Initialize(PlayerController firstPlayer, PlayerController secondPlayer, float activeDuration)
    {
        playerOne = firstPlayer;
        playerTwo = secondPlayer;
        duration = activeDuration;
        timer = duration;
        BuildLineRenderer();
        UpdateLink();
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f || !IsPlayerUsable(playerOne) || !IsPlayerUsable(playerTwo))
        {
            Destroy(gameObject);
            return;
        }

        UpdateLink();
        DamageGhostsOnLine();
    }

    private void BuildLineRenderer()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.sortingOrder = 5;
    }

    private void UpdateLink()
    {
        Vector3 start = playerOne.transform.position;
        Vector3 end = playerTwo.transform.position;
        float flicker = (Mathf.Sin(Time.time * flickerSpeed) + 1f) * 0.5f;
        Color lightningColor = new Color(10f / 255f, 186f / 255f, 181f / 255f, 1f);
        Color core = Color.Lerp(lightningColor, Color.white, flicker);

        lineRenderer.startColor = core;
        lineRenderer.endColor = core;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }

    private void DamageGhostsOnLine()
    {
        Vector2 start = playerOne.transform.position;
        Vector2 end = playerTwo.transform.position;
        Vector2 delta = end - start;
        float length = delta.magnitude;
        if (length <= 0.01f)
        {
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(start + delta * 0.5f, length * 0.5f + damageWidth);
        foreach (Collider2D hit in hits)
        {
            GhostController ghost = hit.GetComponent<GhostController>();
            if (ghost != null && IsColliderTouchingLightningSegment(hit, start, delta, length))
            {
                ghost.TryKillByLightning();
            }
        }
    }

    private bool IsColliderTouchingLightningSegment(Collider2D collider, Vector2 start, Vector2 delta, float length)
    {
        Vector2 closestPoint = collider.ClosestPoint(start + delta * 0.5f);
        float projection = Vector2.Dot(closestPoint - start, delta.normalized);
        if (projection <= endpointSafeDistance || projection >= length - endpointSafeDistance)
        {
            return false;
        }

        Vector2 pointOnLine = start + delta.normalized * projection;
        return Vector2.Distance(closestPoint, pointOnLine) <= damageWidth * 0.5f;
    }

    private static bool IsPlayerUsable(PlayerController player)
    {
        return player != null && player.gameObject.activeInHierarchy && player.IsAvailableAsGhostTarget;
    }
}
