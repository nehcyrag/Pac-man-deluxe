using System.Collections.Generic;
using UnityEngine;

public class GhostController : MonoBehaviour
{
    private const int RecentTileMemory = 8;
    private const int RecentTilePenalty = 6;
    private static readonly Vector2Int UnsetTile = new Vector2Int(int.MinValue, int.MinValue);
    private static AudioClip deathClip;

    public enum TargetMode
    {
        CurrentPosition,
        FourTilesAhead,
        ChaseUntilCloseThenRandom,
        InkyVector,
        RandomTurns
    }

    private enum GhostState
    {
        Normal,
        Frightened,
        Eaten,
        Respawning
    }

    [SerializeField] private float moveSpeed = 6.5f;
    [SerializeField] private float frightenedMoveSpeed = 5f;
    [SerializeField] private float eatenMoveSpeed = 11f;
    [SerializeField] private float respawnDelay = 3f;
    [SerializeField, Range(0.1f, 1f)] private float ghostHouseExitSpeedMultiplier = 0.55f;
    [SerializeField] private Vector2 startDirection = Vector2.left;
    [SerializeField] private TargetMode targetMode = TargetMode.CurrentPosition;
    [SerializeField] private int lookAheadTiles = 4;
    [SerializeField] private int inkyAheadTiles = 2;
    [SerializeField] private int ambushMaxExtraPathCost = 2;
    [SerializeField] private int randomWhenWithinTiles = 8;
    [SerializeField, Range(0f, 1f)] private float detourChance = 0.25f;
    [SerializeField] private int detourMaxExtraPathCost = 3;
    [SerializeField] private LayerMask wallLayers;
    [SerializeField] private float wallCheckPadding = 0.02f;
    [SerializeField] private float collisionRadius = 0.42f;
    [SerializeField] private float laneSnapTolerance = 0.001f;
    [SerializeField] private KeyCode manualUpKey = KeyCode.I;
    [SerializeField] private KeyCode manualDownKey = KeyCode.K;
    [SerializeField] private KeyCode manualLeftKey = KeyCode.J;
    [SerializeField] private KeyCode manualRightKey = KeyCode.L;
    [SerializeField] private float tentacleAnimationSpeed = 8f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private PlayerController target;
    private GhostController blinky;
    private Vector3 startPosition;
    private Vector2 direction;
    private bool hasExitedGhostHouse;
    private Vector2 forcedDoorExitDirection;
    private readonly List<Vector2> doorExitWaypoints = new List<Vector2>();
    private int doorExitWaypointIndex;
    private bool hasMovementTarget;
    private Vector2Int movementTargetTile = UnsetTile;
    private Vector2Int previousTile = UnsetTile;
    private Vector2Int lastDecisionTile = UnsetTile;
    private Vector2Int lastRecordedTile = UnsetTile;
    private readonly List<Vector2Int> recentTiles = new List<Vector2Int>();
    private GhostState state = GhostState.Normal;
    private float respawnTimer;
    private float levelSpeedBonus;
    private float levelSpeedMultiplier = 1f;
    private float activeRespawnDelay;
    private Sprite normalSprite;
    private Sprite normalWaveSprite;
    private bool releaseLocked;
    private bool playerControlled;
    private Vector2 manualMoveInput;
    private Vector2 manualBufferedDirection;
    private Vector2 manualLastPressedDirection = Vector2.left;

    public bool CanBeEatenByPacMan => state == GhostState.Frightened;
    public bool IsDangerousToPacMan => state == GhostState.Normal;

    private void Update()
    {
        if (playerControlled)
        {
            ReadManualInput();
        }

        UpdateTentacleAnimation();
    }

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
        startPosition = GetDefaultRespawnPosition();
        direction = ToCardinalDirection(startDirection == Vector2.zero ? Vector2.left : startDirection);
        target = FindFirstObjectByType<PlayerController>();
        blinky = FindBlinky();
    }

    private void Start()
    {
        startPosition = GetDefaultRespawnPosition();
        rb.position = startPosition;
        transform.position = startPosition;
        MoveToDoorIfSpawnIsBlocked();
    }

    private void FixedUpdate()
    {
        RefreshTargetPlayer();

        if (blinky == null && targetMode == TargetMode.InkyVector)
        {
            blinky = FindBlinky();
        }

        if (target == null)
        {
            return;
        }

        if (releaseLocked && state != GhostState.Eaten && state != GhostState.Respawning)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (state == GhostState.Respawning)
        {
            TickRespawnTimer();
            return;
        }

        float moveDistance = GetCurrentMoveSpeed() * Time.fixedDeltaTime;
        float ghostHouseExitMoveDistance = moveDistance * ghostHouseExitSpeedMultiplier;
        if (TryLeaveGhostHouse(ghostHouseExitMoveDistance))
        {
            return;
        }

        if (TryFollowDoorExitLane(ghostHouseExitMoveDistance))
        {
            return;
        }

        MoveTileToTile(moveDistance);
        HandlePortalTeleport();
        UpdateGhostHouseExitState();
    }

    public void ResetGhost()
    {
        rb.position = startPosition;
        direction = ToCardinalDirection(startDirection == Vector2.zero ? Vector2.left : startDirection);
        rb.linearVelocity = Vector2.zero;
        hasExitedGhostHouse = false;
        forcedDoorExitDirection = Vector2.zero;
        doorExitWaypoints.Clear();
        doorExitWaypointIndex = 0;
        hasMovementTarget = false;
        movementTargetTile = UnsetTile;
        previousTile = UnsetTile;
        lastDecisionTile = UnsetTile;
        lastRecordedTile = UnsetTile;
        recentTiles.Clear();
        state = GhostState.Normal;
        respawnTimer = 0f;
        manualMoveInput = Vector2.zero;
        manualBufferedDirection = Vector2.zero;
        manualLastPressedDirection = direction != Vector2.zero ? direction : Vector2.left;
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        ApplyStateSprite();
    }

    public void SetReleaseLocked(bool locked)
    {
        if (releaseLocked == locked)
        {
            return;
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        releaseLocked = locked;
        if (!releaseLocked)
        {
            hasMovementTarget = false;
            movementTargetTile = UnsetTile;
            previousTile = UnsetTile;
            lastDecisionTile = UnsetTile;
            recentTiles.Clear();
            lastRecordedTile = UnsetTile;
            direction = Vector2.up;
            return;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        doorExitWaypoints.Clear();
        doorExitWaypointIndex = 0;
        forcedDoorExitDirection = Vector2.zero;
    }

    public void SetTargetMode(TargetMode mode)
    {
        targetMode = mode;
    }

    public void SetPlayerControlled(bool controlled)
    {
        if (playerControlled == controlled)
        {
            return;
        }

        playerControlled = controlled;
        manualMoveInput = Vector2.zero;
        manualBufferedDirection = Vector2.zero;
        manualLastPressedDirection = direction != Vector2.zero ? direction : Vector2.left;
        hasMovementTarget = false;
        movementTargetTile = UnsetTile;
        previousTile = UnsetTile;
        lastDecisionTile = UnsetTile;
        recentTiles.Clear();
        lastRecordedTile = UnsetTile;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void SetNormalSprite(Sprite sprite)
    {
        if (sprite == null)
        {
            return;
        }

        normalSprite = sprite;
        normalWaveSprite = GetWaveSpriteForNormalSprite(sprite, gameObject.name);
        if (state == GhostState.Normal && spriteRenderer != null)
        {
            ApplyStateSprite();
        }
    }

    public void SetLevelSpeedBonus(float bonus)
    {
        levelSpeedBonus = Mathf.Max(0f, bonus);
    }

    public void SetLevelSpeedMultiplier(float multiplier)
    {
        levelSpeedMultiplier = Mathf.Max(0.1f, multiplier);
    }

    public void EnterFrightenedMode()
    {
        if (state == GhostState.Eaten || state == GhostState.Respawning)
        {
            return;
        }

        state = GhostState.Frightened;
        hasMovementTarget = false;
        movementTargetTile = UnsetTile;
        previousTile = UnsetTile;
        recentTiles.Clear();
        lastRecordedTile = UnsetTile;
        direction = -direction;
        ApplyStateSprite();
    }

    public void ExitFrightenedMode()
    {
        if (state != GhostState.Frightened)
        {
            return;
        }

        state = GhostState.Normal;
        ApplyStateSprite();
    }

    public void EatByPacMan()
    {
        if (state != GhostState.Frightened)
        {
            return;
        }

        EnterEatenState();
    }

    public bool TryKillByBullet()
    {
        if (state == GhostState.Eaten || state == GhostState.Respawning)
        {
            return false;
        }

        EnterEatenState();
        return true;
    }

    public bool TryKillByBomb()
    {
        return TryKillByBullet();
    }

    public bool TryKillByLightning()
    {
        Vector2 position = rb != null ? rb.position : (Vector2)transform.position;
        Vector2Int currentTile = MazeGenerator.WrapPortalTile(MazeGenerator.WorldToTile(position));
        if (IsInsideGhostHouseArea(currentTile))
        {
            return false;
        }

        return TryKillByBullet();
    }

    private void EnterEatenState()
    {
        PlayDeathSound();
        state = GhostState.Eaten;
        hasMovementTarget = false;
        movementTargetTile = UnsetTile;
        previousTile = UnsetTile;
        recentTiles.Clear();
        lastRecordedTile = UnsetTile;
        ApplyStateSprite();

        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.AddScore(100);
        }
    }

    private static void PlayDeathSound()
    {
        if (deathClip == null)
        {
            deathClip = Resources.Load<AudioClip>("Audio/ghostdeath");
        }

        if (deathClip == null)
        {
            return;
        }

        GameObject audioObject = new GameObject("Ghost Death Sound");
        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.clip = deathClip;
        GameManager.SuppressPelletSoundsFor(deathClip.length);
        source.Play();
        Destroy(audioObject, deathClip.length + 0.1f);
    }

    private void MoveTileToTile(float moveDistance)
    {
        if (hasMovementTarget)
        {
            if (playerControlled)
            {
                TryReverseManualMovement();
            }

            MoveTowardMovementTarget(moveDistance);
            return;
        }

        Vector2Int currentTile = MazeGenerator.WrapPortalTile(MazeGenerator.WorldToTile(rb.position));
        if (!hasExitedGhostHouse && IsInsideGhostHouseArea(currentTile))
        {
            MoveInsideGhostHouseTowardDoor(currentTile, moveDistance);
            return;
        }

        Vector2 currentCenter = MazeGenerator.TileToWorldCenter(currentTile.x, currentTile.y);
        rb.position = currentCenter;
        transform.position = currentCenter;

        RecordVisitedTile(currentTile);

        Vector2Int nextTile = playerControlled ? ChooseManualNextTile(currentTile) : ChooseNextTile(currentTile);
        if (nextTile == currentTile || !IsGridTile(nextTile))
        {
            direction = Vector2.zero;
            return;
        }

        previousTile = currentTile;
        movementTargetTile = nextTile;
        hasMovementTarget = true;
        direction = DirectionFromTiles(currentTile, nextTile);
        MoveTowardMovementTarget(moveDistance);
    }

    private void ReadManualInput()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(manualLeftKey))
        {
            horizontal -= 1f;
        }
        if (Input.GetKey(manualRightKey))
        {
            horizontal += 1f;
        }
        if (Input.GetKey(manualDownKey))
        {
            vertical -= 1f;
        }
        if (Input.GetKey(manualUpKey))
        {
            vertical += 1f;
        }

        if (Input.GetKeyDown(manualLeftKey))
        {
            BufferManualDirection(Vector2.left);
        }
        else if (Input.GetKeyDown(manualRightKey))
        {
            BufferManualDirection(Vector2.right);
        }
        else if (Input.GetKeyDown(manualDownKey))
        {
            BufferManualDirection(Vector2.down);
        }
        else if (Input.GetKeyDown(manualUpKey))
        {
            BufferManualDirection(Vector2.up);
        }

        if (horizontal != 0f && vertical != 0f)
        {
            manualMoveInput = Mathf.Abs(manualLastPressedDirection.x) > 0f
                ? new Vector2(horizontal, 0f)
                : new Vector2(0f, vertical);
            return;
        }

        manualMoveInput = new Vector2(horizontal, vertical);
    }

    private void BufferManualDirection(Vector2 bufferedDirection)
    {
        manualLastPressedDirection = bufferedDirection;
        manualBufferedDirection = bufferedDirection;
    }

    private Vector2Int ChooseManualNextTile(Vector2Int currentTile)
    {
        if (state == GhostState.Eaten)
        {
            List<Vector2Int> eatenCandidates = GetOpenNeighborTiles(currentTile, false);
            if (eatenCandidates.Count == 0)
            {
                eatenCandidates = GetOpenNeighborTiles(currentTile, true);
            }

            return eatenCandidates.Count > 0 ? ChooseTileTowardGhostHouse(currentTile, eatenCandidates) : currentTile;
        }

        Vector2 requestedDirection = manualBufferedDirection != Vector2.zero ? manualBufferedDirection : manualMoveInput;
        if (requestedDirection != Vector2.zero && CanEnterTile(currentTile, requestedDirection))
        {
            manualBufferedDirection = Vector2.zero;
            return GetTileInDirection(currentTile, requestedDirection);
        }

        if (manualMoveInput != Vector2.zero && CanEnterTile(currentTile, manualMoveInput))
        {
            manualBufferedDirection = Vector2.zero;
            return GetTileInDirection(currentTile, manualMoveInput);
        }

        if (direction != Vector2.zero && CanEnterTile(currentTile, direction))
        {
            return GetTileInDirection(currentTile, direction);
        }

        return currentTile;
    }

    private void TryReverseManualMovement()
    {
        if (manualBufferedDirection == Vector2.zero || !IsReverseDirection(manualBufferedDirection, direction))
        {
            return;
        }

        Vector2Int currentTile = MazeGenerator.WrapPortalTile(MazeGenerator.WorldToTile(rb.position));
        Vector2Int reverseTarget = previousTile != UnsetTile ? previousTile : GetTileInDirection(currentTile, manualBufferedDirection);
        if (!IsGridTile(reverseTarget) || !IsGhostWalkableTile(reverseTarget))
        {
            return;
        }

        Vector2Int oldTarget = movementTargetTile;
        movementTargetTile = reverseTarget;
        previousTile = oldTarget;
        direction = manualBufferedDirection;
        manualBufferedDirection = Vector2.zero;
    }

    private bool CanEnterTile(Vector2Int fromTile, Vector2 moveDirection)
    {
        Vector2Int targetTile = GetTileInDirection(fromTile, moveDirection);
        return IsGridTile(targetTile)
            && IsGhostWalkableTile(targetTile)
            && CanMoveBetweenTiles(fromTile, targetTile);
    }

    private void MoveTowardMovementTarget(float moveDistance)
    {
        if (IsPortalWrapMove(previousTile, movementTargetTile))
        {
            Vector2 portalPosition = MazeGenerator.TileToWorldCenter(movementTargetTile.x, movementTargetTile.y);
            rb.position = portalPosition;
            transform.position = portalPosition;
            hasMovementTarget = false;
            RecordVisitedTile(movementTargetTile);
            return;
        }

        Vector2 targetPosition = MazeGenerator.TileToWorldCenter(movementTargetTile.x, movementTargetTile.y);
        Vector2 nextPosition = Vector2.MoveTowards(rb.position, targetPosition, moveDistance);
        rb.MovePosition(nextPosition);

        if (Vector2.Distance(nextPosition, targetPosition) > 0.001f)
        {
            return;
        }

        rb.position = targetPosition;
        transform.position = targetPosition;
        hasMovementTarget = false;
        RecordVisitedTile(movementTargetTile);
    }

    private Vector2Int ChooseNextTile(Vector2Int currentTile)
    {
        List<Vector2Int> candidates = GetOpenNeighborTiles(currentTile, false);
        if (candidates.Count == 0)
        {
            candidates = GetOpenNeighborTiles(currentTile, true);
        }

        if (candidates.Count == 0)
        {
            return currentTile;
        }

        if (state == GhostState.Eaten)
        {
            return ChooseTileTowardGhostHouse(currentTile, candidates);
        }

        if (state == GhostState.Frightened)
        {
            return ChooseTileAwayFromPacMan(candidates);
        }

        if (targetMode == TargetMode.RandomTurns)
        {
            return ChooseRandomTurnTile(currentTile, candidates);
        }

        if (targetMode == TargetMode.ChaseUntilCloseThenRandom && IsCloseToPacMan())
        {
            return candidates[Random.Range(0, candidates.Count)];
        }

        Vector2Int targetTile = MazeGenerator.WrapPortalTile(MazeGenerator.WorldToTile(GetTargetPosition()));
        if (!IsGhostWalkableTile(targetTile))
        {
            targetTile = FindNearestWalkableTile(targetTile);
        }

        int[,] distances = BuildDistanceMap(targetTile);
        int bestScore = int.MaxValue;
        List<Vector2Int> bestTiles = new List<Vector2Int>();
        List<Vector2Int> detourTiles = new List<Vector2Int>();

        foreach (Vector2Int candidate in candidates)
        {
            if (!IsGridTile(candidate) || distances[candidate.x, candidate.y] < 0)
            {
                continue;
            }

            int score = distances[candidate.x, candidate.y] + GetRecentTilePenalty(candidate);
            if (score < bestScore)
            {
                bestScore = score;
                bestTiles.Clear();
                bestTiles.Add(candidate);
            }
            else if (score == bestScore)
            {
                bestTiles.Add(candidate);
            }
        }

        if (bestTiles.Count == 0)
        {
            return candidates[Random.Range(0, candidates.Count)];
        }

        foreach (Vector2Int candidate in candidates)
        {
            if (!IsGridTile(candidate) || distances[candidate.x, candidate.y] < 0)
            {
                continue;
            }

            int extraCost = distances[candidate.x, candidate.y] + GetRecentTilePenalty(candidate) - bestScore;
            if (extraCost > 0 && extraCost <= detourMaxExtraPathCost)
            {
                detourTiles.Add(candidate);
            }
        }

        if (detourTiles.Count > 0 && Random.value < detourChance)
        {
            return detourTiles[Random.Range(0, detourTiles.Count)];
        }

        return bestTiles[Random.Range(0, bestTiles.Count)];
    }

    private Vector2Int ChooseTileTowardGhostHouse(Vector2Int currentTile, List<Vector2Int> candidates)
    {
        Vector2Int houseDoor = currentTile.x <= 13 ? new Vector2Int(13, 12) : new Vector2Int(14, 12);
        if (currentTile == houseDoor || IsGhostHouseDoorTile(currentTile))
        {
            StartRespawnAtGhostHouse();
            return currentTile;
        }

        int[,] distances = BuildDistanceMap(houseDoor);
        int bestDistance = int.MaxValue;
        List<Vector2Int> bestTiles = new List<Vector2Int>();

        foreach (Vector2Int candidate in candidates)
        {
            if (!IsGridTile(candidate) || distances[candidate.x, candidate.y] < 0)
            {
                continue;
            }

            int distance = distances[candidate.x, candidate.y];
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTiles.Clear();
                bestTiles.Add(candidate);
            }
            else if (distance == bestDistance)
            {
                bestTiles.Add(candidate);
            }
        }

        return bestTiles.Count > 0 ? bestTiles[Random.Range(0, bestTiles.Count)] : candidates[Random.Range(0, candidates.Count)];
    }

    private Vector2Int ChooseRandomTurnTile(Vector2Int currentTile, List<Vector2Int> candidates)
    {
        List<Vector2Int> turnTiles = new List<Vector2Int>();
        foreach (Vector2Int candidate in candidates)
        {
            Vector2 candidateDirection = DirectionFromTiles(currentTile, candidate);
            if (direction != Vector2.zero && Mathf.Abs(Vector2.Dot(candidateDirection, direction)) < 0.1f)
            {
                turnTiles.Add(candidate);
            }
        }

        if (turnTiles.Count > 0)
        {
            return turnTiles[Random.Range(0, turnTiles.Count)];
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private Vector2Int ChooseTileAwayFromPacMan(List<Vector2Int> candidates)
    {
        Vector2Int pacManTile = MazeGenerator.WrapPortalTile(MazeGenerator.WorldToTile(GetEffectiveTargetPosition()));
        if (!IsGhostWalkableTile(pacManTile))
        {
            pacManTile = FindNearestWalkableTile(pacManTile);
        }

        int[,] distances = BuildDistanceMap(pacManTile);
        int bestScore = int.MinValue;
        List<Vector2Int> bestTiles = new List<Vector2Int>();

        foreach (Vector2Int candidate in candidates)
        {
            if (!IsGridTile(candidate) || distances[candidate.x, candidate.y] < 0)
            {
                continue;
            }

            int score = distances[candidate.x, candidate.y] - GetRecentTilePenalty(candidate);
            if (score > bestScore)
            {
                bestScore = score;
                bestTiles.Clear();
                bestTiles.Add(candidate);
            }
            else if (score == bestScore)
            {
                bestTiles.Add(candidate);
            }
        }

        return bestTiles.Count > 0 ? bestTiles[Random.Range(0, bestTiles.Count)] : candidates[Random.Range(0, candidates.Count)];
    }

    private List<Vector2Int> GetOpenNeighborTiles(Vector2Int tile, bool allowPreviousTile)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        foreach (Vector2Int neighbor in GetNeighborTiles(tile))
        {
            if (!IsGridTile(neighbor) || !IsGhostWalkableTile(neighbor))
            {
                continue;
            }

            if (!allowPreviousTile && previousTile != UnsetTile && neighbor == previousTile)
            {
                continue;
            }

            if (!CanMoveBetweenTiles(tile, neighbor))
            {
                continue;
            }

            neighbors.Add(neighbor);
        }

        return neighbors;
    }

    private void MoveInsideGhostHouseTowardDoor(Vector2Int currentTile, float moveDistance)
    {
        Vector2Int doorTile = new Vector2Int(currentTile.x <= 13 ? 13 : 14, 12);
        Vector2 currentRowDoorPosition = MazeGenerator.TileToWorldCenter(doorTile.x, currentTile.y);
        Vector2 doorPosition = MazeGenerator.TileToWorldCenter(doorTile.x, doorTile.y);
        Vector2 targetPosition = Mathf.Abs(rb.position.x - currentRowDoorPosition.x) > 0.001f
            ? currentRowDoorPosition
            : doorPosition;

        Vector2 nextPosition = Vector2.MoveTowards(rb.position, targetPosition, moveDistance);
        Vector2 delta = targetPosition - rb.position;

        if (delta.sqrMagnitude > 0.0001f)
        {
            direction = ToCardinalDirection(delta);
        }

        rb.MovePosition(nextPosition);
        if (Vector2.Distance(nextPosition, targetPosition) <= 0.001f)
        {
            rb.position = targetPosition;
            transform.position = targetPosition;
        }
    }

    private bool CanMoveBetweenTiles(Vector2Int from, Vector2Int to)
    {
        if (from == to)
        {
            return true;
        }

        if (IsPortalWrapMove(from, to))
        {
            return true;
        }

        Vector2 fromPosition = MazeGenerator.TileToWorldCenter(from.x, from.y);
        Vector2 toPosition = MazeGenerator.TileToWorldCenter(to.x, to.y);
        Vector2 delta = toPosition - fromPosition;
        float distance = delta.magnitude;

        if (distance <= 0.001f || wallLayers.value == 0)
        {
            return true;
        }

        RaycastHit2D[] hits = Physics2D.CircleCastAll(fromPosition, collisionRadius, delta.normalized, distance, wallLayers);
        return HasOnlyGhostDoorBarrierHits(hits);
    }

    private static bool IsInsideGhostHouseArea(Vector2Int tile)
    {
        return tile.x >= 10 && tile.x < 18 && tile.y >= 12 && tile.y < 16;
    }

    private static Vector2 DirectionFromTiles(Vector2Int from, Vector2Int to)
    {
        if (IsPortalWrapMove(from, to))
        {
            return from.x == 0 ? Vector2.left : Vector2.right;
        }

        if (to.x > from.x)
        {
            return Vector2.right;
        }

        if (to.x < from.x)
        {
            return Vector2.left;
        }

        if (to.y > from.y)
        {
            return Vector2.down;
        }

        return Vector2.up;
    }

    private static bool IsPortalWrapMove(Vector2Int from, Vector2Int to)
    {
        return from.y == MazeGenerator.PortalRow
            && to.y == MazeGenerator.PortalRow
            && ((from.x == 0 && to.x == MazeGenerator.MapWidth - 1)
                || (from.x == MazeGenerator.MapWidth - 1 && to.x == 0));
    }

    private Vector2 ChooseNextDirection(Vector2 targetPosition, float moveDistance)
    {
        if (targetMode == TargetMode.ChaseUntilCloseThenRandom && IsCloseToPacMan())
        {
            return ChooseRandomDirection(moveDistance);
        }

        return ChooseShortestPathDirection(targetPosition, moveDistance);
    }

    private bool CanMove(Vector2 moveDirection, float moveDistance)
    {
        if (moveDirection == Vector2.zero || wallLayers.value == 0)
        {
            return true;
        }

        RaycastHit2D[] hits = Physics2D.CircleCastAll(rb.position, collisionRadius, moveDirection, moveDistance + wallCheckPadding, wallLayers);
        return HasOnlyGhostDoorBarrierHits(hits);
    }

    private static bool HasOnlyGhostDoorBarrierHits(RaycastHit2D[] hits)
    {
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null)
            {
                continue;
            }

            if (hit.collider.GetComponent<GhostDoorBarrier>() == null)
            {
                return false;
            }
        }

        return true;
    }

    private Vector2 ChooseShortestPathDirection(Vector2 targetPosition, float moveDistance)
    {
        Vector2Int startTile = MazeGenerator.WrapPortalTile(MazeGenerator.WorldToTile(rb.position));
        Vector2Int targetTile = MazeGenerator.WrapPortalTile(MazeGenerator.WorldToTile(targetPosition));

        if (!IsGhostWalkableTile(startTile))
        {
            return ChooseOpenDirection(moveDistance);
        }

        if (!IsGhostWalkableTile(targetTile))
        {
            targetTile = FindNearestWalkableTile(targetTile);
        }

        if (startTile == targetTile)
        {
            return ChooseOpenDirection(moveDistance);
        }

        int[,] distances = BuildDistanceMap(targetTile);
        if (distances[startTile.x, startTile.y] < 0)
        {
            return ChooseOpenDirection(moveDistance);
        }

        Vector2 nextDirection = ChooseDirectionFromDistanceMap(startTile, distances, moveDistance);
        return nextDirection != Vector2.zero ? nextDirection : ChooseOpenDirection(moveDistance);
    }

    private Vector2 ChooseDirectionFromDistanceMap(Vector2Int startTile, int[,] distances, float moveDistance)
    {
        Vector2[] directions =
        {
            Vector2.up,
            Vector2.down,
            Vector2.left,
            Vector2.right
        };

        int bestScore = int.MaxValue;
        List<Vector2> shortestDirections = new List<Vector2>();
        List<Vector2> detourDirections = new List<Vector2>();

        foreach (Vector2 candidate in directions)
        {
            if (IsReverseDirection(candidate, direction) && HasNonReverseOption(startTile, moveDistance))
            {
                continue;
            }

            Vector2Int neighbor = GetTileInDirection(startTile, candidate);
            if (!IsCandidateDirectionOpen(startTile, candidate, moveDistance) || distances[neighbor.x, neighbor.y] < 0)
            {
                continue;
            }

            int distance = distances[neighbor.x, neighbor.y];
            int score = distance + GetRecentTilePenalty(neighbor);
            if (score < bestScore)
            {
                bestScore = score;
                shortestDirections.Clear();
                shortestDirections.Add(candidate);
            }
            else if (score == bestScore)
            {
                shortestDirections.Add(candidate);
            }
        }

        if (bestScore == int.MaxValue)
        {
            return Vector2.zero;
        }

        foreach (Vector2 candidate in directions)
        {
            if (IsReverseDirection(candidate, direction) && HasNonReverseOption(startTile, moveDistance))
            {
                continue;
            }

            Vector2Int neighbor = GetTileInDirection(startTile, candidate);
            if (!IsCandidateDirectionOpen(startTile, candidate, moveDistance) || distances[neighbor.x, neighbor.y] < 0)
            {
                continue;
            }

            int extraCost = distances[neighbor.x, neighbor.y] + GetRecentTilePenalty(neighbor) - bestScore;
            if (extraCost > 0 && extraCost <= detourMaxExtraPathCost)
            {
                detourDirections.Add(candidate);
            }
        }

        if (detourDirections.Count > 0 && Random.value < detourChance)
        {
            return detourDirections[Random.Range(0, detourDirections.Count)];
        }

        return shortestDirections[Random.Range(0, shortestDirections.Count)];
    }

    private int[,] BuildDistanceMap(Vector2Int targetTile)
    {
        int[,] distances = new int[MazeGenerator.MapWidth, MazeGenerator.MapHeight];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        for (int y = 0; y < MazeGenerator.MapHeight; y++)
        {
            for (int x = 0; x < MazeGenerator.MapWidth; x++)
            {
                distances[x, y] = -1;
            }
        }

        distances[targetTile.x, targetTile.y] = 0;
        queue.Enqueue(targetTile);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            foreach (Vector2Int neighbor in GetNeighborTiles(current))
            {
                if (!IsGridTile(neighbor) || !IsGhostWalkableTile(neighbor) || distances[neighbor.x, neighbor.y] >= 0)
                {
                    continue;
                }

                distances[neighbor.x, neighbor.y] = distances[current.x, current.y] + 1;
                queue.Enqueue(neighbor);
            }
        }

        return distances;
    }

    private static Vector2Int GetTileInDirection(Vector2Int tile, Vector2 direction)
    {
        Vector2Int step = DirectionToTileStep(direction);
        return MazeGenerator.WrapPortalTile(tile + step);
    }

    private Vector2 GetTargetPosition()
    {
        Vector2 effectiveTargetPosition = GetEffectiveTargetPosition();
        if (HasPhantomTarget())
        {
            return effectiveTargetPosition;
        }

        PlayerController effectiveTarget = GetEffectiveTargetPlayer();
        if (effectiveTarget == null)
        {
            return rb.position;
        }

        if (targetMode == TargetMode.FourTilesAhead)
        {
            Vector2Int ghostTile = MazeGenerator.WrapPortalTile(MazeGenerator.WorldToTile(rb.position));
            Vector2Int pacManTile = MazeGenerator.WrapPortalTile(MazeGenerator.WorldToTile(effectiveTargetPosition));
            Vector2Int forward = DirectionToTileStep(effectiveTarget.CurrentDirection);
            Vector2Int pacManTargetTile = IsGhostWalkableTile(pacManTile) ? pacManTile : FindNearestWalkableTile(pacManTile);
            Vector2Int ambushTargetTile = MazeGenerator.WrapPortalTile(pacManTile + forward * lookAheadTiles);

            if (!IsGhostWalkableTile(ambushTargetTile))
            {
                ambushTargetTile = FindNearestWalkableTile(ambushTargetTile);
            }

            int pacManPathLength = GetShortestPathLength(ghostTile, pacManTargetTile);
            int ambushPathLength = GetShortestPathLength(ghostTile, ambushTargetTile);
            bool ambushIsTooFar = ambushPathLength < 0
                || (pacManPathLength >= 0 && ambushPathLength > pacManPathLength + ambushMaxExtraPathCost);

            Vector2Int targetTile = ambushIsTooFar ? pacManTargetTile : ambushTargetTile;
            return MazeGenerator.TileToWorldCenter(targetTile.x, targetTile.y);
        }

        if (targetMode == TargetMode.InkyVector)
        {
            Vector2Int pacManTile = MazeGenerator.WrapPortalTile(MazeGenerator.WorldToTile(effectiveTargetPosition));
            Vector2Int forward = DirectionToTileStep(effectiveTarget.CurrentDirection);
            Vector2Int aheadTile = pacManTile + forward * inkyAheadTiles;

            Vector2Int blinkyTile = blinky != null
                ? MazeGenerator.WrapPortalTile(MazeGenerator.WorldToTile(blinky.transform.position))
                : pacManTile;

            Vector2Int targetTile = blinkyTile + (aheadTile - blinkyTile) * 2;
            targetTile = new Vector2Int(
                Mathf.Clamp(targetTile.x, 0, MazeGenerator.MapWidth - 1),
                Mathf.Clamp(targetTile.y, 0, MazeGenerator.MapHeight - 1));

            if (!IsGhostWalkableTile(targetTile))
            {
                targetTile = FindNearestWalkableTile(targetTile);
            }

            return MazeGenerator.TileToWorldCenter(targetTile.x, targetTile.y);
        }

        return effectiveTargetPosition;
    }

    private Vector2 GetEffectiveTargetPosition()
    {
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null && gameManager.HasPhantomDecoy)
        {
            return gameManager.PhantomDecoyPosition;
        }

        PlayerController effectiveTarget = GetEffectiveTargetPlayer();
        return effectiveTarget != null ? (Vector2)effectiveTarget.transform.position : rb.position;
    }

    private bool HasPhantomTarget()
    {
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        return gameManager != null && gameManager.HasPhantomDecoy;
    }

    private void RefreshTargetPlayer()
    {
        PlayerController closestTarget = GetClosestActivePlayer();
        if (closestTarget != null)
        {
            target = closestTarget;
        }
    }

    private PlayerController GetEffectiveTargetPlayer()
    {
        if (target != null && target.isActiveAndEnabled && target.IsAvailableAsGhostTarget)
        {
            return target;
        }

        return GetClosestActivePlayer();
    }

    private PlayerController GetClosestActivePlayer()
    {
        PlayerController closest = null;
        float closestDistance = float.MaxValue;

        foreach (PlayerController playerController in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (playerController == null || !playerController.isActiveAndEnabled || !playerController.IsAvailableAsGhostTarget)
            {
                continue;
            }

            float distance = ((Vector2)playerController.transform.position - rb.position).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = playerController;
            }
        }

        return closest;
    }

    private static GhostController FindBlinky()
    {
        GameObject blinkyObject = GameObject.Find("Ghost");
        return blinkyObject != null ? blinkyObject.GetComponent<GhostController>() : null;
    }

    private int GetShortestPathLength(Vector2Int startTile, Vector2Int targetTile)
    {
        if (!IsGridTile(startTile) || !IsGridTile(targetTile))
        {
            return -1;
        }

        if (!IsGhostWalkableTile(startTile) || !IsGhostWalkableTile(targetTile))
        {
            return -1;
        }

        if (startTile == targetTile)
        {
            return 0;
        }

        bool[,] visited = new bool[MazeGenerator.MapWidth, MazeGenerator.MapHeight];
        int[,] distances = new int[MazeGenerator.MapWidth, MazeGenerator.MapHeight];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        visited[startTile.x, startTile.y] = true;
        queue.Enqueue(startTile);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            foreach (Vector2Int neighbor in GetNeighborTiles(current))
            {
                if (!IsGridTile(neighbor) || !IsGhostWalkableTile(neighbor) || visited[neighbor.x, neighbor.y])
                {
                    continue;
                }

                visited[neighbor.x, neighbor.y] = true;
                distances[neighbor.x, neighbor.y] = distances[current.x, current.y] + 1;

                if (neighbor == targetTile)
                {
                    return distances[neighbor.x, neighbor.y];
                }

                queue.Enqueue(neighbor);
            }
        }

        return -1;
    }

    private static Vector2Int DirectionToTileStep(Vector2 direction)
    {
        Vector2 cardinal = ToCardinalDirection(direction);

        if (cardinal == Vector2.up)
        {
            return new Vector2Int(0, -1);
        }

        if (cardinal == Vector2.down)
        {
            return new Vector2Int(0, 1);
        }

        if (cardinal == Vector2.right)
        {
            return new Vector2Int(1, 0);
        }

        return new Vector2Int(-1, 0);
    }

    private Vector2 ChooseOpenDirection(float moveDistance)
    {
        Vector2[] directions =
        {
            Vector2.up,
            Vector2.down,
            Vector2.left,
            Vector2.right
        };

        foreach (Vector2 candidate in directions)
        {
            if (IsReverseDirection(candidate, direction))
            {
                continue;
            }

            if (IsCandidateDirectionOpen(MazeGenerator.WrapPortalTile(MazeGenerator.WorldToTile(rb.position)), candidate, moveDistance))
            {
                return candidate;
            }
        }

        foreach (Vector2 candidate in directions)
        {
            if (IsCandidateDirectionOpen(MazeGenerator.WrapPortalTile(MazeGenerator.WorldToTile(rb.position)), candidate, moveDistance))
            {
                return candidate;
            }
        }

        return Vector2.zero;
    }

    private Vector2 ChooseRandomDirection(float moveDistance)
    {
        Vector2Int currentTile = MazeGenerator.WrapPortalTile(MazeGenerator.WorldToTile(rb.position));
        List<Vector2> candidates = new List<Vector2>();
        List<Vector2> recentCandidates = new List<Vector2>();

        foreach (Vector2 candidate in GetCardinalDirections())
        {
            if (IsReverseDirection(candidate, direction) && HasNonReverseOption(currentTile, moveDistance))
            {
                continue;
            }

            if (!IsCandidateDirectionOpen(currentTile, candidate, moveDistance))
            {
                continue;
            }

            Vector2Int neighbor = GetTileInDirection(currentTile, candidate);
            if (IsRecentTile(neighbor))
            {
                recentCandidates.Add(candidate);
            }
            else
            {
                candidates.Add(candidate);
            }
        }

        if (candidates.Count == 0)
        {
            if (recentCandidates.Count > 0)
            {
                return recentCandidates[Random.Range(0, recentCandidates.Count)];
            }

            return ChooseOpenDirection(moveDistance);
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private bool IsCloseToPacMan()
    {
        if (target == null)
        {
            return false;
        }

        Vector2Int ghostTile = MazeGenerator.WrapPortalTile(MazeGenerator.WorldToTile(rb.position));
        Vector2Int pacManTile = MazeGenerator.WrapPortalTile(MazeGenerator.WorldToTile(GetEffectiveTargetPosition()));
        int distance = GetShortestPathLength(ghostTile, pacManTile);
        return distance >= 0 && distance <= randomWhenWithinTiles;
    }

    private bool IsIntersection(Vector2Int tile, Vector2 currentDirection)
    {
        int forwardOptions = 0;

        foreach (Vector2 candidate in GetCardinalDirections())
        {
            if (IsReverseDirection(candidate, currentDirection))
            {
                continue;
            }

            Vector2Int neighbor = GetTileInDirection(tile, candidate);
            if (IsGhostWalkableTile(neighbor))
            {
                forwardOptions++;
            }
        }

        return forwardOptions > 1;
    }

    private bool HasNonReverseOption(Vector2Int tile, float moveDistance)
    {
        tile = MazeGenerator.WrapPortalTile(tile);

        foreach (Vector2 candidate in GetCardinalDirections())
        {
            if (IsReverseDirection(candidate, direction))
            {
                continue;
            }

            if (IsCandidateDirectionOpen(tile, candidate, moveDistance))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsCandidateDirectionOpen(Vector2Int tile, Vector2 candidate, float moveDistance)
    {
        Vector2Int neighbor = GetTileInDirection(tile, candidate);
        return IsGridTile(neighbor) && IsGhostWalkableTile(neighbor) && CanMove(candidate, moveDistance);
    }

    private void RecordVisitedTile(Vector2Int tile)
    {
        if (!IsGridTile(tile) || tile == lastRecordedTile)
        {
            return;
        }

        lastRecordedTile = tile;
        recentTiles.Remove(tile);
        recentTiles.Insert(0, tile);

        while (recentTiles.Count > RecentTileMemory)
        {
            recentTiles.RemoveAt(recentTiles.Count - 1);
        }
    }

    private int GetRecentTilePenalty(Vector2Int tile)
    {
        int recentIndex = recentTiles.IndexOf(tile);
        if (recentIndex < 0)
        {
            return 0;
        }

        return RecentTilePenalty + (RecentTileMemory - recentIndex);
    }

    private bool IsRecentTile(Vector2Int tile)
    {
        return recentTiles.Contains(tile);
    }

    private static Vector2[] GetCardinalDirections()
    {
        return new[]
        {
            Vector2.up,
            Vector2.down,
            Vector2.left,
            Vector2.right
        };
    }

    private static bool IsReverseDirection(Vector2 candidate, Vector2 currentDirection)
    {
        return currentDirection != Vector2.zero && Vector2.Dot(candidate, currentDirection) < -0.9f;
    }

    private bool IsGhostWalkableTile(Vector2Int tile)
    {
        if (!MazeGenerator.IsWalkableTile(tile))
        {
            return false;
        }

        if (state == GhostState.Eaten)
        {
            return true;
        }

        return !hasExitedGhostHouse || !IsGhostHouseDoorTile(tile);
    }

    private void UpdateGhostHouseExitState()
    {
        if (hasExitedGhostHouse)
        {
            return;
        }

        Vector2Int currentTile = MazeGenerator.WrapPortalTile(MazeGenerator.WorldToTile(rb.position));
        if (currentTile.y < 12)
        {
            hasExitedGhostHouse = true;
            BeginDoorExitLane();
        }
    }

    private static bool IsGhostHouseDoorTile(Vector2Int tile)
    {
        return tile.y == 12 && tile.x >= 13 && tile.x <= 14;
    }

    private bool TryLeaveGhostHouse(float moveDistance)
    {
        if (hasExitedGhostHouse)
        {
            return false;
        }

        Vector2Int currentTile = MazeGenerator.WrapPortalTile(MazeGenerator.WorldToTile(rb.position));
        if (currentTile.y < 12)
        {
            hasExitedGhostHouse = true;
            BeginDoorExitLane();
            return false;
        }

        if (!IsGhostHouseDoorTile(currentTile))
        {
            return false;
        }

        direction = Vector2.up;
        if (!AlignToLaneCenter(direction, moveDistance))
        {
            return true;
        }

        if (!CanMove(direction, moveDistance))
        {
            return true;
        }

        rb.MovePosition(rb.position + direction * moveDistance);
        UpdateGhostHouseExitState();
        return true;
    }

    private void BeginDoorExitLane()
    {
        if (doorExitWaypoints.Count > 0)
        {
            return;
        }

        forcedDoorExitDirection = GetPreferredDoorExitDirection();
        doorExitWaypointIndex = 0;
        doorExitWaypoints.Clear();
        doorExitWaypoints.Add(new Vector2(rb.position.x, MazeGenerator.TileToWorldCenter(13, 11).y));

        if (forcedDoorExitDirection == Vector2.right)
        {
            doorExitWaypoints.Add(MazeGenerator.TileToWorldCenter(15, 11));
            doorExitWaypoints.Add(MazeGenerator.TileToWorldCenter(15, 10));
        }
        else
        {
            doorExitWaypoints.Add(MazeGenerator.TileToWorldCenter(12, 11));
            doorExitWaypoints.Add(MazeGenerator.TileToWorldCenter(12, 10));
        }

        direction = forcedDoorExitDirection;
    }

    private bool TryFollowDoorExitLane(float moveDistance)
    {
        if (doorExitWaypoints.Count == 0)
        {
            return false;
        }

        if (doorExitWaypointIndex >= doorExitWaypoints.Count)
        {
            FinishDoorExitLane();
            return false;
        }

        Vector2 targetWaypoint = doorExitWaypoints[doorExitWaypointIndex];
        Vector2 nextPosition = Vector2.MoveTowards(rb.position, targetWaypoint, moveDistance);
        Vector2 delta = targetWaypoint - rb.position;

        if (delta.sqrMagnitude > 0.0001f)
        {
            direction = ToCardinalDirection(delta);
        }

        rb.MovePosition(nextPosition);
        if (Vector2.Distance(nextPosition, targetWaypoint) <= 0.001f)
        {
            rb.position = targetWaypoint;
            transform.position = targetWaypoint;
            doorExitWaypointIndex++;
        }

        if (doorExitWaypointIndex >= doorExitWaypoints.Count)
        {
            FinishDoorExitLane();
        }

        return true;
    }

    private void FinishDoorExitLane()
    {
        doorExitWaypoints.Clear();
        doorExitWaypointIndex = 0;
        forcedDoorExitDirection = Vector2.zero;
        hasMovementTarget = false;
        movementTargetTile = UnsetTile;
        previousTile = UnsetTile;
        lastDecisionTile = UnsetTile;
        recentTiles.Clear();
        lastRecordedTile = UnsetTile;
    }

    private void StartRespawnAtGhostHouse()
    {
        state = GhostState.Respawning;
        respawnTimer = respawnDelay;
        activeRespawnDelay = Mathf.Max(0.001f, respawnDelay);
        hasMovementTarget = false;
        movementTargetTile = UnsetTile;
        previousTile = UnsetTile;
        hasExitedGhostHouse = false;
        doorExitWaypoints.Clear();
        doorExitWaypointIndex = 0;
        rb.position = startPosition;
        transform.position = startPosition;
        rb.linearVelocity = Vector2.zero;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            ApplyStateSprite();
            SetSpriteAlpha(0f);
        }
    }

    private void TickRespawnTimer()
    {
        respawnTimer -= Time.fixedDeltaTime;
        if (respawnTimer > 0f)
        {
            SetSpriteAlpha(1f - Mathf.Clamp01(respawnTimer / activeRespawnDelay));
            return;
        }

        state = GhostState.Normal;
        respawnTimer = 0f;
        direction = Vector2.up;
        hasMovementTarget = false;
        movementTargetTile = UnsetTile;
        previousTile = UnsetTile;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        ApplyStateSprite();
        SetSpriteAlpha(1f);
    }

    private float GetCurrentMoveSpeed()
    {
        if (state == GhostState.Eaten)
        {
            return eatenMoveSpeed * levelSpeedMultiplier;
        }

        if (state == GhostState.Frightened)
        {
            return frightenedMoveSpeed * levelSpeedMultiplier;
        }

        return (moveSpeed + levelSpeedBonus) * levelSpeedMultiplier;
    }

    private void ApplyStateSprite()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (state == GhostState.Frightened)
        {
            spriteRenderer.sprite = GetAnimatedGhostSprite(SimpleSprites.FrightenedGhost, SimpleSprites.FrightenedGhostWave);
            spriteRenderer.color = Color.white;
            return;
        }

        if (state == GhostState.Eaten)
        {
            spriteRenderer.sprite = SimpleSprites.GhostEyes;
            spriteRenderer.color = Color.white;
            return;
        }

        Sprite baseSprite = normalSprite != null ? normalSprite : SimpleSprites.Ghost;
        Sprite waveSprite = normalWaveSprite != null ? normalWaveSprite : baseSprite;
        spriteRenderer.sprite = GetAnimatedGhostSprite(baseSprite, waveSprite);
        spriteRenderer.color = Color.white;
    }

    private void UpdateTentacleAnimation()
    {
        if (spriteRenderer == null || state == GhostState.Eaten || state == GhostState.Respawning)
        {
            return;
        }

        ApplyStateSprite();
    }

    private Sprite GetAnimatedGhostSprite(Sprite baseSprite, Sprite waveSprite)
    {
        int frame = Mathf.FloorToInt(Time.time * tentacleAnimationSpeed) % 2;
        return frame == 0 || waveSprite == null ? baseSprite : waveSprite;
    }

    private static Sprite GetWaveSpriteForNormalSprite(Sprite sprite, string ghostName)
    {
        if (!string.IsNullOrEmpty(ghostName))
        {
            if (ghostName.Contains("Pink"))
            {
                return SimpleSprites.PinkGhostWave;
            }

            if (ghostName.Contains("Orange"))
            {
                return SimpleSprites.OrangeGhostWave;
            }

            if (ghostName.Contains("Cyan"))
            {
                return SimpleSprites.CyanGhostWave;
            }

            if (ghostName.Contains("White"))
            {
                return SimpleSprites.WhiteGhostWave;
            }

            if (ghostName.Contains("Green"))
            {
                return SimpleSprites.GreenGhostWave;
            }
        }

        if (sprite == SimpleSprites.PinkGhost)
        {
            return SimpleSprites.PinkGhostWave;
        }

        if (sprite == SimpleSprites.OrangeGhost)
        {
            return SimpleSprites.OrangeGhostWave;
        }

        if (sprite == SimpleSprites.CyanGhost)
        {
            return SimpleSprites.CyanGhostWave;
        }

        if (sprite == SimpleSprites.WhiteGhost)
        {
            return SimpleSprites.WhiteGhostWave;
        }

        if (sprite == SimpleSprites.GreenGhost)
        {
            return SimpleSprites.GreenGhostWave;
        }

        if (sprite == SimpleSprites.Ghost)
        {
            return SimpleSprites.GhostWave;
        }

        return SimpleSprites.GhostWave;
    }

    private void SetSpriteAlpha(float alpha)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Color color = spriteRenderer.color;
        color.a = Mathf.Clamp01(alpha);
        spriteRenderer.color = color;
    }

    private Vector2 GetPreferredDoorExitDirection()
    {
        if (targetMode == TargetMode.FourTilesAhead || targetMode == TargetMode.InkyVector)
        {
            return Vector2.right;
        }

        return Vector2.left;
    }

    private static IEnumerable<Vector2Int> GetNeighborTiles(Vector2Int tile)
    {
        yield return MazeGenerator.WrapPortalTile(new Vector2Int(tile.x, tile.y - 1));
        yield return MazeGenerator.WrapPortalTile(new Vector2Int(tile.x, tile.y + 1));
        yield return MazeGenerator.WrapPortalTile(new Vector2Int(tile.x - 1, tile.y));
        yield return MazeGenerator.WrapPortalTile(new Vector2Int(tile.x + 1, tile.y));
    }

    private Vector2Int FindNearestWalkableTile(Vector2Int tile)
    {
        Vector2Int clampedTile = new Vector2Int(
            Mathf.Clamp(tile.x, 0, MazeGenerator.MapWidth - 1),
            Mathf.Clamp(tile.y, 0, MazeGenerator.MapHeight - 1));

        if (IsGhostWalkableTile(clampedTile))
        {
            return clampedTile;
        }

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        bool[,] visited = new bool[MazeGenerator.MapWidth, MazeGenerator.MapHeight];
        queue.Enqueue(clampedTile);
        visited[clampedTile.x, clampedTile.y] = true;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            foreach (Vector2Int neighbor in GetNeighborTiles(current))
            {
                if (!IsGridTile(neighbor))
                {
                    continue;
                }

                if (visited[neighbor.x, neighbor.y])
                {
                    continue;
                }

                if (IsGhostWalkableTile(neighbor))
                {
                    return neighbor;
                }

                visited[neighbor.x, neighbor.y] = true;
                queue.Enqueue(neighbor);
            }
        }

        return clampedTile;
    }

    private static bool IsGridTile(Vector2Int tile)
    {
        return tile.x >= 0 && tile.x < MazeGenerator.MapWidth && tile.y >= 0 && tile.y < MazeGenerator.MapHeight;
    }

    private void HandlePortalTeleport()
    {
        Vector2 position = rb.position;
        float portalY = MazeGenerator.TileToWorldCenter(0, MazeGenerator.PortalRow).y;

        if (Mathf.Abs(position.y - portalY) > 0.25f)
        {
            return;
        }

        if (position.x < -14.5f)
        {
            TeleportTo(new Vector2(13.5f, portalY));
        }
        else if (position.x > 14.5f)
        {
            TeleportTo(new Vector2(-13.5f, portalY));
        }
    }

    private void TeleportTo(Vector2 position)
    {
        rb.position = position;
        transform.position = position;
        Physics2D.SyncTransforms();
    }

    private void MoveToDoorIfSpawnIsBlocked()
    {
        Physics2D.SyncTransforms();

        Vector2Int currentTile = MazeGenerator.WorldToTile(rb.position);
        Collider2D wall = wallLayers.value != 0 ? Physics2D.OverlapCircle(rb.position, collisionRadius, wallLayers) : null;
        if (IsInsideGhostHouseArea(currentTile) && wall == null)
        {
            return;
        }

        if (IsGhostWalkableTile(currentTile) && wall == null)
        {
            return;
        }

        Vector2 doorPosition = targetMode == TargetMode.FourTilesAhead ? new Vector2(0.5f, 2.5f) : new Vector2(-0.5f, 2.5f);
        rb.position = doorPosition;
        transform.position = doorPosition;
        rb.linearVelocity = Vector2.zero;
        direction = Vector2.up;
    }

    private Vector3 GetDefaultRespawnPosition()
    {
        if (gameObject.name == "Ghost")
        {
            return new Vector3(-1.5f, 1.5f, transform.position.z);
        }

        if (gameObject.name == "PinkGhost")
        {
            return new Vector3(-0.5f, 1.5f, transform.position.z);
        }

        if (gameObject.name == "OrangeGhost")
        {
            return new Vector3(0.5f, 1.5f, transform.position.z);
        }

        if (gameObject.name == "CyanGhost")
        {
            return new Vector3(1.5f, 1.5f, transform.position.z);
        }

        if (gameObject.name == "WhiteGhost")
        {
            return new Vector3(-1.5f, 0.5f, transform.position.z);
        }

        if (gameObject.name == "GreenGhost")
        {
            return new Vector3(-0.5f, 0.5f, transform.position.z);
        }

        return transform.position;
    }

    private bool AlignToLaneCenter(Vector2 moveDirection, float moveDistance)
    {
        Vector2 position = rb.position;
        bool horizontalMove = Mathf.Abs(moveDirection.x) > 0f;
        float current = horizontalMove ? position.y : position.x;
        float targetCenter = NearestLaneCenter(current);

        if (Mathf.Abs(current - targetCenter) <= laneSnapTolerance)
        {
            rb.position = horizontalMove ? new Vector2(position.x, targetCenter) : new Vector2(targetCenter, position.y);
            return true;
        }

        float next = Mathf.MoveTowards(current, targetCenter, moveDistance);
        Vector2 snappedPosition = horizontalMove ? new Vector2(position.x, next) : new Vector2(next, position.y);
        rb.MovePosition(snappedPosition);
        return Mathf.Abs(next - targetCenter) <= laneSnapTolerance;
    }

    private bool IsAtLaneCenter(float moveDistance)
    {
        Vector2 position = rb.position;
        float tolerance = Mathf.Max(laneSnapTolerance, moveDistance * 0.75f);
        return Mathf.Abs(position.x - NearestLaneCenter(position.x)) <= tolerance
            && Mathf.Abs(position.y - NearestLaneCenter(position.y)) <= tolerance;
    }

    private static float NearestLaneCenter(float value)
    {
        return Mathf.Round(value - 0.5f) + 0.5f;
    }

    private static Vector2 ToCardinalDirection(Vector2 value)
    {
        if (value == Vector2.zero)
        {
            return Vector2.left;
        }

        return Mathf.Abs(value.x) >= Mathf.Abs(value.y)
            ? new Vector2(Mathf.Sign(value.x), 0f)
            : new Vector2(0f, Mathf.Sign(value.y));
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
            spriteRenderer.sprite = SimpleSprites.Ghost;
        }

        normalSprite = spriteRenderer.sprite;
        normalWaveSprite = GetWaveSpriteForNormalSprite(normalSprite, gameObject.name);
        ApplyStateSprite();
    }
}
