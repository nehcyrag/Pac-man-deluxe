using UnityEngine;

public static class SimpleSprites
{
    private const int Size = 64;
    private const float PixelsPerUnit = 64f;

    private static Sprite pacMan;
    private static Sprite pacManHalfOpen;
    private static Sprite pacManClosed;
    private static Sprite ghost;
    private static Sprite ghostWave;
    private static Sprite pinkGhost;
    private static Sprite pinkGhostWave;
    private static Sprite orangeGhost;
    private static Sprite orangeGhostWave;
    private static Sprite cyanGhost;
    private static Sprite cyanGhostWave;
    private static Sprite whiteGhost;
    private static Sprite whiteGhostWave;
    private static Sprite greenGhost;
    private static Sprite greenGhostWave;
    private static Sprite frightenedGhost;
    private static Sprite frightenedGhostWave;
    private static Sprite ghostEyes;
    private static Sprite wall;
    private static Sprite powerPellet;
    private static Sprite itemBackground;
    private static Sprite originalItemBackground;
    private static Sprite energyPellet;
    private static Sprite phantomPellet;
    private static Sprite phantomPacMan;
    private static Sprite bombPellet;
    private static Sprite lightningPellet;
    private static Sprite bomb;
    private static Sprite bombExplosion;
    private static Sprite energyBullet;

    public static Sprite PacMan => pacMan != null ? pacMan : (pacMan = CreatePacMan());
    public static Sprite PacManHalfOpen => pacManHalfOpen != null ? pacManHalfOpen : (pacManHalfOpen = CreatePacMan(new Color(1f, 0.9f, 0.05f, 1f), 14f));
    public static Sprite PacManClosed => pacManClosed != null ? pacManClosed : (pacManClosed = CreatePacMan(new Color(1f, 0.9f, 0.05f, 1f), 0f));
    public static Sprite Ghost => ghost != null ? ghost : (ghost = CreateGhost(new Color(1f, 0.15f, 0.25f, 1f)));
    public static Sprite GhostWave => ghostWave != null ? ghostWave : (ghostWave = CreateGhost(new Color(1f, 0.15f, 0.25f, 1f), true));
    public static Sprite PinkGhost => pinkGhost != null ? pinkGhost : (pinkGhost = CreateGhost(new Color(1f, 0.45f, 0.85f, 1f)));
    public static Sprite PinkGhostWave => pinkGhostWave != null ? pinkGhostWave : (pinkGhostWave = CreateGhost(new Color(1f, 0.45f, 0.85f, 1f), true));
    public static Sprite OrangeGhost => orangeGhost != null ? orangeGhost : (orangeGhost = CreateGhost(new Color(1f, 0.55f, 0.08f, 1f)));
    public static Sprite OrangeGhostWave => orangeGhostWave != null ? orangeGhostWave : (orangeGhostWave = CreateGhost(new Color(1f, 0.55f, 0.08f, 1f), true));
    public static Sprite CyanGhost => cyanGhost != null ? cyanGhost : (cyanGhost = CreateGhost(new Color(0.1f, 0.95f, 1f, 1f)));
    public static Sprite CyanGhostWave => cyanGhostWave != null ? cyanGhostWave : (cyanGhostWave = CreateGhost(new Color(0.1f, 0.95f, 1f, 1f), true));
    public static Sprite WhiteGhost => whiteGhost != null ? whiteGhost : (whiteGhost = CreateGhost(Color.white));
    public static Sprite WhiteGhostWave => whiteGhostWave != null ? whiteGhostWave : (whiteGhostWave = CreateGhost(Color.white, true));
    public static Sprite GreenGhost => greenGhost != null ? greenGhost : (greenGhost = CreateGhost(new Color(0.45f, 0.24f, 0.1f, 1f)));
    public static Sprite GreenGhostWave => greenGhostWave != null ? greenGhostWave : (greenGhostWave = CreateGhost(new Color(0.45f, 0.24f, 0.1f, 1f), true));
    public static Sprite FrightenedGhost => frightenedGhost != null ? frightenedGhost : (frightenedGhost = CreateGhost(new Color(0.02f, 0.05f, 0.45f, 1f)));
    public static Sprite FrightenedGhostWave => frightenedGhostWave != null ? frightenedGhostWave : (frightenedGhostWave = CreateGhost(new Color(0.02f, 0.05f, 0.45f, 1f), true));
    public static Sprite GhostEyes => ghostEyes != null ? ghostEyes : (ghostEyes = CreateGhostEyes());
    public static Sprite Wall => wall != null ? wall : (wall = CreateSolid("Wall Sprite", new Color(0.05f, 0.2f, 1f, 1f)));
    public static Sprite PowerPellet => powerPellet != null ? powerPellet : (powerPellet = LoadResourceSprite("Sprites/power_pellet") ?? CreateCircle("Power Pellet Sprite", new Color(1f, 0.78f, 0.12f, 1f)));
    public static Sprite ItemBackground => itemBackground != null ? itemBackground : (itemBackground = CreateCircle("Item Background Sprite", Color.white));
    public static Sprite OriginalItemBackground => originalItemBackground != null ? originalItemBackground : (originalItemBackground = CreateCircle("Original Item Background Sprite", new Color(1f, 0.86f, 0.08f, 0.92f)));
    public static Sprite EnergyPellet => energyPellet != null ? energyPellet : (energyPellet = LoadResourceSprite("Sprites/energy_pellet") ?? CreateCircle("Energy Pellet Sprite", new Color(0.45f, 0.9f, 1f, 1f)));
    public static Sprite PhantomPellet => phantomPellet != null ? phantomPellet : (phantomPellet = LoadResourceSprite("Sprites/phantom_pellet") ?? CreateCircle("Phantom Pellet Sprite", new Color(0.65f, 0.25f, 1f, 1f)));
    public static Sprite PhantomPacMan => phantomPacMan != null ? phantomPacMan : (phantomPacMan = CreatePacMan(new Color(0.65f, 0.25f, 1f, 1f)));
    public static Sprite BombPellet => bombPellet != null ? bombPellet : (bombPellet = LoadResourceSprite("Sprites/bomb_pellet") ?? CreateCircle("Bomb Pellet Sprite", new Color(1f, 0.08f, 0.06f, 1f)));
    public static Sprite LightningPellet => lightningPellet != null ? lightningPellet : (lightningPellet = CreateLightningPellet());
    public static Sprite Bomb => bomb != null ? bomb : (bomb = CreateCircle("Bomb Sprite", new Color(0.9f, 0.02f, 0.02f, 1f)));
    public static Sprite BombExplosion => bombExplosion != null ? bombExplosion : (bombExplosion = CreateSolid("Bomb Explosion Sprite", new Color(1f, 0.22f, 0.08f, 0.85f)));
    public static Sprite EnergyBullet => energyBullet != null ? energyBullet : (energyBullet = CreateEnergyBullet());

    private static Sprite CreatePacMan()
    {
        return CreatePacMan(new Color(1f, 0.9f, 0.05f, 1f), 28f);
    }

    private static Sprite CreatePacMan(Color color)
    {
        return CreatePacMan(color, 28f);
    }

    private static Sprite CreatePacMan(Color color, float mouthHalfAngle)
    {
        Texture2D texture = CreateTexture("Pac-Man Sprite");
        Vector2 center = new Vector2((Size - 1) * 0.5f, (Size - 1) * 0.5f);
        float radius = 29f;
        float mouthAngle = Mathf.Max(0f, mouthHalfAngle);

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                Vector2 point = new Vector2(x, y) - center;
                float distance = point.magnitude;
                float angle = Mathf.Atan2(point.y, point.x) * Mathf.Rad2Deg;
                bool insideMouth = mouthAngle > 0f && angle > -mouthAngle && angle < mouthAngle && point.x > 0f;

                texture.SetPixel(x, y, distance <= radius && !insideMouth ? color : Color.clear);
            }
        }

        texture.Apply();
        return CreateSprite(texture);
    }

    private static Sprite CreateGhost(Color body)
    {
        return CreateGhost(body, false);
    }

    private static Sprite CreateGhost(Color body, bool alternateSkirt)
    {
        Texture2D texture = CreateTexture("Ghost Sprite");
        Color eye = Color.white;
        Color pupil = new Color(0.05f, 0.1f, 0.6f, 1f);
        Vector2 headCenter = new Vector2((Size - 1) * 0.5f, 36f);

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                bool inHead = (new Vector2(x, y) - headCenter).magnitude <= 25f && y >= 28;
                bool inBody = x >= 7 && x <= 56 && y >= 12 && y < 38;
                bool inSkirt = y >= 7 && y < 13 && IsInGhostSkirt(x, alternateSkirt);

                texture.SetPixel(x, y, inHead || inBody || inSkirt ? body : Color.clear);
            }
        }

        DrawCircle(texture, new Vector2(23f, 38f), 7f, eye);
        DrawCircle(texture, new Vector2(41f, 38f), 7f, eye);
        DrawCircle(texture, new Vector2(25f, 38f), 3f, pupil);
        DrawCircle(texture, new Vector2(43f, 38f), 3f, pupil);

        texture.Apply();
        return CreateSprite(texture);
    }

    private static Sprite CreateSolid(string name, Color color)
    {
        Texture2D texture = CreateTexture(name);

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                bool border = x < 4 || x > Size - 5 || y < 4 || y > Size - 5;
                texture.SetPixel(x, y, border ? Color.white : color);
            }
        }

        texture.Apply();
        return CreateSprite(texture);
    }

    private static Sprite CreateEnergyBullet()
    {
        Texture2D texture = CreateTexture("Energy Bullet Sprite");
        Color fill = new Color(1f, 0.9f, 0.05f, 1f);
        Color edge = Color.white;

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                texture.SetPixel(x, y, Color.clear);
            }
        }

        Vector2 a = new Vector2(56f, 32f);
        Vector2 b = new Vector2(10f, 54f);
        Vector2 c = new Vector2(10f, 10f);

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                Vector2 point = new Vector2(x, y);
                if (!IsPointInTriangle(point, a, b, c))
                {
                    continue;
                }

                float edgeDistance = Mathf.Min(DistanceToSegment(point, a, b), Mathf.Min(DistanceToSegment(point, b, c), DistanceToSegment(point, c, a)));
                texture.SetPixel(x, y, edgeDistance <= 2.5f ? edge : fill);
            }
        }

        texture.Apply();
        return CreateSprite(texture);
    }

    private static Sprite CreateLightningPellet()
    {
        Texture2D texture = CreateTexture("Lightning Pellet Sprite");
        Color fill = new Color(0.1f, 0.95f, 1f, 1f);
        Color edge = Color.white;

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                texture.SetPixel(x, y, Color.clear);
            }
        }

        Vector2[] points =
        {
            new Vector2(38f, 4f),
            new Vector2(14f, 34f),
            new Vector2(30f, 34f),
            new Vector2(24f, 60f),
            new Vector2(52f, 25f),
            new Vector2(35f, 25f)
        };

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                Vector2 point = new Vector2(x, y);
                if (!IsPointInPolygon(point, points))
                {
                    continue;
                }

                float edgeDistance = DistanceToPolygon(point, points);
                texture.SetPixel(x, y, edgeDistance <= 2.2f ? edge : fill);
            }
        }

        texture.Apply();
        return CreateSprite(texture);
    }

    private static Sprite CreateCircle(string name, Color color)
    {
        Texture2D texture = CreateTexture(name);
        Vector2 center = new Vector2((Size - 1) * 0.5f, (Size - 1) * 0.5f);
        float radius = 27f;

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                texture.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), center) <= radius ? color : Color.clear);
            }
        }

        texture.Apply();
        return CreateSprite(texture);
    }

    private static Sprite LoadResourceSprite(string resourcePath)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture != null)
        {
            float pixelsPerUnit = Mathf.Max(1f, Mathf.Max(texture.width, texture.height));
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }

        return Resources.Load<Sprite>(resourcePath);
    }

    private static Sprite CreateGhostEyes()
    {
        Texture2D texture = CreateTexture("Ghost Eyes Sprite");
        Color pupil = new Color(0.05f, 0.1f, 0.6f, 1f);

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                texture.SetPixel(x, y, Color.clear);
            }
        }

        DrawCircle(texture, new Vector2(23f, 38f), 7f, Color.white);
        DrawCircle(texture, new Vector2(41f, 38f), 7f, Color.white);
        DrawCircle(texture, new Vector2(25f, 38f), 3f, pupil);
        DrawCircle(texture, new Vector2(43f, 38f), 3f, pupil);

        texture.Apply();
        return CreateSprite(texture);
    }

    private static Texture2D CreateTexture(string name)
    {
        Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            name = name,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        return texture;
    }

    private static Sprite CreateSprite(Texture2D texture)
    {
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, Size, Size),
            new Vector2(0.5f, 0.5f),
            PixelsPerUnit,
            0,
            SpriteMeshType.FullRect);
    }

    private static void DrawCircle(Texture2D texture, Vector2 center, float radius, Color color)
    {
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                if ((new Vector2(x, y) - center).magnitude <= radius)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }
    }

    private static bool IsInGhostSkirt(int x, bool alternate)
    {
        if (alternate)
        {
            return x < 13 || (x >= 20 && x <= 30) || (x >= 38 && x <= 48) || x > 55;
        }

        return x < 17 || x > 47 || (x >= 25 && x <= 39);
    }

    private static bool IsPointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Sign(point, a, b);
        float d2 = Sign(point, b, c);
        float d3 = Sign(point, c, a);

        bool hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
        bool hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(hasNegative && hasPositive);
    }

    private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }

    private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 segment = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(point - a, segment) / segment.sqrMagnitude);
        return Vector2.Distance(point, a + segment * t);
    }

    private static bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            if ((polygon[i].y > point.y) == (polygon[j].y > point.y))
            {
                continue;
            }

            float intersectX = (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x;
            if (point.x < intersectX)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static float DistanceToPolygon(Vector2 point, Vector2[] polygon)
    {
        float minDistance = float.MaxValue;
        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[(i + 1) % polygon.Length];
            minDistance = Mathf.Min(minDistance, DistanceToSegment(point, a, b));
        }

        return minDistance;
    }
}
