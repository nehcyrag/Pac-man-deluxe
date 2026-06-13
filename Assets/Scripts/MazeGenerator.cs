using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    public const int MapWidth = 28;
    public const int MapHeight = 30;
    public const int PortalRow = 14;

    private const float TileSize = 16f;
    private const float PelletRadius = 0.096f;
    private const float WallLineWidth = 0.125f;

    private static readonly int[,] WallRects =
    {
        { 0, 0, 28, 1 },
        { 0, 1, 1, 8 },
        { 27, 1, 1, 8 },
        { 13, 1, 2, 4 },
        { 2, 2, 4, 3 },
        { 7, 2, 5, 3 },
        { 16, 2, 5, 3 },
        { 22, 2, 4, 3 },
        { 2, 6, 4, 2 },
        { 7, 6, 2, 3 },
        { 10, 6, 8, 2 },
        { 19, 6, 2, 3 },
        { 22, 6, 4, 2 },
        { 13, 8, 2, 3 },
        { 0, 9, 6, 1 },
        { 7, 9, 5, 2 },
        { 16, 9, 5, 2 },
        { 22, 9, 6, 1 },
        { 5, 10, 1, 3 },
        { 22, 10, 1, 3 },
        { 7, 11, 2, 3 },
        { 19, 11, 2, 3 },
        { 10, 12, 3, 1 },
        { 15, 12, 3, 1 },
        { 0, 13, 6, 1 },
        { 10, 13, 1, 3 },
        { 17, 13, 1, 3 },
        { 22, 13, 6, 1 },
        { 0, 15, 6, 1 },
        { 7, 15, 2, 5 },
        { 19, 15, 2, 5 },
        { 22, 15, 6, 1 },
        { 10, 16, 8, 1 },
        { 5, 16, 1, 3 },
        { 22, 16, 1, 3 },
        { 10, 18, 8, 2 },
        { 0, 19, 6, 1 },
        { 22, 19, 6, 1 },
        { 0, 20, 1, 3 },
        { 13, 20, 2, 2 },
        { 27, 20, 1, 3 },
        { 2, 21, 4, 1 },
        { 7, 21, 5, 1 },
        { 16, 21, 5, 1 },
        { 22, 21, 4, 1 },
        { 4, 22, 2, 3 },
        { 22, 22, 2, 3 },
        { 0, 23, 3, 2 },
        { 7, 23, 2, 3 },
        { 10, 23, 8, 2 },
        { 19, 23, 2, 3 },
        { 25, 23, 3, 2 },
        { 0, 25, 1, 4 },
        { 13, 25, 2, 3 },
        { 27, 25, 1, 4 },
        { 2, 26, 10, 2 },
        { 16, 26, 10, 2 },
        { 0, 29, 28, 1 }
    };

    private static bool[,] blockedTiles;
    private Transform wallsRoot;
    private Transform pelletsRoot;
    private Sprite wallSegmentSprite;
    private Sprite pelletSprite;
    private Sprite ghostDoorSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureGeneratorExists()
    {
        if (FindFirstObjectByType<MazeGenerator>() != null)
        {
            return;
        }

        new GameObject("MazeGenerator").AddComponent<MazeGenerator>();
    }

    private void Awake()
    {
        Generate();
    }

    public void Generate()
    {
        ClearGeneratedObjects();

        wallsRoot = new GameObject("Generated Walls").transform;
        wallsRoot.SetParent(transform);

        pelletsRoot = new GameObject("Generated Pellets").transform;
        pelletsRoot.SetParent(transform);

        wallSegmentSprite = CreateSolidWallSprite();
        ghostDoorSprite = CreateSolidSprite("Ghost Door Sprite", new Color(1f, 0.35f, 0.85f, 1f));
        pelletSprite = CreateCircleSprite("Pellet Sprite", new Color(1f, 0.78f, 0.12f, 1f));

        blockedTiles = BuildBlockedTiles();

        bool[,] reachableTiles = BuildReachableTiles();

        GenerateWalls();
        GenerateGhostHouse();
        int collectibleCount = 0;
        collectibleCount += GeneratePellets(blockedTiles, reachableTiles);
        RegisterCollectibleCount(collectibleCount);
    }

    private void GenerateWalls()
    {
        int wallLayer = LayerMask.NameToLayer("Wall");

        for (int i = 0; i < WallRects.GetLength(0); i++)
        {
            int x = WallRects[i, 0];
            int y = WallRects[i, 1];
            int width = WallRects[i, 2];
            int height = WallRects[i, 3];
            if (IsGhostHouseWallRect(x, y, width, height))
            {
                continue;
            }

            CreateWall("Wall " + i, x, y, width, height, wallLayer);
        }
    }

    private static bool IsGhostHouseWallRect(int x, int y, int width, int height)
    {
        return (x == 10 && y == 12 && width == 3 && height == 1)
            || (x == 15 && y == 12 && width == 3 && height == 1)
            || (x == 10 && y == 13 && width == 1 && height == 3)
            || (x == 17 && y == 13 && width == 1 && height == 3)
            || (x == 10 && y == 16 && width == 8 && height == 1);
    }

    private void GenerateGhostHouse()
    {
        const float x = 10f;
        const float y = 12f;
        const float width = 8f;
        const float height = 5f;
        const float wallThickness = 1f;

        int wallLayer = LayerMask.NameToLayer("Wall");

        CreateGhostHouseWallCollider("Ghost House Top Left", x, y, 3f, wallThickness, wallLayer);
        CreateGhostHouseWallCollider("Ghost House Top Right", 15f, y, 3f, wallThickness, wallLayer);
        CreateGhostHouseWallCollider("Ghost House Bottom", x, y + height - wallThickness, width, wallThickness, wallLayer);
        CreateGhostHouseWallCollider("Ghost House Left", x, y + wallThickness, wallThickness, height - wallThickness * 2f, wallLayer);
        CreateGhostHouseWallCollider("Ghost House Right", x + width - wallThickness, y + wallThickness, wallThickness, height - wallThickness * 2f, wallLayer);
        CreateGhostHouseOutlineVisual(x, y, width, height, wallThickness);
        CreateGhostDoorBarrier(wallLayer);
    }

    private void CreateGhostHouseWallCollider(string name, float x, float y, float width, float height, int layer)
    {
        GameObject wall = new GameObject(name);
        wall.layer = layer >= 0 ? layer : 0;
        wall.transform.SetParent(wallsRoot);
        wall.transform.position = TileToWorldCenter(x, y, width, height);

        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(width, height);
    }

    private void CreateGhostHouseOutlineVisual(float x, float y, float width, float height, float wallThickness)
    {
        GameObject outline = new GameObject("Ghost House Outline");
        outline.transform.SetParent(wallsRoot);

        float right = x + width;
        float bottom = y + height;
        float doorLeft = 13f;
        float doorRight = 15f;

        CreateWallVisualSegment(outline.transform, "Outer Top Left", x, y, doorLeft - x, WallLineWidth);
        CreateWallVisualSegment(outline.transform, "Outer Top Right", doorRight, y, right - doorRight, WallLineWidth);
        CreateWallVisualSegment(outline.transform, "Outer Left", x, y, WallLineWidth, height);
        CreateWallVisualSegment(outline.transform, "Outer Right", right - WallLineWidth, y, WallLineWidth, height);
        CreateWallVisualSegment(outline.transform, "Outer Bottom", x, bottom - WallLineWidth, width, WallLineWidth);
    }

    private int GeneratePellets(bool[,] blockedTiles, bool[,] reachableTiles)
    {
        int count = 0;

        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                if (blockedTiles[x, y] || !reachableTiles[x, y] || IsInsideGhostHouse(x, y) || IsPacManSpawnTile(x, y))
                {
                    continue;
                }

                GameObject pellet = new GameObject("Pellet");
                pellet.transform.SetParent(pelletsRoot);
                pellet.transform.position = TileToWorldCenter(x, y, 1f, 1f);
                pellet.transform.localScale = Vector3.one * (PelletRadius * 2f);

                SpriteRenderer renderer = pellet.AddComponent<SpriteRenderer>();
                renderer.sprite = pelletSprite;
                renderer.color = Color.white;

                CircleCollider2D collider = pellet.AddComponent<CircleCollider2D>();
                collider.radius = 0.5f;
                collider.isTrigger = true;

                pellet.AddComponent<Pellet>();
                count++;
            }
        }

        return count;
    }

    private static void RegisterCollectibleCount(int count)
    {
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.RegisterCollectibles(count);
        }
    }

    public static Vector2Int WorldToTile(Vector2 worldPosition)
    {
        int x = Mathf.RoundToInt(worldPosition.x + MapWidth * 0.5f - 0.5f);
        int y = Mathf.RoundToInt(MapHeight * 0.5f - worldPosition.y - 0.5f);
        return new Vector2Int(x, y);
    }

    public static Vector2 TileToWorldCenter(int x, int y)
    {
        return TileToWorldCenter(x, y, 1f, 1f);
    }

    public static bool IsWalkableTile(Vector2Int tile)
    {
        EnsureBlockedTiles();

        if (tile.y == PortalRow && (tile.x < 0 || tile.x >= MapWidth))
        {
            return true;
        }

        if (tile.x < 0 || tile.x >= MapWidth || tile.y < 0 || tile.y >= MapHeight)
        {
            return false;
        }

        if (IsInsideGhostHouseInterior(tile.x, tile.y))
        {
            return false;
        }

        return !blockedTiles[tile.x, tile.y];
    }

    public static Vector2Int WrapPortalTile(Vector2Int tile)
    {
        if (tile.y == PortalRow)
        {
            if (tile.x < 0)
            {
                return new Vector2Int(MapWidth - 1, tile.y);
            }

            if (tile.x >= MapWidth)
            {
                return new Vector2Int(0, tile.y);
            }
        }

        return tile;
    }

    public static List<Vector2Int> GetReachableWalkableTiles()
    {
        bool[,] reachableTiles = BuildReachableTiles();
        List<Vector2Int> tiles = new List<Vector2Int>();

        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                if (reachableTiles[x, y] && !IsInsideGhostHouse(x, y))
                {
                    tiles.Add(new Vector2Int(x, y));
                }
            }
        }

        return tiles;
    }

    private static bool[,] BuildReachableTiles()
    {
        bool[,] reachableTiles = new bool[MapWidth, MapHeight];
        Vector2Int startTile = WorldToTile(new Vector2(-0.5f, -7.5f));

        if (!IsWalkableTile(startTile))
        {
            return reachableTiles;
        }

        Vector2Int[] queue = new Vector2Int[MapWidth * MapHeight];
        int head = 0;
        int tail = 0;

        reachableTiles[startTile.x, startTile.y] = true;
        queue[tail++] = startTile;

        while (head < tail)
        {
            Vector2Int current = queue[head++];
            foreach (Vector2Int neighbor in GetNeighborTiles(current))
            {
                if (neighbor.x < 0 || neighbor.x >= MapWidth || neighbor.y < 0 || neighbor.y >= MapHeight)
                {
                    continue;
                }

                if (reachableTiles[neighbor.x, neighbor.y] || !IsWalkableTile(neighbor))
                {
                    continue;
                }

                reachableTiles[neighbor.x, neighbor.y] = true;
                queue[tail++] = neighbor;
            }
        }

        return reachableTiles;
    }

    private static Vector2Int[] GetNeighborTiles(Vector2Int tile)
    {
        return new[]
        {
            WrapPortalTile(new Vector2Int(tile.x, tile.y - 1)),
            WrapPortalTile(new Vector2Int(tile.x, tile.y + 1)),
            WrapPortalTile(new Vector2Int(tile.x - 1, tile.y)),
            WrapPortalTile(new Vector2Int(tile.x + 1, tile.y))
        };
    }

    private static void EnsureBlockedTiles()
    {
        if (blockedTiles == null)
        {
            blockedTiles = BuildBlockedTiles();
        }
    }

    private static bool[,] BuildBlockedTiles()
    {
        bool[,] blockedTiles = new bool[MapWidth, MapHeight];

        for (int i = 0; i < WallRects.GetLength(0); i++)
        {
            int x = WallRects[i, 0];
            int y = WallRects[i, 1];
            int width = WallRects[i, 2];
            int height = WallRects[i, 3];

            for (int tileY = y; tileY < y + height; tileY++)
            {
                for (int tileX = x; tileX < x + width; tileX++)
                {
                    if (tileX >= 0 && tileX < MapWidth && tileY >= 0 && tileY < MapHeight)
                    {
                        blockedTiles[tileX, tileY] = true;
                    }
                }
            }
        }

        return blockedTiles;
    }

    private void CreateWall(string name, int x, int y, int width, int height, int layer)
    {
        CreateWallWorld(name, x, y, width, height, layer, true);
    }

    private void CreateWallWorld(string name, float x, float y, float width, float height, int layer, bool removeSharedEdges = false)
    {
        GameObject wall = new GameObject(name);
        wall.layer = layer >= 0 ? layer : 0;
        wall.transform.SetParent(wallsRoot);
        wall.transform.position = TileToWorldCenter(x, y, width, height);
        wall.transform.localScale = Vector3.one;

        if (width <= WallLineWidth * 2f || height <= WallLineWidth * 2f)
        {
            CreateWallVisualSegment(wall.transform, "Line", x, y, width, height);
        }
        else
        {
            CreateWallOutline(wall.transform, x, y, width, height, removeSharedEdges);
        }

        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(width, height);
    }

    private void CreateWallOutline(Transform parent, float x, float y, float width, float height, bool removeSharedEdges)
    {
        CreateHorizontalWallSide(parent, "Top", x, y, width, y, false, removeSharedEdges);
        CreateHorizontalWallSide(parent, "Bottom", x, y + height - WallLineWidth, width, y + height, true, removeSharedEdges);
        CreateVerticalWallSide(parent, "Left", x, y, height, x, false, removeSharedEdges);
        CreateVerticalWallSide(parent, "Right", x + width - WallLineWidth, y, height, x + width, true, removeSharedEdges);
    }

    private void CreateHorizontalWallSide(Transform parent, string name, float drawX, float drawY, float width, float sharedY, bool matchOtherTopEdge, bool removeSharedEdges)
    {
        if (!removeSharedEdges)
        {
            CreateWallVisualSegment(parent, name, drawX, drawY, width, WallLineWidth);
            return;
        }

        List<Vector2> sharedIntervals = GetSharedHorizontalIntervals(drawX, sharedY, width, matchOtherTopEdge);
        CreateWallSideParts(parent, name, drawX, drawX + width, sharedIntervals, (partName, start, length) =>
        {
            CreateWallVisualSegment(parent, partName, start, drawY, length, WallLineWidth);
        });
    }

    private void CreateVerticalWallSide(Transform parent, string name, float drawX, float drawY, float height, float sharedX, bool matchOtherLeftEdge, bool removeSharedEdges)
    {
        if (!removeSharedEdges)
        {
            CreateWallVisualSegment(parent, name, drawX, drawY, WallLineWidth, height);
            return;
        }

        List<Vector2> sharedIntervals = GetSharedVerticalIntervals(drawY, sharedX, height, matchOtherLeftEdge);
        CreateWallSideParts(parent, name, drawY, drawY + height, sharedIntervals, (partName, start, length) =>
        {
            CreateWallVisualSegment(parent, partName, drawX, start, WallLineWidth, length);
        });
    }

    private void CreateWallSideParts(Transform parent, string name, float start, float end, List<Vector2> sharedIntervals, System.Action<string, float, float> createPart)
    {
        float cursor = start;
        int partIndex = 0;

        sharedIntervals.Sort((a, b) => a.x.CompareTo(b.x));
        foreach (Vector2 interval in sharedIntervals)
        {
            float intervalStart = Mathf.Clamp(interval.x, start, end);
            float intervalEnd = Mathf.Clamp(interval.y, start, end);
            if (intervalEnd <= intervalStart)
            {
                continue;
            }

            if (intervalStart > cursor)
            {
                createPart(name + " " + partIndex, cursor, intervalStart - cursor);
                partIndex++;
            }

            cursor = Mathf.Max(cursor, intervalEnd);
        }

        if (cursor < end)
        {
            createPart(name + " " + partIndex, cursor, end - cursor);
        }
    }

    private List<Vector2> GetSharedHorizontalIntervals(float x, float sharedY, float width, bool matchOtherTopEdge)
    {
        List<Vector2> intervals = new List<Vector2>();
        float minX = x;
        float maxX = x + width;

        for (int i = 0; i < WallRects.GetLength(0); i++)
        {
            float otherX = WallRects[i, 0];
            float otherY = WallRects[i, 1];
            float otherWidth = WallRects[i, 2];
            float otherHeight = WallRects[i, 3];
            bool touches = matchOtherTopEdge
                ? Mathf.Approximately(otherY, sharedY)
                : Mathf.Approximately(otherY + otherHeight, sharedY);

            if (!touches)
            {
                continue;
            }

            float overlapStart = Mathf.Max(minX, otherX);
            float overlapEnd = Mathf.Min(maxX, otherX + otherWidth);
            if (overlapEnd > overlapStart)
            {
                intervals.Add(new Vector2(overlapStart, overlapEnd));
            }
        }

        return intervals;
    }

    private List<Vector2> GetSharedVerticalIntervals(float y, float sharedX, float height, bool matchOtherLeftEdge)
    {
        List<Vector2> intervals = new List<Vector2>();
        float minY = y;
        float maxY = y + height;

        for (int i = 0; i < WallRects.GetLength(0); i++)
        {
            float otherX = WallRects[i, 0];
            float otherY = WallRects[i, 1];
            float otherWidth = WallRects[i, 2];
            float otherHeight = WallRects[i, 3];
            bool touches = matchOtherLeftEdge
                ? Mathf.Approximately(otherX, sharedX)
                : Mathf.Approximately(otherX + otherWidth, sharedX);

            if (!touches)
            {
                continue;
            }

            float overlapStart = Mathf.Max(minY, otherY);
            float overlapEnd = Mathf.Min(maxY, otherY + otherHeight);
            if (overlapEnd > overlapStart)
            {
                intervals.Add(new Vector2(overlapStart, overlapEnd));
            }
        }

        return intervals;
    }

    private void CreateWallVisualSegment(Transform parent, string name, float x, float y, float width, float height)
    {
        if (width <= 0f || height <= 0f)
        {
            return;
        }

        GameObject segment = new GameObject(name);
        segment.transform.SetParent(parent, false);
        segment.transform.position = TileToWorldCenter(x, y, width, height);

        SpriteRenderer renderer = segment.AddComponent<SpriteRenderer>();
        renderer.sprite = wallSegmentSprite;
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = new Vector2(width, height);
        renderer.color = Color.white;
    }

    private void CreateGhostDoorBarrier(int layer)
    {
        const float x = 13f;
        const float y = 12f;
        const float width = 2f;
        const float height = WallLineWidth;

        GameObject door = new GameObject("Ghost Door Barrier");
        door.layer = layer >= 0 ? layer : 0;
        door.transform.SetParent(wallsRoot);
        door.transform.position = TileToWorldCenter(x, y, width, height);

        SpriteRenderer renderer = door.AddComponent<SpriteRenderer>();
        renderer.sprite = ghostDoorSprite;
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = new Vector2(width, height);
        renderer.color = Color.white;

        BoxCollider2D collider = door.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(width, height);

        door.AddComponent<GhostDoorBarrier>();
    }

    private static Vector3 TileToWorldCenter(float x, float y, float width, float height)
    {
        float worldX = x + width * 0.5f - MapWidth * 0.5f;
        float worldY = -(y + height * 0.5f - MapHeight * 0.5f);
        return new Vector3(worldX, worldY, 0f);
    }

    private static bool IsInsideGhostHouse(int x, int y)
    {
        return x >= 10 && x < 18 && y >= 12 && y < 16;
    }

    private static bool IsPacManSpawnTile(int x, int y)
    {
        Vector2Int playerOneSpawn = WorldToTile(new Vector2(-0.5f, -7.5f));
        Vector2Int playerTwoSpawn = WorldToTile(new Vector2(0.5f, -7.5f));
        return (x == playerOneSpawn.x && y == playerOneSpawn.y)
            || (x == playerTwoSpawn.x && y == playerTwoSpawn.y);
    }

    private static bool IsInsideGhostHouseInterior(int x, int y)
    {
        return x >= 10 && x < 18 && y >= 13 && y < 16;
    }

    private void ClearGeneratedObjects()
    {
        DestroyExisting("Generated Walls");
        DestroyExisting("Generated Pellets");
        DestroyExisting("Generated Power Pellets");
        DestroyExisting("Walls");
    }

    private static void DestroyExisting(string objectName)
    {
        GameObject existing = GameObject.Find(objectName);
        if (existing == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(existing);
        }
        else
        {
            DestroyImmediate(existing);
        }
    }

    private static Sprite CreateSolidWallSprite()
    {
        return CreateSolidSprite("Wall Segment Sprite", new Color(0.05f, 0.12f, 1f, 1f));
    }

    private static Sprite CreateSolidSprite(string name, Color color)
    {
        const int size = 8;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = name,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        for (int py = 0; py < size; py++)
        {
            for (int px = 0; px < size; px++)
            {
                texture.SetPixel(px, py, color);
            }
        }

        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            TileSize);
    }

    private static Sprite CreateCircleSprite(string name, Color color)
    {
        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = name,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), center) <= radius ? color : Color.clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), TileSize);
    }

    private static Color ColorFromHex(string hex)
    {
        return ColorUtility.TryParseHtmlString(hex, out Color color) ? color : Color.white;
    }
}
