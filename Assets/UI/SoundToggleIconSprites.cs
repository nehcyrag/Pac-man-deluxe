using UnityEngine;

public static class SoundToggleIconSprites
{
    private const int Size = 64;
    private static Sprite soundOn;
    private static Sprite soundOff;

    public static Sprite SoundOn => soundOn != null ? soundOn : (soundOn = LoadOrCreate("Menu/sound_on", false));
    public static Sprite SoundOff => soundOff != null ? soundOff : (soundOff = LoadOrCreate("Menu/sound_off", true));

    private static Sprite LoadOrCreate(string resourcePath, bool muted)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture != null)
        {
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
        {
            return sprite;
        }

        return CreateSpeakerSprite(muted);
    }

    private static Sprite CreateSpeakerSprite(bool muted)
    {
        Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                texture.SetPixel(x, y, Color.clear);
            }
        }

        Color color = Color.black;
        FillRect(texture, 10, 24, 12, 16, color);
        FillTriangle(texture, new Vector2(22f, 24f), new Vector2(40f, 12f), new Vector2(40f, 52f), color);

        if (muted)
        {
            DrawThickLine(texture, new Vector2(47f, 20f), new Vector2(58f, 44f), 4f, color);
            DrawThickLine(texture, new Vector2(58f, 20f), new Vector2(47f, 44f), 4f, color);
        }
        else
        {
            DrawArc(texture, new Vector2(40f, 32f), 12f, -45f, 45f, 3f, color);
            DrawArc(texture, new Vector2(40f, 32f), 20f, -45f, 45f, 3f, color);
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), Size);
    }

    private static void FillRect(Texture2D texture, int startX, int startY, int width, int height, Color color)
    {
        for (int y = startY; y < startY + height; y++)
        {
            for (int x = startX; x < startX + width; x++)
            {
                texture.SetPixel(x, y, color);
            }
        }
    }

    private static void FillTriangle(Texture2D texture, Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                Vector2 point = new Vector2(x, y);
                if (IsPointInTriangle(point, a, b, c))
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }
    }

    private static void DrawArc(Texture2D texture, Vector2 center, float radius, float startAngle, float endAngle, float thickness, Color color)
    {
        for (float angle = startAngle; angle <= endAngle; angle += 1.5f)
        {
            float radians = angle * Mathf.Deg2Rad;
            Vector2 point = center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
            DrawCircle(texture, point, thickness, color);
        }
    }

    private static void DrawThickLine(Texture2D texture, Vector2 start, Vector2 end, float thickness, Color color)
    {
        Vector2 delta = end - start;
        int steps = Mathf.CeilToInt(delta.magnitude * 2f);
        for (int i = 0; i <= steps; i++)
        {
            Vector2 point = Vector2.Lerp(start, end, i / Mathf.Max(1f, steps));
            DrawCircle(texture, point, thickness, color);
        }
    }

    private static void DrawCircle(Texture2D texture, Vector2 center, float radius, Color color)
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
        int maxX = Mathf.Min(Size - 1, Mathf.CeilToInt(center.x + radius));
        int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
        int maxY = Mathf.Min(Size - 1, Mathf.CeilToInt(center.y + radius));

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (Vector2.Distance(new Vector2(x, y), center) <= radius)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }
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
}
