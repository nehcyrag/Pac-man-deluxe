using UnityEngine;
using UnityEngine.UI;

public class CountdownUI : MonoBehaviour
{
    private const string RootName = "Countdown Overlay";

    private GameObject root;
    private Text levelText;
    private Text countdownText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureCountdownExists()
    {
        if (FindFirstObjectByType<CountdownUI>() != null)
        {
            return;
        }

        new GameObject("CountdownUI").AddComponent<CountdownUI>();
    }

    private void Awake()
    {
        Build();
        Hide();
    }

    public void Show(string value)
    {
        Show(value, "");
    }

    public void Show(string value, string levelLabel)
    {
        countdownText.text = value;
        if (levelText != null)
        {
            levelText.text = levelLabel;
            levelText.gameObject.SetActive(!string.IsNullOrEmpty(levelLabel));
        }

        root.SetActive(true);
    }

    public void Hide()
    {
        if (root != null)
        {
            root.SetActive(false);
        }
    }

    private void Build()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            ConfigureCanvasScaler(canvasObject.AddComponent<CanvasScaler>());
            canvasObject.AddComponent<GraphicRaycaster>();
        }
        else
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            ConfigureCanvasScaler(scaler != null ? scaler : canvas.gameObject.AddComponent<CanvasScaler>());

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        Transform oldRoot = canvas.transform.Find(RootName);
        if (oldRoot != null)
        {
            Destroy(oldRoot.gameObject);
        }

        root = new GameObject(RootName);
        root.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        levelText = CreateText("Level Text", root.transform, 34, new Color(0.1f, 0.95f, 1f, 1f));
        RectTransform levelRect = levelText.GetComponent<RectTransform>();
        levelRect.anchorMin = new Vector2(0.5f, 0.5f);
        levelRect.anchorMax = new Vector2(0.5f, 0.5f);
        levelRect.anchoredPosition = new Vector2(0f, 78f);
        levelRect.sizeDelta = new Vector2(320f, 54f);

        countdownText = CreateText("Countdown Text", root.transform, 72, new Color(1f, 0.78f, 0.12f, 1f));
        RectTransform textRect = countdownText.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(260f, 130f);
    }

    private static Text CreateText(string name, Transform parent, int fontSize, Color color)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static void ConfigureCanvasScaler(CanvasScaler scaler)
    {
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960f, 540f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }
}
