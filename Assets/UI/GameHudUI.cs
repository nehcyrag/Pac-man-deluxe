using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameHudUI : MonoBehaviour
{
    private Text scoreText;
    private Text levelText;
    private Text livesText;
    private Text p1LabelText;
    private Text p1HeartsText;
    private Text p2LabelText;
    private Text p2HeartsText;
    private Text invincibleText;
    private Image itemIcon;
    private Image p1ItemIcon;
    private Image p2ItemIcon;
    private Sprite energyItemSprite;
    private Sprite bombItemSprite;
    private GameManager gameManager;
    private Button pauseButton;
    private Button quitButton;
    private Button soundButton;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureHudExists()
    {
        if (FindFirstObjectByType<GameHudUI>() != null)
        {
            return;
        }

        new GameObject("GameHudUI").AddComponent<GameHudUI>();
    }

    private void Awake()
    {
        EnsureEventSystem();
        BuildHud();
    }

    private void Update()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (gameManager == null)
        {
            return;
        }

        scoreText.text = "SCORE " + gameManager.Score.ToString("000000");
        levelText.text = "LEVEL " + gameManager.Level;
        UpdateLivesDisplay();
        UpdateItemDisplay();
        invincibleText.text = gameManager.IsInvincible ? "INVINCIBLE" : "";
        pauseButton.GetComponentInChildren<Text>().text = gameManager.IsPaused ? "RESUME" : "PAUSE";
        UpdateSoundButtonIcon(soundButton);
    }

    private void BuildHud()
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

        scoreText = CreateHudText("Score Text", canvas.transform, TextAnchor.UpperLeft);
        RectTransform scoreRect = scoreText.GetComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(0f, 1f);
        scoreRect.anchorMax = new Vector2(0f, 1f);
        scoreRect.pivot = new Vector2(0f, 1f);
        scoreRect.anchoredPosition = new Vector2(14f, -10f);
        scoreRect.sizeDelta = new Vector2(220f, 36f);

        livesText = CreateHudText("Lives Text", canvas.transform, TextAnchor.UpperLeft);
        livesText.color = new Color(1f, 0.08f, 0.08f, 1f);
        RectTransform livesRect = livesText.GetComponent<RectTransform>();
        livesRect.anchorMin = new Vector2(0f, 1f);
        livesRect.anchorMax = new Vector2(0f, 1f);
        livesRect.pivot = new Vector2(0f, 1f);
        livesRect.anchoredPosition = new Vector2(14f, -42f);
        livesRect.sizeDelta = new Vector2(240f, 34f);

        itemIcon = CreateItemIcon("Item Icon", canvas.transform);
        RectTransform itemRect = itemIcon.GetComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0f, 1f);
        itemRect.anchorMax = new Vector2(0f, 1f);
        itemRect.pivot = new Vector2(0f, 1f);
        itemRect.anchoredPosition = new Vector2(14f, -78f);
        itemRect.sizeDelta = new Vector2(32f, 32f);

        p1LabelText = CreateHudText("P1 Label Text", canvas.transform, TextAnchor.UpperLeft);
        RectTransform p1LabelRect = p1LabelText.GetComponent<RectTransform>();
        p1LabelRect.anchorMin = new Vector2(0f, 1f);
        p1LabelRect.anchorMax = new Vector2(0f, 1f);
        p1LabelRect.pivot = new Vector2(0f, 1f);
        p1LabelRect.anchoredPosition = new Vector2(14f, -42f);
        p1LabelRect.sizeDelta = new Vector2(34f, 34f);

        p1HeartsText = CreateHudText("P1 Hearts Text", canvas.transform, TextAnchor.UpperLeft);
        p1HeartsText.color = new Color(1f, 0.08f, 0.08f, 1f);
        RectTransform p1HeartsRect = p1HeartsText.GetComponent<RectTransform>();
        p1HeartsRect.anchorMin = new Vector2(0f, 1f);
        p1HeartsRect.anchorMax = new Vector2(0f, 1f);
        p1HeartsRect.pivot = new Vector2(0f, 1f);
        p1HeartsRect.anchoredPosition = new Vector2(48f, -42f);
        p1HeartsRect.sizeDelta = new Vector2(76f, 34f);

        p1ItemIcon = CreateItemIcon("P1 Item Icon", canvas.transform);
        RectTransform p1ItemRect = p1ItemIcon.GetComponent<RectTransform>();
        p1ItemRect.anchorMin = new Vector2(0f, 1f);
        p1ItemRect.anchorMax = new Vector2(0f, 1f);
        p1ItemRect.pivot = new Vector2(0f, 1f);
        p1ItemRect.anchoredPosition = new Vector2(48f, -78f);
        p1ItemRect.sizeDelta = new Vector2(30f, 30f);

        p2LabelText = CreateHudText("P2 Label Text", canvas.transform, TextAnchor.UpperLeft);
        RectTransform p2LabelRect = p2LabelText.GetComponent<RectTransform>();
        p2LabelRect.anchorMin = new Vector2(0f, 1f);
        p2LabelRect.anchorMax = new Vector2(0f, 1f);
        p2LabelRect.pivot = new Vector2(0f, 1f);
        p2LabelRect.anchoredPosition = new Vector2(128f, -42f);
        p2LabelRect.sizeDelta = new Vector2(34f, 34f);

        p2HeartsText = CreateHudText("P2 Hearts Text", canvas.transform, TextAnchor.UpperLeft);
        p2HeartsText.color = new Color(1f, 0.08f, 0.08f, 1f);
        RectTransform p2HeartsRect = p2HeartsText.GetComponent<RectTransform>();
        p2HeartsRect.anchorMin = new Vector2(0f, 1f);
        p2HeartsRect.anchorMax = new Vector2(0f, 1f);
        p2HeartsRect.pivot = new Vector2(0f, 1f);
        p2HeartsRect.anchoredPosition = new Vector2(162f, -42f);
        p2HeartsRect.sizeDelta = new Vector2(76f, 34f);

        p2ItemIcon = CreateItemIcon("P2 Item Icon", canvas.transform);
        RectTransform p2ItemRect = p2ItemIcon.GetComponent<RectTransform>();
        p2ItemRect.anchorMin = new Vector2(0f, 1f);
        p2ItemRect.anchorMax = new Vector2(0f, 1f);
        p2ItemRect.pivot = new Vector2(0f, 1f);
        p2ItemRect.anchoredPosition = new Vector2(162f, -78f);
        p2ItemRect.sizeDelta = new Vector2(30f, 30f);

        invincibleText = CreateHudText("Invincible Text", canvas.transform, TextAnchor.UpperLeft);
        RectTransform invincibleRect = invincibleText.GetComponent<RectTransform>();
        invincibleRect.anchorMin = new Vector2(0f, 1f);
        invincibleRect.anchorMax = new Vector2(0f, 1f);
        invincibleRect.pivot = new Vector2(0f, 1f);
        invincibleRect.anchoredPosition = new Vector2(14f, -112f);
        invincibleRect.sizeDelta = new Vector2(190f, 34f);

        energyItemSprite = LoadItemSprite("Sprites/energy_pellet");
        bombItemSprite = LoadItemSprite("Sprites/bomb_pellet");
        if (energyItemSprite == null)
        {
            energyItemSprite = SimpleSprites.EnergyPellet;
        }

        if (bombItemSprite == null)
        {
            bombItemSprite = SimpleSprites.BombPellet;
        }

        levelText = CreateHudText("Level Text", canvas.transform, TextAnchor.UpperRight);
        RectTransform levelRect = levelText.GetComponent<RectTransform>();
        levelRect.anchorMin = new Vector2(1f, 1f);
        levelRect.anchorMax = new Vector2(1f, 1f);
        levelRect.pivot = new Vector2(1f, 1f);
        levelRect.anchoredPosition = new Vector2(-14f, -10f);
        levelRect.sizeDelta = new Vector2(180f, 36f);

        pauseButton = CreateHudButton("Pause Button", canvas.transform, "PAUSE");
        RectTransform pauseRect = pauseButton.GetComponent<RectTransform>();
        pauseRect.anchorMin = new Vector2(1f, 1f);
        pauseRect.anchorMax = new Vector2(1f, 1f);
        pauseRect.pivot = new Vector2(1f, 1f);
        pauseRect.anchoredPosition = new Vector2(-14f, -52f);
        pauseRect.sizeDelta = new Vector2(108f, 34f);
        pauseButton.onClick.AddListener(TogglePause);

        quitButton = CreateHudButton("Quit Button", canvas.transform, "QUIT");
        RectTransform quitRect = quitButton.GetComponent<RectTransform>();
        quitRect.anchorMin = new Vector2(1f, 1f);
        quitRect.anchorMax = new Vector2(1f, 1f);
        quitRect.pivot = new Vector2(1f, 1f);
        quitRect.anchoredPosition = new Vector2(-130f, -52f);
        quitRect.sizeDelta = new Vector2(92f, 34f);
        quitButton.onClick.AddListener(ShowResults);

        soundButton = CreateSoundButton("Sound Button", canvas.transform);
        RectTransform soundRect = soundButton.GetComponent<RectTransform>();
        soundRect.anchorMin = new Vector2(0f, 0f);
        soundRect.anchorMax = new Vector2(0f, 0f);
        soundRect.pivot = new Vector2(0f, 0f);
        soundRect.anchoredPosition = new Vector2(14f, 14f);
        soundRect.sizeDelta = new Vector2(44f, 44f);
        soundButton.onClick.AddListener(ToggleSound);
    }

    private static Text CreateHudText(string name, Transform parent, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 22;
        text.color = new Color(1f, 0.78f, 0.12f, 1f);
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    private static Image CreateItemIcon(string name, Transform parent)
    {
        GameObject iconObject = new GameObject(name);
        iconObject.transform.SetParent(parent, false);

        Image image = iconObject.AddComponent<Image>();
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.gameObject.SetActive(false);
        return image;
    }

    private static Sprite LoadItemSprite(string resourcePath)
    {
        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
        {
            return sprite;
        }

        Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
        return sprites != null && sprites.Length > 0 ? sprites[0] : null;
    }

    private static string BuildHearts(int count)
    {
        count = Mathf.Max(0, count);
        return new string('♥', count);
    }

    private void UpdateLivesDisplay()
    {
        bool twoPlayerMode = gameManager.CurrentMode == GameManager.GameMode.TwoPlayer;
        livesText.gameObject.SetActive(!twoPlayerMode);
        itemIcon.gameObject.SetActive(!twoPlayerMode && itemIcon.sprite != null);
        p1LabelText.gameObject.SetActive(twoPlayerMode);
        p1HeartsText.gameObject.SetActive(twoPlayerMode);
        p2LabelText.gameObject.SetActive(twoPlayerMode);
        p2HeartsText.gameObject.SetActive(twoPlayerMode);

        if (twoPlayerMode)
        {
            p1LabelText.text = "P1";
            p1HeartsText.text = BuildHearts(gameManager.Lives);
            p2LabelText.text = "P2";
            p2HeartsText.text = BuildHearts(gameManager.SecondPlayerLives);
            return;
        }

        livesText.text = BuildHearts(gameManager.Lives);
    }

    private void UpdateItemDisplay()
    {
        bool twoPlayerMode = gameManager.CurrentMode == GameManager.GameMode.TwoPlayer;

        if (twoPlayerMode)
        {
            SetItemIcon(itemIcon, null);
            SetItemIcon(p1ItemIcon, GetHeldItemSprite(gameManager.Player));
            SetItemIcon(p2ItemIcon, GetHeldItemSprite(gameManager.SecondPlayer));
            return;
        }

        SetItemIcon(itemIcon, GetHeldItemSprite(gameManager.Player));
        SetItemIcon(p1ItemIcon, null);
        SetItemIcon(p2ItemIcon, null);
    }

    private Sprite GetHeldItemSprite(PlayerController player)
    {
        if (player == null)
        {
            return null;
        }

        if (player.HasBulletAmmo)
        {
            return energyItemSprite;
        }

        if (player.HasBombAmmo)
        {
            return bombItemSprite;
        }

        return null;
    }

    private static void SetItemIcon(Image icon, Sprite sprite)
    {
        if (icon == null)
        {
            return;
        }

        icon.sprite = sprite;
        icon.gameObject.SetActive(sprite != null);
    }

    private static Button CreateHudButton(string name, Transform parent, string label)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.05f, 0.12f, 1f, 0.9f);

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.14f, 0.28f, 1f, 1f);
        colors.pressedColor = new Color(1f, 0.78f, 0.12f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        Text text = CreateHudText("Label", buttonObject.transform, TextAnchor.MiddleCenter);
        text.text = label;
        text.fontSize = 16;
        text.color = Color.white;

        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    private static Button CreateSoundButton(string name, Transform parent)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(1f, 0.86f, 0f, 0.92f);

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.82f);
        colors.pressedColor = new Color(0.14f, 0.28f, 1f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        Image icon = CreateSoundIcon(buttonObject.transform);
        icon.sprite = AudioListener.volume > 0f ? SoundToggleIconSprites.SoundOn : SoundToggleIconSprites.SoundOff;
        return button;
    }

    private static Image CreateSoundIcon(Transform parent)
    {
        GameObject iconObject = new GameObject("Icon");
        iconObject.transform.SetParent(parent, false);

        Image icon = iconObject.AddComponent<Image>();
        icon.color = Color.white;
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        RectTransform rect = icon.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(6f, 6f);
        rect.offsetMax = new Vector2(-6f, -6f);
        return icon;
    }

    private void TogglePause()
    {
        GameManager manager = FindFirstObjectByType<GameManager>();
        if (manager != null && manager.HasStarted)
        {
            manager.TogglePause();
        }
    }

    private void ShowResults()
    {
        GameManager manager = FindFirstObjectByType<GameManager>();
        if (manager != null && manager.HasStarted)
        {
            manager.ShowResults();
        }
    }

    private void ToggleSound()
    {
        AudioListener.volume = AudioListener.volume > 0f ? 0f : 1f;
        UpdateSoundButtonIcon(soundButton);
    }

    private static void UpdateSoundButtonIcon(Button button)
    {
        if (button == null)
        {
            return;
        }

        Transform iconTransform = button.transform.Find("Icon");
        Image image = iconTransform != null ? iconTransform.GetComponent<Image>() : button.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = AudioListener.volume > 0f ? SoundToggleIconSprites.SoundOn : SoundToggleIconSprites.SoundOff;
        }
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
