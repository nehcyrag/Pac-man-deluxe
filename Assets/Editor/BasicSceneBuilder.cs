using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public static class BasicSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tools/Pac-Man Deluxe/Build Basic Scene")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play Mode before running Tools > Pac-Man Deluxe > Build Basic Scene.");
            return;
        }

        EditorSceneManager.OpenScene(ScenePath);

        EnsureCamera();
        GameObject grid = EnsureGrid();
        GameObject player = EnsurePlayer();
        GameObject ghost = EnsureGhost();
        EnsurePinkGhost();
        EnsureOrangeGhost();
        EnsureCyanGhost();
        EnsureMazeGenerator();
        EnsureGameManager(player, ghost);
        EnsureCanvas();

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Pac-Man Deluxe/Build Basic Scene", true)]
    private static bool CanBuild()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    private static void EnsureCamera()
    {
        Camera camera = Object.FindFirstObjectByType<Camera>();
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.tag = "MainCamera";
        }

        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.orthographic = true;
        camera.orthographicSize = 15.5f;
        camera.backgroundColor = Color.black;
    }

    private static GameObject EnsureGrid()
    {
        GameObject grid = FindOrCreate("Grid");
        Grid gridComponent = GetOrAddComponent<Grid>(grid);
        gridComponent.cellSize = Vector3.one;
        gridComponent.cellLayout = GridLayout.CellLayout.Rectangle;

        GameObject tilemap = GameObject.Find("Tilemap");
        if (tilemap == null)
        {
            tilemap = new GameObject("Tilemap");
        }

        tilemap.transform.SetParent(grid.transform);
        tilemap.transform.localPosition = Vector3.zero;
        tilemap.transform.localRotation = Quaternion.identity;
        tilemap.transform.localScale = Vector3.one;
        GetOrAddComponent<Tilemap>(tilemap);
        GetOrAddComponent<TilemapRenderer>(tilemap);

        return grid;
    }

    private static GameObject EnsurePlayer()
    {
        CleanupExtraPlayerControllers();

        GameObject player = FindOrCreate("Player");
        player.layer = LayerMask.NameToLayer("Default");
        player.transform.position = new Vector3(-0.5f, -7.5f, 0f);
        player.transform.localScale = Vector3.one * 0.8f;
        GetOrAddComponent<PlayerController>(player);
        GetOrAddComponent<Rigidbody2D>(player);
        GetOrAddComponent<CircleCollider2D>(player);

        Rigidbody2D rb = GetOrAddComponent<Rigidbody2D>(player);
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D playerCollider = GetOrAddComponent<CircleCollider2D>(player);
        playerCollider.radius = 0.34f;
        playerCollider.isTrigger = false;

        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(player);
        renderer.sprite = SimpleSprites.PacMan;
        renderer.color = Color.white;

        return player;
    }

    private static GameObject EnsureGhost()
    {
        GameObject ghost = FindOrCreate("Ghost");
        ghost.layer = LayerMask.NameToLayer("Default");
        ghost.transform.position = new Vector3(-1.5f, 1.5f, 0f);
        ghost.transform.localScale = Vector3.one * 0.8f;
        GetOrAddComponent<GhostController>(ghost);
        GetOrAddComponent<Rigidbody2D>(ghost);
        GetOrAddComponent<CircleCollider2D>(ghost);

        Rigidbody2D rb = GetOrAddComponent<Rigidbody2D>(ghost);
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D ghostCollider = GetOrAddComponent<CircleCollider2D>(ghost);
        ghostCollider.radius = 0.45f;
        ghostCollider.isTrigger = false;

        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(ghost);
        renderer.sprite = SimpleSprites.Ghost;
        renderer.color = Color.white;

        return ghost;
    }

    private static GameObject EnsurePinkGhost()
    {
        GameObject ghost = FindOrCreate("PinkGhost");
        ghost.layer = LayerMask.NameToLayer("Default");
        ghost.transform.position = new Vector3(-0.5f, 1.5f, 0f);
        ghost.transform.localScale = Vector3.one * 0.8f;

        GhostController controller = GetOrAddComponent<GhostController>(ghost);
        controller.SetTargetMode(GhostController.TargetMode.FourTilesAhead);
        GetOrAddComponent<Rigidbody2D>(ghost);
        GetOrAddComponent<CircleCollider2D>(ghost);

        Rigidbody2D rb = GetOrAddComponent<Rigidbody2D>(ghost);
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D ghostCollider = GetOrAddComponent<CircleCollider2D>(ghost);
        ghostCollider.radius = 0.42f;
        ghostCollider.isTrigger = false;

        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(ghost);
        renderer.sprite = SimpleSprites.PinkGhost;
        renderer.color = Color.white;

        return ghost;
    }

    private static GameObject EnsureOrangeGhost()
    {
        GameObject ghost = FindOrCreate("OrangeGhost");
        ghost.layer = LayerMask.NameToLayer("Default");
        ghost.transform.position = new Vector3(0.5f, 1.5f, 0f);
        ghost.transform.localScale = Vector3.one * 0.8f;

        GhostController controller = GetOrAddComponent<GhostController>(ghost);
        controller.SetTargetMode(GhostController.TargetMode.ChaseUntilCloseThenRandom);
        GetOrAddComponent<Rigidbody2D>(ghost);
        GetOrAddComponent<CircleCollider2D>(ghost);

        Rigidbody2D rb = GetOrAddComponent<Rigidbody2D>(ghost);
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D ghostCollider = GetOrAddComponent<CircleCollider2D>(ghost);
        ghostCollider.radius = 0.42f;
        ghostCollider.isTrigger = false;

        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(ghost);
        renderer.sprite = SimpleSprites.OrangeGhost;
        renderer.color = Color.white;

        return ghost;
    }

    private static GameObject EnsureCyanGhost()
    {
        GameObject ghost = FindOrCreate("CyanGhost");
        ghost.layer = LayerMask.NameToLayer("Default");
        ghost.transform.position = new Vector3(1.5f, 1.5f, 0f);
        ghost.transform.localScale = Vector3.one * 0.8f;

        GhostController controller = GetOrAddComponent<GhostController>(ghost);
        controller.SetTargetMode(GhostController.TargetMode.InkyVector);
        GetOrAddComponent<Rigidbody2D>(ghost);
        GetOrAddComponent<CircleCollider2D>(ghost);

        Rigidbody2D rb = GetOrAddComponent<Rigidbody2D>(ghost);
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D ghostCollider = GetOrAddComponent<CircleCollider2D>(ghost);
        ghostCollider.radius = 0.42f;
        ghostCollider.isTrigger = false;

        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(ghost);
        renderer.sprite = SimpleSprites.CyanGhost;
        renderer.color = Color.white;

        return ghost;
    }

    private static void EnsureMazeGenerator()
    {
        GameObject oldWalls = GameObject.Find("Walls");
        if (oldWalls != null)
        {
            Object.DestroyImmediate(oldWalls);
        }

        GameObject mazeGenerator = FindOrCreate("MazeGenerator");
        GetOrAddComponent<MazeGenerator>(mazeGenerator);
    }

    private static void EnsureWalls()
    {
        int wallLayer = LayerMask.NameToLayer("Wall");
        if (wallLayer < 0)
        {
            Debug.LogWarning("Wall layer was not found. Add a layer named 'Wall' in Project Settings > Tags and Layers.");
            return;
        }

        GameObject walls = FindOrCreate("Walls");
        walls.layer = wallLayer;
        ClearChildren(walls.transform);

        const float thin = 0.16f;

        H("Outer Top", walls.transform, -5.6f, 5.6f, 5.6f, wallLayer, thin);
        H("Outer Bottom", walls.transform, -5.6f, 5.6f, -7f, wallLayer, thin);
        V("Outer Left Upper", walls.transform, -5.6f, 1.4f, 5.6f, wallLayer, thin);
        V("Outer Left Lower", walls.transform, -5.6f, -7f, -1.4f, wallLayer, thin);
        V("Outer Right Upper", walls.transform, 5.6f, 1.4f, 5.6f, wallLayer, thin);
        V("Outer Right Lower", walls.transform, 5.6f, -7f, -1.4f, wallLayer, thin);
        H("Tunnel Left Top", walls.transform, -5.6f, -4.2f, 1.4f, wallLayer, thin);
        H("Tunnel Left Bottom", walls.transform, -5.6f, -4.2f, -1.4f, wallLayer, thin);
        H("Tunnel Right Top", walls.transform, 4.2f, 5.6f, 1.4f, wallLayer, thin);
        H("Tunnel Right Bottom", walls.transform, 4.2f, 5.6f, -1.4f, wallLayer, thin);

        Box("Top Left Box", walls.transform, -4.55f, 3.85f, 1.1f, 0.7f, wallLayer, thin);
        Box("Top Right Box", walls.transform, 4.55f, 3.85f, 1.1f, 0.7f, wallLayer, thin);
        H("Top Center Left", walls.transform, -2.8f, -1.4f, 4.2f, wallLayer, thin);
        H("Top Center Right", walls.transform, 1.4f, 2.8f, 4.2f, wallLayer, thin);
        V("Top Center Stem", walls.transform, 0f, 4.2f, 5.6f, wallLayer, thin);

        V("Upper Left Pillar", walls.transform, -2.8f, 1.4f, 2.8f, wallLayer, thin);
        V("Upper Right Pillar", walls.transform, 2.8f, 1.4f, 2.8f, wallLayer, thin);
        H("Upper Left Bar", walls.transform, -1.4f, -0.7f, 2.8f, wallLayer, thin);
        H("Upper Right Bar", walls.transform, 0.7f, 1.4f, 2.8f, wallLayer, thin);

        H("Ghost House Top Left", walls.transform, -1.4f, -0.45f, 1.4f, wallLayer, thin);
        H("Ghost House Top Right", walls.transform, 0.45f, 1.4f, 1.4f, wallLayer, thin);
        H("Ghost House Bottom", walls.transform, -1.4f, 1.4f, 0f, wallLayer, thin);
        V("Ghost House Left", walls.transform, -1.4f, 0f, 1.4f, wallLayer, thin);
        V("Ghost House Right", walls.transform, 1.4f, 0f, 1.4f, wallLayer, thin);

        H("Lower Center Lane", walls.transform, -0.9f, 0.9f, -2.8f, wallLayer, thin);

        H("Lower Left Inner Top", walls.transform, -3.45f, -2.35f, -2.3f, wallLayer, thin);
        V("Lower Left Inner Side", walls.transform, -2.45f, -4.0f, -2.75f, wallLayer, thin);
        H("Lower Right Inner Top", walls.transform, 2.35f, 3.45f, -2.3f, wallLayer, thin);
        V("Lower Right Inner Side", walls.transform, 2.45f, -4.0f, -2.75f, wallLayer, thin);

        H("Lower Left Outer Top", walls.transform, -5.6f, -4.45f, -2f, wallLayer, thin);
        V("Lower Left Outer Step", walls.transform, -4.45f, -3.35f, -2f, wallLayer, thin);
        H("Lower Left Outer Bottom", walls.transform, -5.6f, -5.05f, -3.35f, wallLayer, thin);
        H("Lower Right Outer Top", walls.transform, 4.45f, 5.6f, -2f, wallLayer, thin);
        V("Lower Right Outer Step", walls.transform, 4.45f, -3.35f, -2f, wallLayer, thin);
        H("Lower Right Outer Bottom", walls.transform, 5.05f, 5.6f, -3.35f, wallLayer, thin);

        H("Lower Center T Top", walls.transform, -1.4f, 1.4f, -3.75f, wallLayer, thin);
        V("Lower Center T Stem", walls.transform, 0f, -4.9f, -3.75f, wallLayer, thin);
        H("Bottom Left Long", walls.transform, -4.9f, -1.4f, -5.35f, wallLayer, thin);
        H("Bottom Right Long", walls.transform, 1.4f, 4.9f, -5.35f, wallLayer, thin);
    }

    private static void H(string name, Transform parent, float x1, float x2, float y, int layer, float thickness)
    {
        float min = Mathf.Min(x1, x2);
        float max = Mathf.Max(x1, x2);
        CreateWall(name, parent, new Vector2((min + max) * 0.5f, y), new Vector2(max - min + thickness, thickness), layer);
    }

    private static void V(string name, Transform parent, float x, float y1, float y2, int layer, float thickness)
    {
        float min = Mathf.Min(y1, y2);
        float max = Mathf.Max(y1, y2);
        CreateWall(name, parent, new Vector2(x, (min + max) * 0.5f), new Vector2(thickness, max - min + thickness), layer);
    }

    private static void Box(string name, Transform parent, float centerX, float centerY, float width, float height, int layer, float thickness)
    {
        float left = centerX - width * 0.5f;
        float right = centerX + width * 0.5f;
        float bottom = centerY - height * 0.5f;
        float top = centerY + height * 0.5f;

        H(name + " Top", parent, left, right, top, layer, thickness);
        H(name + " Bottom", parent, left, right, bottom, layer, thickness);
        V(name + " Left", parent, left, bottom, top, layer, thickness);
        V(name + " Right", parent, right, bottom, top, layer, thickness);
    }

    private static void CreateWall(string name, Transform parent, Vector2 position, Vector2 size, int layer)
    {
        GameObject wall = new GameObject(name);

        wall.layer = layer;
        wall.transform.SetParent(parent);
        wall.transform.position = position;
        wall.transform.localRotation = Quaternion.identity;
        wall.transform.localScale = Vector3.one;

        BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(wall);

        collider.size = size;
        collider.isTrigger = false;

        SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(wall);

        renderer.sprite = SimpleSprites.Wall;
        renderer.color = Color.white;
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = size;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }

    private static void EnsureGameManager(GameObject player, GameObject ghost)
    {
        GameObject gameManager = FindOrCreate("GameManager");
        GameManager manager = GetOrAddComponent<GameManager>(gameManager);
        SerializedObject serializedManager = new SerializedObject(manager);
        serializedManager.FindProperty("player").objectReferenceValue = player.GetComponent<PlayerController>();
        serializedManager.FindProperty("ghost").objectReferenceValue = ghost.GetComponent<GhostController>();
        serializedManager.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureCanvas()
    {
        GameObject canvasObject = FindOrCreate("Canvas");
        Canvas canvas = GetOrAddComponent<Canvas>(canvasObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        GetOrAddComponent<CanvasScaler>(canvasObject);
        GetOrAddComponent<GraphicRaycaster>(canvasObject);
    }

    private static void CleanupExtraPlayerControllers()
    {
        foreach (PlayerController controller in Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (controller.gameObject.name != "Player")
            {
                Object.DestroyImmediate(controller.gameObject);
            }
        }
    }

    private static GameObject FindOrCreate(string name)
    {
        GameObject found = GameObject.Find(name);
        if (found != null)
        {
            return found;
        }

        return new GameObject(name);
    }

    private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }

        return component;
    }
}
