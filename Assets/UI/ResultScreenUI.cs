using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ResultScreenUI : MonoBehaviour
{
    private const string ResultRootName = "Result Screen";
    private const string PauseRootName = "Pause Overlay";

    private GameObject resultRoot;
    private GameObject pauseRoot;
    private Text scoreText;
    private Text levelText;
    private GameManager gameManager;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureResultScreenExists()
    {
        if (FindFirstObjectByType<ResultScreenUI>() != null)
        {
            return;
        }

        new GameObject("ResultScreenUI").AddComponent<ResultScreenUI>();
    }

    private void Awake()
    {
        EnsureEventSystem();
        BuildResultScreen();
        BuildPauseOverlay();
    }

    private void Update()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (gameManager == null || resultRoot.activeSelf)
        {
            return;
        }

        pauseRoot.SetActive(gameManager.IsPaused);
    }

    public void Show(int finalScore, int finalLevel)
    {
        pauseRoot.SetActive(false);
        resultRoot.SetActive(true);
        scoreText.text = "SCORE " + finalScore.ToString("000000");
        levelText.text = "LEVEL " + finalLevel;
    }

    public void Hide()
    {
        resultRoot.SetActive(false);
        pauseRoot.SetActive(false);
    }

    private void Restart()
    {
        Hide();
        GameManager manager = FindFirstObjectByType<GameManager>();
        if (manager != null)
        {
            manager.RestartGame();
        }
    }

    private void MainMenu()
    {
        Hide();
        GameManager manager = FindFirstObjectByType<GameManager>();
        if (manager != null)
        {
            manager.ReturnToMainMenu();
        }
    }

    private void BuildResultScreen()
    {
        Canvas canvas = GetOrCreateCanvas();
        Transform oldRoot = canvas.transform.Find(ResultRootName);
        if (oldRoot != null)
        {
            Destroy(oldRoot.gameObject);
        }

        resultRoot = CreateFullScreenPanel(ResultRootName, canvas.transform, new Color(0f, 0f, 0f, 0.9f));

        Text title = CreateText("Title", resultRoot.transform, "RESULT", 52, new Color(1f, 0.78f, 0.12f, 1f));
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 105f);
        titleRect.sizeDelta = new Vector2(420f, 76f);

        scoreText = CreateText("Score", resultRoot.transform, "SCORE 000000", 34, Color.white);
        RectTransform scoreRect = scoreText.GetComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(0.5f, 0.5f);
        scoreRect.anchorMax = new Vector2(0.5f, 0.5f);
        scoreRect.anchoredPosition = new Vector2(0f, 35f);
        scoreRect.sizeDelta = new Vector2(420f, 50f);

        levelText = CreateText("Level", resultRoot.transform, "LEVEL 1", 28, new Color(0.1f, 0.95f, 1f, 1f));
        RectTransform levelRect = levelText.GetComponent<RectTransform>();
        levelRect.anchorMin = new Vector2(0.5f, 0.5f);
        levelRect.anchorMax = new Vector2(0.5f, 0.5f);
        levelRect.anchoredPosition = new Vector2(0f, -15f);
        levelRect.sizeDelta = new Vector2(320f, 44f);

        Button restartButton = CreateButton("Restart Button", resultRoot.transform, "RESTART");
        RectTransform restartRect = restartButton.GetComponent<RectTransform>();
        restartRect.anchorMin = new Vector2(0.5f, 0.5f);
        restartRect.anchorMax = new Vector2(0.5f, 0.5f);
        restartRect.anchoredPosition = new Vector2(0f, -80f);
        restartRect.sizeDelta = new Vector2(220f, 56f);
        restartButton.onClick.AddListener(Restart);

        Button mainMenuButton = CreateButton("Main Menu Button", resultRoot.transform, "MAIN MENU");
        RectTransform mainMenuRect = mainMenuButton.GetComponent<RectTransform>();
        mainMenuRect.anchorMin = new Vector2(0.5f, 0.5f);
        mainMenuRect.anchorMax = new Vector2(0.5f, 0.5f);
        mainMenuRect.anchoredPosition = new Vector2(0f, -145f);
        mainMenuRect.sizeDelta = new Vector2(220f, 56f);
        mainMenuButton.onClick.AddListener(MainMenu);

        resultRoot.SetActive(false);
    }

    private void BuildPauseOverlay()
    {
        Canvas canvas = GetOrCreateCanvas();
        Transform oldRoot = canvas.transform.Find(PauseRootName);
        if (oldRoot != null)
        {
            Destroy(oldRoot.gameObject);
        }

        pauseRoot = CreateFullScreenPanel(PauseRootName, canvas.transform, new Color(0f, 0f, 0f, 0.45f));

        Text paused = CreateText("Paused", pauseRoot.transform, "PAUSED", 48, Color.white);
        RectTransform pausedRect = paused.GetComponent<RectTransform>();
        pausedRect.anchorMin = new Vector2(0.5f, 0.5f);
        pausedRect.anchorMax = new Vector2(0.5f, 0.5f);
        pausedRect.anchoredPosition = new Vector2(0f, 25f);
        pausedRect.sizeDelta = new Vector2(360f, 70f);

        Text hint = CreateText("Hint", pauseRoot.transform, "T RESUME   Q RESULT   R RESTART", 22, new Color(1f, 0.78f, 0.12f, 1f));
        RectTransform hintRect = hint.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.5f, 0.5f);
        hintRect.anchorMax = new Vector2(0.5f, 0.5f);
        hintRect.anchoredPosition = new Vector2(0f, -30f);
        hintRect.sizeDelta = new Vector2(420f, 40f);

        pauseRoot.SetActive(false);
    }

    private static GameObject CreateFullScreenPanel(string name, Transform parent, Color color)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);

        RectTransform rect = root.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = root.AddComponent<Image>();
        image.color = color;
        return root;
    }

    private static Text CreateText(string name, Transform parent, string content, int fontSize, Color color)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, string label)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.05f, 0.12f, 1f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.14f, 0.28f, 1f, 1f);
        colors.pressedColor = new Color(1f, 0.78f, 0.12f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        Text text = CreateText("Label", buttonObject.transform, label, 24, Color.white);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    private static Canvas GetOrCreateCanvas()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            ConfigureCanvasScaler(scaler != null ? scaler : canvas.gameObject.AddComponent<CanvasScaler>());

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            return canvas;
        }

        GameObject canvasObject = new GameObject("Canvas");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        ConfigureCanvasScaler(canvasObject.AddComponent<CanvasScaler>());
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private static void ConfigureCanvasScaler(CanvasScaler scaler)
    {
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960f, 540f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }
}
