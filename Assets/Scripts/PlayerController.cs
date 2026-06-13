using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private LayerMask wallLayers;
    [SerializeField] private float collisionRadius = 0.34f;
    [SerializeField] private float wallCheckPadding = 0.01f;
    [SerializeField] private Vector2 fallbackStartPosition = new Vector2(-0.5f, -7.5f);
    [SerializeField] private float laneSnapTolerance = 0.001f;
    [SerializeField] private float portalY = 0.5f;
    [SerializeField] private float portalExitX = 13.5f;
    [SerializeField] private float portalTriggerX = 14.5f;
    [SerializeField] private float effectSpeedMultiplier = 3f;
    [SerializeField] private float bulletSpawnOffset = 0.58f;
    [SerializeField] private float bulletLifetime = 3f;
    [SerializeField] private float bombPlaceOffset = 0f;
    [SerializeField] private KeyCode upKey = KeyCode.UpArrow;
    [SerializeField] private KeyCode downKey = KeyCode.DownArrow;
    [SerializeField] private KeyCode leftKey = KeyCode.LeftArrow;
    [SerializeField] private KeyCode rightKey = KeyCode.RightArrow;
    [SerializeField] private KeyCode alternateUpKey = KeyCode.None;
    [SerializeField] private KeyCode alternateDownKey = KeyCode.None;
    [SerializeField] private KeyCode alternateLeftKey = KeyCode.None;
    [SerializeField] private KeyCode alternateRightKey = KeyCode.None;
    [SerializeField] private KeyCode fireKey = KeyCode.Return;
    [SerializeField] private Color spriteTint = Color.white;
    [SerializeField] private float mouthAnimationSpeed = 12f;
    [SerializeField] private float invincibleBlinkSpeed = 14f;
    [SerializeField, Range(0f, 1f)] private float invincibleBlinkMinAlpha = 0.35f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Vector2 moveInput;
    private Vector2 facingDirection = Vector2.right;
    private Vector2 startDirection = Vector2.right;
    private Vector2 startPosition;
    private Vector2 lastPressedDirection = Vector2.right;
    private Vector2 bufferedTurnDirection;
    private bool hasBulletAmmo;
    private bool hasBombAmmo;
    private bool isRespawning;
    private bool isEliminated;
    private bool isRespawnInvincible;
    private float respawnTimer;
    private float activeRespawnDelay;
    private float respawnInvincibleTimer;
    private Collider2D playerCollider;
    private GameManager gameManager;
    private AudioClip bulletFireClip;
    private AudioSource audioSource;
    private Sprite[] mouthSprites;

    public Vector2 CurrentDirection => facingDirection;
    public bool HasBulletAmmo => hasBulletAmmo;
    public bool HasBombAmmo => hasBombAmmo;
    public bool IsVulnerable => !isRespawning && !isEliminated && !isRespawnInvincible;
    public bool IsAvailableAsGhostTarget => !isRespawning && !isEliminated;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        if (wallLayers.value == 0)
        {
            wallLayers = LayerMask.GetMask("Wall");
        }

        ConfigureRigidbody();
        ConfigureCollider();
        ConfigureSprite();
        startPosition = rb.position;
    }

    private void Start()
    {
        MoveToFallbackStartIfBlocked();
        startPosition = rb.position;
    }

    private void Update()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        TickRespawn();
        UpdateMouthAnimation();
        UpdateInvincibleBlink();

        if (isRespawning || isEliminated)
        {
            return;
        }

        ReadInput();
        ReadFireInput();
    }

    private void FixedUpdate()
    {
        if (isRespawning || isEliminated)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Move();
        HandlePortalTeleport();
        CheckGhostContacts();

        UpdateSpriteFacing();
    }

    public void ResetPlayer()
    {
        rb.position = startPosition;
        transform.position = startPosition;
        moveInput = Vector2.zero;
        facingDirection = startDirection;
        lastPressedDirection = startDirection;
        bufferedTurnDirection = Vector2.zero;
        hasBulletAmmo = false;
        hasBombAmmo = false;
        isRespawning = false;
        isEliminated = false;
        isRespawnInvincible = false;
        respawnTimer = 0f;
        respawnInvincibleTimer = 0f;
        rb.linearVelocity = Vector2.zero;
        SetVisibleAndCollidable(true);
        UpdateSpriteFacing();
    }

    public void ConfigureControls(
        KeyCode up,
        KeyCode down,
        KeyCode left,
        KeyCode right,
        KeyCode fire,
        Vector2 respawnPosition,
        Vector2 initialDirection,
        Color tint,
        bool useAlternateKeys)
    {
        upKey = up;
        downKey = down;
        leftKey = left;
        rightKey = right;
        fireKey = fire;
        alternateUpKey = useAlternateKeys ? KeyCode.UpArrow : KeyCode.None;
        alternateDownKey = useAlternateKeys ? KeyCode.DownArrow : KeyCode.None;
        alternateLeftKey = useAlternateKeys ? KeyCode.LeftArrow : KeyCode.None;
        alternateRightKey = useAlternateKeys ? KeyCode.RightArrow : KeyCode.None;
        fallbackStartPosition = respawnPosition;
        startPosition = respawnPosition;
        startDirection = ToCardinalDirection(initialDirection == Vector2.zero ? Vector2.right : initialDirection);
        facingDirection = startDirection;
        lastPressedDirection = startDirection;
        spriteTint = tint;
        transform.position = respawnPosition;

        if (rb != null)
        {
            rb.position = respawnPosition;
        }

        if (spriteRenderer != null)
        {
            ApplySpriteTint(1f);
        }
    }

    public void GrantBulletAmmo()
    {
        hasBulletAmmo = true;
        hasBombAmmo = false;
    }

    public void GrantBombAmmo()
    {
        hasBombAmmo = true;
        hasBulletAmmo = false;
    }

    public void BeginRespawn(float delaySeconds, float invincibleSeconds)
    {
        if (isEliminated)
        {
            return;
        }

        isRespawning = true;
        isRespawnInvincible = false;
        respawnTimer = Mathf.Max(0f, delaySeconds);
        activeRespawnDelay = Mathf.Max(0.001f, respawnTimer);
        respawnInvincibleTimer = Mathf.Max(0f, invincibleSeconds);
        moveInput = Vector2.zero;
        bufferedTurnDirection = Vector2.zero;
        facingDirection = startDirection;
        lastPressedDirection = startDirection;
        rb.position = startPosition;
        transform.position = startPosition;
        rb.linearVelocity = Vector2.zero;
        SetVisibleAndCollidable(true, false);
        ApplySpriteTint(respawnTimer > 0f ? 0f : 1f);
        UpdateSpriteFacing();
    }

    public void Eliminate()
    {
        isEliminated = true;
        isRespawning = false;
        isRespawnInvincible = false;
        respawnTimer = 0f;
        activeRespawnDelay = 0f;
        respawnInvincibleTimer = 0f;
        moveInput = Vector2.zero;
        bufferedTurnDirection = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        SetVisibleAndCollidable(false);
        ApplySpriteTint(1f);
    }

    private void ReadInput()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (IsHeld(leftKey) || IsHeld(alternateLeftKey))
        {
            horizontal -= 1f;
        }
        if (IsHeld(rightKey) || IsHeld(alternateRightKey))
        {
            horizontal += 1f;
        }
        if (IsHeld(downKey) || IsHeld(alternateDownKey))
        {
            vertical -= 1f;
        }
        if (IsHeld(upKey) || IsHeld(alternateUpKey))
        {
            vertical += 1f;
        }

        if (WasPressed(leftKey) || WasPressed(alternateLeftKey))
        {
            lastPressedDirection = Vector2.left;
            BufferTurn(Vector2.left);
        }
        else if (WasPressed(rightKey) || WasPressed(alternateRightKey))
        {
            lastPressedDirection = Vector2.right;
            BufferTurn(Vector2.right);
        }
        else if (WasPressed(downKey) || WasPressed(alternateDownKey))
        {
            lastPressedDirection = Vector2.down;
            BufferTurn(Vector2.down);
        }
        else if (WasPressed(upKey) || WasPressed(alternateUpKey))
        {
            lastPressedDirection = Vector2.up;
            BufferTurn(Vector2.up);
        }

        if (horizontal != 0f && vertical != 0f)
        {
            if (Mathf.Abs(lastPressedDirection.x) > 0f)
            {
                moveInput = new Vector2(horizontal, 0f);
            }
            else
            {
                moveInput = new Vector2(0f, vertical);
            }

            return;
        }

        moveInput = new Vector2(horizontal, vertical);
    }

    private void ReadFireInput()
    {
        if (!WasPressed(fireKey) || !CanAct())
        {
            return;
        }

        if (hasBombAmmo)
        {
            PlaceBomb();
            return;
        }

        if (hasBulletAmmo)
        {
            FireBullet();
        }
    }

    private static bool IsHeld(KeyCode key)
    {
        return key != KeyCode.None && Input.GetKey(key);
    }

    private static bool WasPressed(KeyCode key)
    {
        return key != KeyCode.None && Input.GetKeyDown(key);
    }

    private void TickRespawn()
    {
        if (isRespawning)
        {
            respawnTimer -= Time.deltaTime;
            if (respawnTimer > 0f)
            {
                ApplySpriteTint(1f - Mathf.Clamp01(respawnTimer / activeRespawnDelay));
                return;
            }

            rb.position = startPosition;
            transform.position = startPosition;
            moveInput = Vector2.zero;
            bufferedTurnDirection = Vector2.zero;
            facingDirection = startDirection;
            lastPressedDirection = startDirection;
            rb.linearVelocity = Vector2.zero;
            isRespawning = false;
            isRespawnInvincible = respawnInvincibleTimer > 0f;
            respawnTimer = 0f;
            activeRespawnDelay = 0f;
            SetVisibleAndCollidable(true);
            ApplySpriteTint(1f);
            UpdateSpriteFacing();
        }

        if (!isRespawnInvincible)
        {
            return;
        }

        respawnInvincibleTimer -= Time.deltaTime;
        if (respawnInvincibleTimer <= 0f)
        {
            isRespawnInvincible = false;
            ApplySpriteTint(1f);
        }
    }

    private void UpdateInvincibleBlink()
    {
        if (spriteRenderer == null || isRespawning || isEliminated)
        {
            return;
        }

        bool globalInvincible = gameManager != null && gameManager.IsInvincible;
        if (!globalInvincible && !isRespawnInvincible)
        {
            ApplySpriteTint(1f);
            return;
        }

        float pulse = (Mathf.Sin(Time.unscaledTime * invincibleBlinkSpeed) + 1f) * 0.5f;
        ApplySpriteTint(Mathf.Lerp(invincibleBlinkMinAlpha, 1f, pulse));
    }

    private bool CanAct()
    {
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        return gameManager == null
            || (gameManager.HasStarted && !gameManager.IsGameEnded && !gameManager.IsCountingDown && !gameManager.IsPaused);
    }

    private void FireBullet()
    {
        PlayBulletFireSound();

        Vector2 direction = facingDirection != Vector2.zero ? facingDirection : Vector2.right;
        Vector2 spawnPosition = rb.position + direction.normalized * bulletSpawnOffset;

        GameObject bullet = new GameObject("Energy Bullet");
        bullet.transform.position = spawnPosition;
        bullet.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        EnergyBulletController bulletController = bullet.AddComponent<EnergyBulletController>();
        bulletController.Launch(direction, moveSpeed * effectSpeedMultiplier, bulletLifetime);
        hasBulletAmmo = false;
    }

    private void PlayBulletFireSound()
    {
        if (bulletFireClip == null)
        {
            bulletFireClip = Resources.Load<AudioClip>("Audio/bulletfire");
        }

        if (bulletFireClip == null)
        {
            return;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
        }

        audioSource.Stop();
        audioSource.clip = bulletFireClip;
        GameManager.SuppressPelletSoundsFor(bulletFireClip.length);
        audioSource.Play();
    }

    private void PlaceBomb()
    {
        Vector2 placePosition = rb.position;
        if (bombPlaceOffset > 0f && facingDirection != Vector2.zero)
        {
            placePosition += facingDirection.normalized * bombPlaceOffset;
        }

        Vector2Int tile = MazeGenerator.WrapPortalTile(MazeGenerator.WorldToTile(placePosition));
        GameObject bomb = new GameObject("Bomb");
        bomb.transform.position = MazeGenerator.TileToWorldCenter(tile.x, tile.y);
        BombController bombController = bomb.AddComponent<BombController>();
        bombController.SetExplosionSpeed(moveSpeed * effectSpeedMultiplier);
        hasBombAmmo = false;
    }

    private void Move()
    {
        Vector2 moveDirection = ChooseMoveDirection();
        if (moveDirection == Vector2.zero)
        {
            rb.linearVelocity = Vector2.zero;
            RecoverToNearestLaneCenter();
            return;
        }

        if (!AlignToLaneCenter(moveDirection))
        {
            return;
        }

        Vector2 delta = moveDirection * moveSpeed * Time.fixedDeltaTime;
        if (TryMove(delta))
        {
            facingDirection = moveDirection;
            return;
        }

        if (facingDirection != Vector2.zero && moveDirection != facingDirection)
        {
            Vector2 fallbackDelta = facingDirection * moveSpeed * Time.fixedDeltaTime;
            if (AlignToLaneCenter(facingDirection) && TryMove(fallbackDelta))
            {
                return;
            }
        }

        RecoverToNearestLaneCenter();
    }

    private Vector2 ChooseMoveDirection()
    {
        float moveDistance = moveSpeed * Time.fixedDeltaTime;

        if (HasBufferedTurn() && CanTurn(bufferedTurnDirection, moveDistance))
        {
            Vector2 turnDirection = bufferedTurnDirection;
            bufferedTurnDirection = Vector2.zero;
            return turnDirection;
        }

        if (moveInput == Vector2.zero)
        {
            return facingDirection;
        }

        if (IsReverseDirection(moveInput, facingDirection))
        {
            return moveInput;
        }

        if (IsSameDirection(moveInput, facingDirection) || facingDirection == Vector2.zero)
        {
            return moveInput;
        }

        if (CanTurn(moveInput, moveDistance))
        {
            bufferedTurnDirection = Vector2.zero;
            return moveInput;
        }

        return facingDirection != Vector2.zero ? facingDirection : moveInput;
    }

    private void BufferTurn(Vector2 direction)
    {
        bufferedTurnDirection = direction;
    }

    private bool HasBufferedTurn()
    {
        return bufferedTurnDirection != Vector2.zero;
    }

    private bool CanTurn(Vector2 direction, float moveDistance)
    {
        if (IsReverseDirection(direction, facingDirection))
        {
            return CanMove(direction, moveDistance);
        }

        if (!IsAtIntersectionCenter(moveDistance))
        {
            return false;
        }

        if (!IsNextTileWalkable(direction))
        {
            return false;
        }

        return CanMove(direction, moveDistance);
    }

    private bool CanMove(Vector2 direction, float distance)
    {
        if (direction == Vector2.zero)
        {
            return false;
        }

        RaycastHit2D hit = Physics2D.CircleCast(rb.position, collisionRadius, direction, distance + wallCheckPadding, wallLayers);
        return hit.collider == null;
    }

    private static bool IsSameDirection(Vector2 a, Vector2 b)
    {
        return a != Vector2.zero && b != Vector2.zero && Vector2.Dot(a, b) > 0.9f;
    }

    private static bool IsReverseDirection(Vector2 a, Vector2 b)
    {
        return a != Vector2.zero && b != Vector2.zero && Vector2.Dot(a, b) < -0.9f;
    }

    private bool IsNextTileWalkable(Vector2 direction)
    {
        Vector2Int currentTile = MazeGenerator.WrapPortalTile(MazeGenerator.WorldToTile(rb.position));
        Vector2Int nextTile = MazeGenerator.WrapPortalTile(currentTile + DirectionToTileStep(direction));
        return MazeGenerator.IsWalkableTile(nextTile);
    }

    private static Vector2Int DirectionToTileStep(Vector2 direction)
    {
        if (direction == Vector2.up)
        {
            return new Vector2Int(0, -1);
        }

        if (direction == Vector2.down)
        {
            return new Vector2Int(0, 1);
        }

        if (direction == Vector2.right)
        {
            return new Vector2Int(1, 0);
        }

        return new Vector2Int(-1, 0);
    }

    private static Vector2 ToCardinalDirection(Vector2 value)
    {
        if (value == Vector2.zero)
        {
            return Vector2.right;
        }

        return Mathf.Abs(value.x) >= Mathf.Abs(value.y)
            ? new Vector2(Mathf.Sign(value.x), 0f)
            : new Vector2(0f, Mathf.Sign(value.y));
    }

    private bool IsAlignedToLaneCenter(Vector2 direction, float moveDistance)
    {
        Vector2 position = rb.position;
        bool horizontalMove = Mathf.Abs(direction.x) > 0f;
        float current = horizontalMove ? position.y : position.x;
        float target = NearestLaneCenter(current);
        float tolerance = Mathf.Max(laneSnapTolerance, moveDistance * 0.75f);
        return Mathf.Abs(current - target) <= tolerance;
    }

    private bool IsAtIntersectionCenter(float moveDistance)
    {
        Vector2 position = rb.position;
        float tolerance = Mathf.Max(laneSnapTolerance, moveDistance * 0.5f);
        return Mathf.Abs(position.x - NearestLaneCenter(position.x)) <= tolerance
            && Mathf.Abs(position.y - NearestLaneCenter(position.y)) <= tolerance;
    }

    private void RecoverToNearestLaneCenter()
    {
        Vector2 position = rb.position;
        Vector2 target = new Vector2(NearestLaneCenter(position.x), NearestLaneCenter(position.y));
        Vector2 next = Vector2.MoveTowards(position, target, moveSpeed * Time.fixedDeltaTime);

        if (!Physics2D.OverlapCircle(next, collisionRadius, wallLayers))
        {
            rb.MovePosition(next);
        }
    }

    private bool TryMove(Vector2 delta)
    {
        if (delta == Vector2.zero)
        {
            return false;
        }

        float distance = delta.magnitude;
        Vector2 direction = delta / distance;
        RaycastHit2D hit = Physics2D.CircleCast(rb.position, collisionRadius, direction, distance + wallCheckPadding, wallLayers);

        if (hit.collider != null)
        {
            return false;
        }

        rb.MovePosition(rb.position + delta);
        return true;
    }

    private bool AlignToLaneCenter(Vector2 direction)
    {
        Vector2 position = rb.position;
        bool horizontalMove = Mathf.Abs(direction.x) > 0f;
        float current = horizontalMove ? position.y : position.x;
        float target = NearestLaneCenter(current);

        if (Mathf.Abs(current - target) <= laneSnapTolerance)
        {
            if (horizontalMove)
            {
                rb.position = new Vector2(position.x, target);
            }
            else
            {
                rb.position = new Vector2(target, position.y);
            }

            return true;
        }

        float next = Mathf.MoveTowards(current, target, moveSpeed * Time.fixedDeltaTime);
        Vector2 snappedPosition = horizontalMove ? new Vector2(position.x, next) : new Vector2(next, position.y);
        rb.MovePosition(snappedPosition);
        return Mathf.Abs(next - target) <= laneSnapTolerance;
    }

    private static float NearestLaneCenter(float value)
    {
        return Mathf.Round(value - 0.5f) + 0.5f;
    }

    private void HandlePortalTeleport()
    {
        Vector2 position = rb.position;
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

    private void MoveToFallbackStartIfBlocked()
    {
        Physics2D.SyncTransforms();

        Collider2D wall = Physics2D.OverlapCircle(rb.position, collisionRadius, wallLayers);
        if (wall == null)
        {
            return;
        }

        rb.position = fallbackStartPosition;
        transform.position = fallbackStartPosition;
        rb.linearVelocity = Vector2.zero;
        moveInput = Vector2.zero;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Pellet pellet = other.GetComponent<Pellet>();
        if (pellet != null)
        {
            pellet.Eat(this);
            return;
        }

        PowerPellet powerPellet = other.GetComponent<PowerPellet>();
        if (powerPellet != null)
        {
            powerPellet.Eat();
            return;
        }

        EnergyPellet energyPellet = other.GetComponent<EnergyPellet>();
        if (energyPellet != null)
        {
            energyPellet.Eat(this);
            return;
        }

        PhantomPellet phantomPellet = other.GetComponent<PhantomPellet>();
        if (phantomPellet != null)
        {
            phantomPellet.Eat();
            return;
        }

        BombPellet bombPellet = other.GetComponent<BombPellet>();
        if (bombPellet != null)
        {
            bombPellet.Eat(this);
            return;
        }

        LightningPellet lightningPellet = other.GetComponent<LightningPellet>();
        if (lightningPellet != null)
        {
            lightningPellet.Eat();
            return;
        }

        GhostController ghost = other.GetComponent<GhostController>();
        HandleGhostContact(ghost);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleGhostContact(collision.collider.GetComponent<GhostController>());
    }

    private void HandleGhostContact(GhostController ghost)
    {
        if (ghost == null)
        {
            return;
        }

        if (ghost.CanBeEatenByPacMan)
        {
            ghost.EatByPacMan();
            return;
        }

        if (ghost.IsDangerousToPacMan)
        {
            if (!IsVulnerable)
            {
                return;
            }

            GameManager gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.HandlePacManHitByGhost(this);
            }
        }
    }

    private void CheckGhostContacts()
    {
        Collider2D[] contacts = Physics2D.OverlapCircleAll(rb.position, collisionRadius + 0.12f);
        foreach (Collider2D contact in contacts)
        {
            if (contact.attachedRigidbody == rb)
            {
                continue;
            }

            HandleGhostContact(contact.GetComponent<GhostController>());
        }
    }

    private void ConfigureRigidbody()
    {
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void ConfigureCollider()
    {
        CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider == null)
        {
            circleCollider = gameObject.AddComponent<CircleCollider2D>();
        }

        circleCollider.radius = collisionRadius;
        circleCollider.isTrigger = false;
        playerCollider = circleCollider;
    }

    private void ConfigureSprite()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = SimpleSprites.PacMan;
        }

        mouthSprites = new[]
        {
            SimpleSprites.PacMan,
            SimpleSprites.PacManHalfOpen,
            SimpleSprites.PacManClosed,
            SimpleSprites.PacManHalfOpen
        };

        ApplySpriteTint(1f);
        UpdateSpriteFacing();
    }

    private void UpdateMouthAnimation()
    {
        if (spriteRenderer == null || mouthSprites == null || mouthSprites.Length == 0 || isRespawning || isEliminated)
        {
            return;
        }

        int frame = Mathf.FloorToInt(Time.time * mouthAnimationSpeed) % mouthSprites.Length;
        spriteRenderer.sprite = mouthSprites[frame];
    }

    private void ApplySpriteTint(float alpha)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Color color = spriteTint;
        color.a = Mathf.Clamp01(alpha);
        spriteRenderer.color = color;
    }

    private void SetVisibleAndCollidable(bool enabled)
    {
        SetVisibleAndCollidable(enabled, enabled);
    }

    private void SetVisibleAndCollidable(bool visible, bool collidable)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = visible;
        }

        if (playerCollider != null)
        {
            playerCollider.enabled = collidable;
        }
    }

    private void UpdateSpriteFacing()
    {
        if (facingDirection == Vector2.zero)
        {
            return;
        }

        float angle = Mathf.Atan2(facingDirection.y, facingDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector2 position = Application.isPlaying && rb != null ? rb.position : (Vector2)transform.position;
        Vector2 direction = Application.isPlaying && moveInput != Vector2.zero ? moveInput : facingDirection;

        if (direction != Vector2.zero)
        {
            Gizmos.DrawLine(position, position + direction.normalized * 0.6f);
        }
    }

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            ConfigureRigidbody();
        }

        ConfigureCollider();
        ConfigureSprite();
        wallLayers = LayerMask.GetMask("Wall");
    }

}
