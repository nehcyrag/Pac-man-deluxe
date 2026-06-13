using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class EnergyBulletController : MonoBehaviour
{
    [SerializeField] private float collisionRadius = 0.18f;
    [SerializeField] private float portalTriggerX = 14.5f;
    [SerializeField] private float portalExitX = 13.5f;
    [SerializeField] private LayerMask wallLayers;

    private Rigidbody2D rb;
    private Vector2 direction = Vector2.right;
    private float speed = 14f;
    private float lifetime = 3f;

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
        spriteRenderer.sprite = SimpleSprites.EnergyBullet;
        spriteRenderer.color = Color.white;
        spriteRenderer.sortingOrder = 5;

        transform.localScale = Vector3.one * 0.55f;

        if (wallLayers.value == 0)
        {
            wallLayers = LayerMask.GetMask("Wall");
        }
    }

    private void FixedUpdate()
    {
        lifetime -= Time.fixedDeltaTime;
        if (lifetime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        float moveDistance = speed * Time.fixedDeltaTime;
        if (Physics2D.CircleCast(rb.position, collisionRadius, direction, moveDistance, wallLayers))
        {
            Destroy(gameObject);
            return;
        }

        rb.MovePosition(rb.position + direction * moveDistance);
        HandlePortalTeleport();
    }

    public void Launch(Vector2 launchDirection, float launchSpeed, float launchLifetime)
    {
        direction = launchDirection == Vector2.zero ? Vector2.right : launchDirection.normalized;
        speed = Mathf.Max(0f, launchSpeed);
        lifetime = Mathf.Max(Time.fixedDeltaTime, launchLifetime);
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        GhostController ghost = other.GetComponent<GhostController>();
        if (ghost != null && ghost.TryKillByBullet())
        {
            Destroy(gameObject);
            return;
        }

        if (IsWall(other))
        {
            Destroy(gameObject);
        }
    }

    private bool IsWall(Collider2D other)
    {
        return (wallLayers.value & (1 << other.gameObject.layer)) != 0;
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
        Physics2D.SyncTransforms();
    }
}
