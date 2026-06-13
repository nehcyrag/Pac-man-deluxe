using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StartMenuUI : MonoBehaviour
{
    private const string MenuName = "Start Menu";
    private const string TitleImagePath = "Menu/pacman_deluxe";
    private const string SinglePlayerImagePath = "Menu/single_player_mode";
    private const string TwoPlayerImagePath = "Menu/two_player_mode";
    private const string SinglePlayerHeadImagePath = "Menu/single_player_head";
    private const string TwoPlayerHeadImagePath = "Menu/two_player_head";
    private const string MenuMusicPath = "Audio/menu_background";

    private static Sprite fallbackCircleSprite;
    private static AudioSource menuMusicSource;
    private GameObject menuRoot;
    private Button soundButton;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureStartMenuExists()
    {
        if (FindFirstObjectByType<StartMenuUI>() != null)
        {
            return;
        }

        new GameObject("StartMenuUI").AddComponent<StartMenuUI>();
    }

    private void Awake()
    {
        Time.timeScale = 0f;
        EnsureEventSystem();
        BuildMenu();
        PlayMenuMusic();
    }

    private void BuildMenu()
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

        Transform oldMenu = canvas.transform.Find(MenuName);
        if (oldMenu != null)
        {
            Destroy(oldMenu.gameObject);
        }

        menuRoot = new GameObject(MenuName);
        menuRoot.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = menuRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image background = menuRoot.AddComponent<Image>();
        background.color = new Color(1f, 0.86f, 0f, 1f);

        soundButton = CreateSoundButton("Sound Button", menuRoot.transform);
        RectTransform soundRect = soundButton.GetComponent<RectTransform>();
        soundRect.anchorMin = new Vector2(0f, 0f);
        soundRect.anchorMax = new Vector2(0f, 0f);
        soundRect.pivot = new Vector2(0f, 0f);
        soundRect.anchoredPosition = new Vector2(14f, 14f);
        soundRect.sizeDelta = new Vector2(44f, 44f);
        soundButton.onClick.AddListener(ToggleSound);

        Image title = CreateMenuImage("Title Image", menuRoot.transform, TitleImagePath, new Color(1f, 0.86f, 0f, 1f));
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.05f, 0.5f);
        titleRect.anchorMax = new Vector2(0.95f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 155f);
        titleRect.sizeDelta = new Vector2(0f, 615f);
        title.preserveAspect = true;
        if (title.sprite == null)
        {
            AddImageLabel(title.transform, "PAC-MAN DELUXE", 56, Color.black, 0f);
        }

        Button singlePlayerButton = CreateImageButton(
            "Single Player Button",
            menuRoot.transform,
            SinglePlayerImagePath,
            "Single  player  mode");
        RectTransform singleRect = singlePlayerButton.GetComponent<RectTransform>();
        singleRect.anchorMin = new Vector2(0.5f, 0.5f);
        singleRect.anchorMax = new Vector2(0.5f, 0.5f);
        singleRect.anchoredPosition = new Vector2(0f, -35f);
        singleRect.sizeDelta = new Vector2(320f, 59f);
        singlePlayerButton.onClick.AddListener(() => StartGame(GameManager.GameMode.SinglePlayer));

        Button twoPlayerButton = CreateImageButton(
            "Two Player Button",
            menuRoot.transform,
            TwoPlayerImagePath,
            "Two-player  mode");
        RectTransform twoRect = twoPlayerButton.GetComponent<RectTransform>();
        twoRect.anchorMin = new Vector2(0.5f, 0.5f);
        twoRect.anchorMax = new Vector2(0.5f, 0.5f);
        twoRect.anchoredPosition = new Vector2(0f, -125f);
        twoRect.sizeDelta = new Vector2(320f, 59f);
        twoPlayerButton.onClick.AddListener(() => StartGame(GameManager.GameMode.TwoPlayer));

        GameObject controlsTooltip = CreateControlsTooltip(menuRoot.transform);
        Button helpButton = CreateHelpButton("Controls Help Button", menuRoot.transform, controlsTooltip);
        RectTransform helpRect = helpButton.GetComponent<RectTransform>();
        helpRect.anchorMin = new Vector2(1f, 0f);
        helpRect.anchorMax = new Vector2(1f, 0f);
        helpRect.pivot = new Vector2(1f, 0f);
        helpRect.anchoredPosition = new Vector2(-14f, 14f);
        helpRect.sizeDelta = new Vector2(44f, 44f);
        controlsTooltip.transform.SetAsLastSibling();
        helpButton.transform.SetAsLastSibling();
    }

    private void StartGame(GameManager.GameMode mode)
    {
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.SetGameMode(mode);
            gameManager.ResetRound();
            gameManager.StartLevelCountdown();
        }

        if (menuRoot != null)
        {
            menuRoot.SetActive(false);
        }

        StopMenuMusic();
    }

    public void Show()
    {
        if (menuRoot == null)
        {
            BuildMenu();
        }

        Time.timeScale = 0f;
        menuRoot.SetActive(true);
        PlayMenuMusic();
    }

    private static void PlayMenuMusic()
    {
        if (menuMusicSource == null)
        {
            GameObject audioObject = new GameObject("Menu Background Music");
            DontDestroyOnLoad(audioObject);
            menuMusicSource = audioObject.AddComponent<AudioSource>();
            menuMusicSource.playOnAwake = false;
            menuMusicSource.loop = true;
            menuMusicSource.clip = Resources.Load<AudioClip>(MenuMusicPath);
        }

        if (menuMusicSource.clip != null && !menuMusicSource.isPlaying)
        {
            menuMusicSource.Play();
        }
    }

    private static void StopMenuMusic()
    {
        if (menuMusicSource != null && menuMusicSource.isPlaying)
        {
            menuMusicSource.Stop();
        }
    }

    private static Image CreateMenuImage(string name, Transform parent, string imagePath, Color fallbackColor)
    {
        GameObject imageObject = new GameObject(name);
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.AddComponent<Image>();
        image.sprite = LoadMenuSprite(imagePath);
        image.color = image.sprite == null ? fallbackColor : Color.white;
        image.raycastTarget = false;
        return image;
    }

    private static Button CreateImageButton(string name, Transform parent, string imagePath, string fallbackLabel)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        image.sprite = LoadMenuSprite(imagePath);
        image.color = image.sprite == null ? new Color(0.05f, 0.08f, 0.5f, 1f) : Color.white;
        image.preserveAspect = true;

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.86f);
        colors.pressedColor = new Color(1f, 0.92f, 0.25f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        if (image.sprite == null)
        {
            bool isTwoPlayerButton = fallbackLabel.StartsWith("Two");
            AddImageLabel(buttonObject.transform, fallbackLabel, 19, Color.white, 55f);
            if (isTwoPlayerButton)
            {
                AddTwoPlayerHeads(buttonObject.transform);
            }
            else
            {
                AddFallbackHeads(buttonObject.transform, SinglePlayerHeadImagePath);
            }
        }

        return button;
    }

    private static Button CreateSoundButton(string name, Transform parent)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.72f);

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

    private static Button CreateHelpButton(string name, Transform parent, GameObject tooltip)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.05f, 0.08f, 0.5f, 0.92f);

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.14f, 0.28f, 1f, 1f);
        colors.pressedColor = new Color(1f, 0.92f, 0.25f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        Text label = CreateText("Label", buttonObject.transform, "?", 28, Color.white);
        label.fontStyle = FontStyle.Bold;
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        HelpTooltipHover hover = buttonObject.AddComponent<HelpTooltipHover>();
        hover.SetTooltip(tooltip);
        return button;
    }

    private static GameObject CreateControlsTooltip(Transform parent)
    {
        GameObject tooltip = new GameObject("Controls Tooltip");
        tooltip.transform.SetParent(parent, false);

        Image background = tooltip.AddComponent<Image>();
        background.color = new Color(0.02f, 0.03f, 0.16f, 0.94f);
        background.raycastTarget = false;

        RectTransform rect = tooltip.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-14f, 66f);
        rect.sizeDelta = new Vector2(330f, 104f);

        Text text = CreateText(
            "Controls Text",
            tooltip.transform,
            "Single Player: Arrow Keys + Enter\nTwo Player P1: Arrow Keys + Enter\nTwo Player P2: WASD + Space",
            18,
            Color.white);
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(14f, 10f);
        textRect.offsetMax = new Vector2(-14f, -10f);

        tooltip.SetActive(false);
        return tooltip;
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

    private static void AddImageLabel(Transform parent, string label, int fontSize, Color color, float leftInset)
    {
        Text text = CreateText("Label", parent, label, fontSize, color);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(leftInset, 0f);
        textRect.offsetMax = new Vector2(-20f, 0f);
    }

    private static void AddTwoPlayerHeads(Transform parent)
    {
        Sprite[] sprites = LoadSplitMenuSprites(TwoPlayerHeadImagePath);
        if (sprites == null || sprites.Length < 2)
        {
            AddFallbackHeads(parent, null, null);
            return;
        }

        for (int i = 0; i < 2; i++)
        {
            GameObject head = new GameObject("Player Head " + (i + 1));
            head.transform.SetParent(parent, false);

            Image image = head.AddComponent<Image>();
            image.sprite = sprites[i];
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;

            RectTransform rect = image.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(38f + i * 41f, 0f);
            rect.sizeDelta = GetHeadImageSize(sprites[i], 51f);
            head.AddComponent<MenuIconSwing>();
        }
    }

    private static void AddFallbackHeads(Transform parent, params string[] imagePaths)
    {
        int count = imagePaths == null || imagePaths.Length == 0 ? 2 : imagePaths.Length;
        for (int i = 0; i < count; i++)
        {
            GameObject head = new GameObject(count == 1 ? "Player Head" : "Player Head " + (i + 1));
            head.transform.SetParent(parent, false);

            Image image = head.AddComponent<Image>();
            Sprite headSprite = imagePaths != null && i < imagePaths.Length && !string.IsNullOrEmpty(imagePaths[i])
                ? LoadMenuSprite(imagePaths[i])
                : null;
            image.sprite = headSprite != null ? headSprite : GetFallbackCircleSprite();
            image.color = headSprite != null
                ? Color.white
                : i == 0 && count > 1 ? new Color(0.78f, 0.82f, 0.95f, 1f) : new Color(1f, 0.92f, 0.05f, 1f);
            image.raycastTarget = false;

            RectTransform rect = image.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(headSprite != null ? 46f : 35f + i * 25f, 0f);
            rect.sizeDelta = GetHeadImageSize(headSprite, 50f);
            head.AddComponent<MenuIconSwing>();
        }
    }

    private static Vector2 GetHeadImageSize(Sprite sprite, float height)
    {
        if (sprite == null)
        {
            return new Vector2(82f, 82f);
        }

        float aspect = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
        return new Vector2(height * aspect, height);
    }

    private static Sprite GetFallbackCircleSprite()
    {
        if (fallbackCircleSprite != null)
        {
            return fallbackCircleSprite;
        }

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.46f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= radius ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        fallbackCircleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return fallbackCircleSprite;
    }

    private static Sprite LoadMenuSprite(string imagePath)
    {
        Texture2D texture = Resources.Load<Texture2D>(imagePath);
        if (texture != null)
        {
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        return Resources.Load<Sprite>(imagePath);
    }

    private static Sprite[] LoadSplitMenuSprites(string imagePath)
    {
        Texture2D texture = Resources.Load<Texture2D>(imagePath);
        if (texture == null)
        {
            Sprite sprite = Resources.Load<Sprite>(imagePath);
            if (sprite == null)
            {
                return null;
            }

            texture = sprite.texture;
        }

        float halfWidth = texture.width * 0.5f;
        return new[]
        {
            Sprite.Create(texture, new Rect(0f, 0f, halfWidth, texture.height), new Vector2(0.5f, 0.5f), 100f),
            Sprite.Create(texture, new Rect(halfWidth, 0f, halfWidth, texture.height), new Vector2(0.5f, 0.5f), 100f)
        };
    }

    private static Text CreateText(string name, Transform parent, string content, int fontSize, Color color)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.text = content;
        text.font = GetDefaultFont();
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static Font GetDefaultFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return font;
    }

    private static void ConfigureCanvasScaler(CanvasScaler scaler)
    {
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960f, 540f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
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
}

public class HelpTooltipHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private GameObject tooltip;

    public void SetTooltip(GameObject value)
    {
        tooltip = value;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltip != null)
        {
            tooltip.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltip != null)
        {
            tooltip.SetActive(false);
        }
    }
}
