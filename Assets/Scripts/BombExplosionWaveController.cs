using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class BombExplosionWaveController : MonoBehaviour
{
    [SerializeField] private float collisionRadius = 0.28f;
    [SerializeField] private float segmentSpacing = 0.5f;
    [SerializeField] private float portalTriggerX = 14.5f;
    [SerializeField] private float portalExitX = 13.5f;
    [SerializeField] private LayerMask wallLayers;

    private Rigidbody2D rb;
    private Vector2 direction = Vector2.right;
    private float speed = 24f;
    private float lifetime = 3f;
    private BombController owner;
    private Vector2 lastSegmentPosition;
    private bool hasStopped;
    private readonly List<GameObject> explosionSegments = new List<GameObject>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
        circleCollider.radius = collisionRadius;
        circleCollider.isTrigger = true;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = SimpleSprites.BombExplosion;
        spriteRenderer.color = Color.white;
        spriteRenderer.sortingOrder = 6;

        transform.localScale = Vector3.one * 0.65f;

        if (wallLayers.value == 0)
        {
            wallLayers = LayerMask.GetMask("Wall");
        }
    }

    private void FixedUpdate()
    {
        if (hasStopped)
        {
            return;
        }

        lifetime -= Time.fixedDeltaTime;
        if (lifetime <= 0f)
        {
            StopAtWall();
            return;
        }

        float moveDistance = speed * Time.fixedDeltaTime;
        RaycastHit2D wallHit = Physics2D.CircleCast(rb.position, collisionRadius, direction, moveDistance, wallLayers);
        if (wallHit.collider != null)
        {
            float safeDistance = Mathf.Max(0f, wallHit.distance - 0.01f);
            Vector2 stopPosition = rb.position + direction * safeDistance;
            rb.MovePosition(stopPosition);
            SpawnSegmentsTo(stopPosition);
            StopAtWall();
            return;
        }

        Vector2 nextPosition = rb.position + direction * moveDistance;
        rb.MovePosition(nextPosition);
        SpawnSegmentsTo(nextPosition);
        HandlePortalTeleport();
    }

    public void Launch(Vector2 launchDirection, float launchSpeed, float launchLifetime, BombController bombOwner)
    {
        direction = launchDirection == Vector2.zero ? Vector2.right : launchDirection.normalized;
        speed = Mathf.Max(0f, launchSpeed);
        lifetime = Mathf.Max(Time.fixedDeltaTime, launchLifetime);
        owner = bombOwner;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        lastSegmentPosition = transform.position;
        CreateExplosionSegment(lastSegmentPosition);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            if (owner != null)
            {
                owner.TryDamagePlayer(player);
            }

            return;
        }

        GhostController ghost = other.GetComponent<GhostController>();
        if (ghost != null)
        {
            ghost.TryKillByBomb();
        }
    }

    private void SpawnSegmentsTo(Vector2 targetPosition)
    {
        float distance = Vector2.Distance(lastSegmentPosition, targetPosition);
        if (distance < segmentSpacing)
        {
            return;
        }

        int segmentCount = Mathf.FloorToInt(distance / segmentSpacing);
        for (int i = 0; i < segmentCount; i++)
        {
            lastSegmentPosition += direction * segmentSpacing;
            CreateExplosionSegment(lastSegmentPosition);
        }
    }

    private void CreateExplosionSegment(Vector2 position)
    {
        GameObject segment = new GameObject("Bomb Explosion Segment");
        segment.transform.position = position;
        segment.transform.rotation = transform.rotation;
        segment.transform.localScale = Vector3.one * 0.65f;
        explosionSegments.Add(segment);

        Rigidbody2D segmentBody = segment.AddComponent<Rigidbody2D>();
        segmentBody.gravityScale = 0f;
        segmentBody.bodyType = RigidbodyType2D.Kinematic;
        segmentBody.freezeRotation = true;

        SpriteRenderer spriteRenderer = segment.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = SimpleSprites.BombExplosion;
        spriteRenderer.color = Color.white;
        spriteRenderer.sortingOrder = 5;

        CircleCollider2D circleCollider = segment.AddComponent<CircleCollider2D>();
        circleCollider.radius = collisionRadius;
        circleCollider.isTrigger = true;

        BombExplosionSegmentController segmentController = segment.AddComponent<BombExplosionSegmentController>();
        segmentController.Initialize(owner);
    }

    private void StopAtWall()
    {
        if (hasStopped)
        {
            return;
        }

        hasStopped = true;
        rb.linearVelocity = Vector2.zero;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider != null)
        {
            circleCollider.enabled = false;
        }

        if (owner != null)
        {
            owner.NotifyExplosionWaveStopped(this);
        }
    }

    public void ForceClear()
    {
        ClearExplosionSegments();
        Destroy(gameObject);
    }

    private void HandlePortalTeleport()
    {
        Vector2 position = rb.position;
        float portalY = MazeGenerator.TileToWorldCenter(0, MazeGenerator.PortalRow).y;

        if (Mathf.Abs(position.y - portalY) > 0.25f)
        {
            return;
        }

        if (position.x < -portalTriggerX)
        {
            TeleportTo(new Vector2(portalExitX, portalY));
        }
        else if (position.x > portalTriggerX)
        {
            TeleportTo(new Vector2(-portalExitX, portalY));
        }
    }

    private void TeleportTo(Vector2 position)
    {
        rb.position = position;
        transform.position = position;
        lastSegmentPosition = position;
        CreateExplosionSegment(position);
        Physics2D.SyncTransforms();
    }

    private void OnDestroy()
    {
        ClearExplosionSegments();
    }

    private void ClearExplosionSegments()
    {
        foreach (GameObject segment in explosionSegments)
        {
            if (segment != null)
            {
                Destroy(segment);
            }
        }

        explosionSegments.Clear();
    }
}
