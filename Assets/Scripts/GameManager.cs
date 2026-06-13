using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static float pelletSoundSuppressedUntil;

    private struct GhostReleaseRule
    {
        public bool releaseAtLevelStart;
        public float pelletRatio;
        public float elapsedSeconds;

        public GhostReleaseRule(bool releaseAtLevelStart, float pelletRatio, float elapsedSeconds)
        {
            this.releaseAtLevelStart = releaseAtLevelStart;
            this.pelletRatio = pelletRatio;
            this.elapsedSeconds = elapsedSeconds;
        }
    }

    public enum GameMode
    {
        SinglePlayer,
        TwoPlayer
    }

    [SerializeField] private PlayerController player;
    [SerializeField] private PlayerController secondPlayer;
    [SerializeField] private GhostController ghost;
    [SerializeField] private GhostController pinkGhost;
    [SerializeField] private GhostController orangeGhost;
    [SerializeField] private GhostController cyanGhost;
    [SerializeField] private GhostController whiteGhost;
    [SerializeField] private GhostController greenGhost;
    [SerializeField] private GameMode currentMode = GameMode.SinglePlayer;
    [SerializeField] private float frightenedDuration = 8f;
    [SerializeField] private float ghostSpeedIncreasePerLevel = 0.35f;
    [SerializeField] private float earlyLevelGhostSpeedMultiplier = 0.9f;
    [SerializeField] private float lateLevelGhostSpeedIncrease = 0.1f;
    [SerializeField] private int countdownSeconds = 3;
    [SerializeField] private int startingLives = 3;
    [SerializeField] private float hitCooldownDuration = 1f;
    [SerializeField] private float playerRespawnDelay = 3f;
    [SerializeField] private float playerRespawnInvincibleDuration = 3f;
    [SerializeField] private float randomPelletSpawnInterval = 10f;
    [SerializeField] private float powerPelletScale = 0.8f;
    [SerializeField] private float energyPelletScale = 0.8f;
    [SerializeField] private float phantomPelletScale = 0.8f;
    [SerializeField] private float bombPelletScale = 0.8f;
    [SerializeField] private float lightningPelletScale = 0.8f;
    [SerializeField] private float randomPelletIconScaleMultiplier = 1.5f;
    [SerializeField] private float phantomDecoyDuration = 10f;
    [SerializeField] private float lightningLinkDuration = 6f;

    private int score;
    private int level = 1;
    private int lives;
    private int secondPlayerLives;
    private int remainingCollectibles;
    private int levelCollectibleCount;
    private bool isPaused;
    private bool isGameEnded;
    private bool hasStarted;
    private bool isCountingDown;
    private bool isInvincible;
    private float levelElapsedTime;
    private float hitCooldownTimer;
    private float randomPelletSpawnTimer;
    private float powerPelletTimer;
    private Coroutine countdownRoutine;
    private GameObject activePhantomDecoy;
    private GameObject activeLightningLink;
    private float phantomDecoyTimer;
    private AudioClip pelletEatClip;
    private AudioClip powerPelletClip;
    private AudioClip phantomPelletClip;
    private AudioClip lightningPelletClip;
    private AudioClip playerDeathClip;
    private AudioClip gameOverClip;
    private AudioClip nextLevelClip;
    private AudioSource audioSource;

    public PlayerController Player => player;
    public PlayerController SecondPlayer => secondPlayer;
    public GhostController Ghost => ghost;
    public GhostController PinkGhost => pinkGhost;
    public GhostController OrangeGhost => orangeGhost;
    public GhostController CyanGhost => cyanGhost;
    public GhostController WhiteGhost => whiteGhost;
    public GhostController GreenGhost => greenGhost;
    public GameMode CurrentMode => currentMode;
    public int Score => score;
    public int Level => level;
    public int Lives => lives;
    public int SecondPlayerLives => secondPlayerLives;
    public int RemainingCollectibles => remainingCollectibles;
    public bool IsPaused => isPaused;
    public bool IsGameEnded => isGameEnded;
    public bool HasStarted => hasStarted;
    public bool IsCountingDown => isCountingDown;
    public bool IsInvincible => isInvincible;
    public bool HasPhantomDecoy => activePhantomDecoy != null;
    public Vector2 PhantomDecoyPosition => activePhantomDecoy != null ? activePhantomDecoy.transform.position : Vector2.zero;

    private void Awake()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
        }

        ConfigurePrimaryPlayer();
        EnsureSecondPlayer();

        if (ghost == null)
        {
            ghost = FindGhostByName("Ghost");
        }

        EnsurePinkGhost();
        EnsureOrangeGhost();
        EnsureCyanGhost();
        EnsureWhiteGhost();
        EnsureGreenGhost();
        ApplyGameModePlayers();
        ConfigureAudio();

        lives = startingLives;
        secondPlayerLives = startingLives;
        ResetRandomPelletSpawnTimer();
    }

    private void Start()
    {
        if (remainingCollectibles <= 0)
        {
            remainingCollectibles = FindObjectsByType<Pellet>(FindObjectsSortMode.None).Length;
            levelCollectibleCount = remainingCollectibles;
        }

        ApplyGhostLevelSpeed();
        ConfigureGhostReleaseRules();
        ApplyGameModePlayers();
        ApplyPlayerLifeState();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) && hasStarted)
        {
            ResultScreenUI resultScreen = FindFirstObjectByType<ResultScreenUI>();
            if (resultScreen != null)
            {
                resultScreen.Hide();
            }

            RestartGame();
            return;
        }

        if (!hasStarted || isGameEnded || isCountingDown)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            TogglePause();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            ShowResults();
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            SkipToNextLevel();
            return;
        }

        if (Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.F9))
        {
            ToggleInvincible();
        }

        if (hitCooldownTimer > 0f)
        {
            hitCooldownTimer -= Time.deltaTime;
        }

        levelElapsedTime += Time.deltaTime;
        UpdateGhostReleaseRules();
        TickPowerPelletTimer();
        TickPhantomDecoy();
        TickRandomPelletSpawn();
    }

    public void ResetRound(bool resetGhostReleaseTimer = true)
    {
        ApplyGhostLevelSpeed();

        if (player != null)
        {
            player.ResetPlayer();
        }

        if (secondPlayer != null)
        {
            secondPlayer.ResetPlayer();
        }

        if (ghost != null)
        {
            ghost.ResetGhost();
        }

        if (pinkGhost != null)
        {
            pinkGhost.ResetGhost();
        }

        if (orangeGhost != null)
        {
            orangeGhost.ResetGhost();
        }

        if (cyanGhost != null)
        {
            cyanGhost.ResetGhost();
        }

        if (whiteGhost != null)
        {
            whiteGhost.ResetGhost();
        }

        if (greenGhost != null)
        {
            greenGhost.ResetGhost();
        }

        ClearRandomPellets();
        ClearPowerPelletEffect();
        ClearPhantomDecoy();
        ClearLightningLink();
        ClearPlacedBombs();
        ResetRandomPelletSpawnTimer();
        if (resetGhostReleaseTimer)
        {
            levelElapsedTime = 0f;
        }

        ConfigureGhostReleaseRules();
        ApplyGameModePlayers();
    }

    public void SetGameMode(GameMode mode)
    {
        currentMode = mode;
        hasStarted = true;
        isPaused = false;
        isGameEnded = false;
        lives = startingLives;
        secondPlayerLives = startingLives;
        isInvincible = false;
        hitCooldownTimer = 0f;
        levelElapsedTime = 0f;
        ClearRandomPellets();
        ClearPowerPelletEffect();
        ClearPhantomDecoy();
        ClearLightningLink();
        ClearPlacedBombs();
        ResetRandomPelletSpawnTimer();
        ApplyGameModePlayers();
    }

    public void TogglePause()
    {
        if (isGameEnded || isCountingDown)
        {
            return;
        }

        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
    }

    public void ShowResults()
    {
        StopCountdown();
        isGameEnded = true;
        isPaused = false;
        Time.timeScale = 0f;

        ResultScreenUI resultScreen = FindFirstObjectByType<ResultScreenUI>();
        if (resultScreen != null)
        {
            resultScreen.Show(score, level);
        }
    }

    public void RestartGame()
    {
        score = 0;
        level = 1;
        lives = startingLives;
        secondPlayerLives = startingLives;
        isPaused = false;
        isGameEnded = false;
        hasStarted = true;
        isInvincible = false;
        hitCooldownTimer = 0f;
        levelElapsedTime = 0f;
        ClearRandomPellets();
        ClearPowerPelletEffect();
        ClearPhantomDecoy();
        ClearLightningLink();
        ClearPlacedBombs();
        ResetRandomPelletSpawnTimer();
        Time.timeScale = 0f;

        MazeGenerator mazeGenerator = FindFirstObjectByType<MazeGenerator>();
        if (mazeGenerator != null)
        {
            mazeGenerator.Generate();
        }

        ResetRound();
        StartLevelCountdown();
    }

    public void ReturnToMainMenu()
    {
        StopCountdown();
        score = 0;
        level = 1;
        lives = startingLives;
        secondPlayerLives = startingLives;
        currentMode = GameMode.SinglePlayer;
        isPaused = false;
        isGameEnded = false;
        hasStarted = false;
        isInvincible = false;
        hitCooldownTimer = 0f;
        levelElapsedTime = 0f;
        ClearRandomPellets();
        ClearPowerPelletEffect();
        ClearPhantomDecoy();
        ClearLightningLink();
        ClearPlacedBombs();
        ResetRandomPelletSpawnTimer();
        MazeGenerator mazeGenerator = FindFirstObjectByType<MazeGenerator>();
        if (mazeGenerator != null)
        {
            mazeGenerator.Generate();
        }

        ResetRound();
        Time.timeScale = 0f;

        StartMenuUI startMenu = FindFirstObjectByType<StartMenuUI>();
        if (startMenu != null)
        {
            startMenu.Show();
        }
    }

    public void ActivatePowerPellet()
    {
        powerPelletTimer = frightenedDuration;
        foreach (GhostController ghostController in FindObjectsByType<GhostController>(FindObjectsSortMode.None))
        {
            ghostController.EnterFrightenedMode();
        }
    }

    public void GrantPlayerBulletAmmo()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
        }

        if (player != null)
        {
            player.GrantBulletAmmo();
        }
    }

    public void GrantPlayerBombAmmo()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
        }

        if (player != null)
        {
            player.GrantBombAmmo();
        }
    }

    public void NotifyPowerPelletEaten(PowerPellet powerPellet)
    {
    }

    public void NotifyEnergyPelletEaten(EnergyPellet energyPellet)
    {
    }

    public void NotifyPhantomPelletEaten(PhantomPellet phantomPellet)
    {
    }

    public void NotifyBombPelletEaten(BombPellet bombPellet)
    {
    }

    public void NotifyLightningPelletEaten(LightningPellet lightningPellet)
    {
    }

    public void ActivatePhantomDecoy(Vector2 position)
    {
        ClearPhantomDecoy();

        activePhantomDecoy = new GameObject("Phantom Pac-Man");
        activePhantomDecoy.transform.position = position;
        activePhantomDecoy.transform.localScale = Vector3.one * 0.8f;

        SpriteRenderer renderer = activePhantomDecoy.AddComponent<SpriteRenderer>();
        renderer.sprite = SimpleSprites.PhantomPacMan;
        renderer.color = Color.white;
        renderer.sortingOrder = 3;

        phantomDecoyTimer = phantomDecoyDuration;
    }

    public void ActivateLightningLink()
    {
        if (currentMode != GameMode.TwoPlayer || player == null || secondPlayer == null)
        {
            return;
        }

        ClearLightningLink();

        activeLightningLink = new GameObject("Lightning Link");
        LightningLinkController lightningLink = activeLightningLink.AddComponent<LightningLinkController>();
        lightningLink.Initialize(player, secondPlayer, lightningLinkDuration);
    }

    public void AddScore(int points)
    {
        score += points;
    }

    public void PlayPelletEatSound(PlayerController eater)
    {
        if (audioSource == null)
        {
            ConfigureAudio();
        }

        if (audioSource == null || Time.unscaledTime < pelletSoundSuppressedUntil)
        {
            return;
        }

        if (pelletEatClip != null)
        {
            audioSource.Stop();
            audioSource.clip = pelletEatClip;
            audioSource.Play();
        }
    }

    public void PlayPowerPelletSound()
    {
        if (audioSource == null)
        {
            ConfigureAudio();
        }

        if (audioSource == null || powerPelletClip == null)
        {
            return;
        }

        SuppressPelletSoundsFor(powerPelletClip.length);
        audioSource.Stop();
        audioSource.clip = powerPelletClip;
        audioSource.Play();
    }

    public void PlayPhantomPelletSound()
    {
        if (audioSource == null)
        {
            ConfigureAudio();
        }

        if (audioSource == null || phantomPelletClip == null)
        {
            return;
        }

        SuppressPelletSoundsFor(phantomPelletClip.length);
        audioSource.Stop();
        audioSource.clip = phantomPelletClip;
        audioSource.Play();
    }

    public void PlayLightningPelletSound()
    {
        if (audioSource == null)
        {
            ConfigureAudio();
        }

        if (audioSource == null || lightningPelletClip == null)
        {
            return;
        }

        SuppressPelletSoundsFor(lightningPelletClip.length);
        audioSource.Stop();
        audioSource.clip = lightningPelletClip;
        audioSource.Play();
    }

    public static void SuppressPelletSoundsFor(float seconds)
    {
        pelletSoundSuppressedUntil = Mathf.Max(pelletSoundSuppressedUntil, Time.unscaledTime + Mathf.Max(0f, seconds));

        GameManager manager = FindFirstObjectByType<GameManager>();
        if (manager != null)
        {
            manager.StopPelletSoundIfPlaying();
        }
    }

    private void StopPelletSoundIfPlaying()
    {
        if (audioSource != null && audioSource.isPlaying && audioSource.clip == pelletEatClip)
        {
            audioSource.Stop();
        }
    }

    public void ToggleInvincible()
    {
        isInvincible = !isInvincible;
    }

    public void HandlePacManHitByGhost()
    {
        HandlePacManHitByGhost(null);
    }

    public void HandlePacManHitByGhost(PlayerController hitPlayer)
    {
        if (isGameEnded || isCountingDown || isInvincible)
        {
            return;
        }

        if (currentMode == GameMode.TwoPlayer)
        {
            HandleTwoPlayerPacManHit(hitPlayer);
            return;
        }

        if (hitCooldownTimer > 0f)
        {
            return;
        }

        hitCooldownTimer = hitCooldownDuration;
        lives = Mathf.Max(0, lives - 1);
        if (lives <= 0)
        {
            PlayGameOverSound();
            ShowResults();
            return;
        }

        PlayPlayerDeathSound();
        ResetRound(false);
        StartLevelCountdown();
    }

    public void HandlePacManHitByBomb()
    {
        HandlePacManHitByBomb(null);
    }

    public void HandlePacManHitByBomb(PlayerController hitPlayer)
    {
        if (isGameEnded || isCountingDown || isInvincible)
        {
            return;
        }

        if (currentMode == GameMode.TwoPlayer)
        {
            HandleTwoPlayerPacManHit(hitPlayer);
            return;
        }

        lives = Mathf.Max(0, lives - 1);
        if (lives <= 0)
        {
            PlayGameOverSound();
            ShowResults();
            return;
        }

        PlayPlayerDeathSound();
        ResetRound(false);
        StartLevelCountdown();
    }

    private void HandleTwoPlayerPacManHit(PlayerController hitPlayer)
    {
        if (hitPlayer == null || !hitPlayer.IsVulnerable)
        {
            return;
        }

        bool hitSecondPlayer = hitPlayer == secondPlayer;
        if (hitSecondPlayer)
        {
            secondPlayerLives = Mathf.Max(0, secondPlayerLives - 1);
        }
        else
        {
            lives = Mathf.Max(0, lives - 1);
        }

        int remainingLives = hitSecondPlayer ? secondPlayerLives : lives;
        if (remainingLives <= 0)
        {
            hitPlayer.Eliminate();
        }
        else
        {
            hitPlayer.BeginRespawn(playerRespawnDelay, playerRespawnInvincibleDuration);
        }

        if (lives <= 0 && secondPlayerLives <= 0)
        {
            PlayGameOverSound();
            ShowResults();
            return;
        }

        PlayPlayerDeathSound();
    }

    private void PlayPlayerDeathSound()
    {
        PlayPrioritySound(playerDeathClip);
    }

    private void PlayGameOverSound()
    {
        PlayPrioritySound(gameOverClip);
    }

    private void PlayNextLevelSound()
    {
        PlayPrioritySound(nextLevelClip);
    }

    private void PlayPrioritySound(AudioClip clip)
    {
        if (audioSource == null)
        {
            ConfigureAudio();
        }

        if (audioSource == null || clip == null)
        {
            return;
        }

        SuppressPelletSoundsFor(clip.length);
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }

    public void RegisterCollectibles(int count)
    {
        remainingCollectibles = count;
        levelCollectibleCount = count;
    }

    public void NotifyCollectibleEaten()
    {
        if (remainingCollectibles <= 0)
        {
            return;
        }

        remainingCollectibles--;
        if (remainingCollectibles == 0)
        {
            AdvanceLevel();
        }
    }

    public void SkipToNextLevel()
    {
        if (isGameEnded || isCountingDown)
        {
            return;
        }

        ClearLevelCollectibles();
        remainingCollectibles = 0;
        AdvanceLevel();
    }

    private void AdvanceLevel()
    {
        PlayNextLevelSound();
        level++;
        levelElapsedTime = 0f;
        ClearRandomPellets();
        ClearPowerPelletEffect();
        ClearPhantomDecoy();
        ClearLightningLink();
        ClearPlacedBombs();
        ResetRandomPelletSpawnTimer();

        MazeGenerator mazeGenerator = FindFirstObjectByType<MazeGenerator>();
        if (mazeGenerator != null)
        {
            mazeGenerator.Generate();
        }

        ResetRound();
        StartLevelCountdown();
    }

    public void StartLevelCountdown()
    {
        StopCountdown();
        countdownRoutine = StartCoroutine(LevelCountdownRoutine());
    }

    private IEnumerator LevelCountdownRoutine()
    {
        isCountingDown = true;
        isPaused = false;
        Time.timeScale = 0f;

        CountdownUI countdownUI = FindFirstObjectByType<CountdownUI>();
        string levelLabel = "LEVEL " + level;
        for (int i = countdownSeconds; i > 0; i--)
        {
            if (countdownUI != null)
            {
                countdownUI.Show(i.ToString(), levelLabel);
            }

            yield return new WaitForSecondsRealtime(1f);
        }

        if (countdownUI != null)
        {
            countdownUI.Show("GO!", levelLabel);
        }

        yield return new WaitForSecondsRealtime(0.45f);

        if (countdownUI != null)
        {
            countdownUI.Hide();
        }

        isCountingDown = false;
        countdownRoutine = null;

        if (!isGameEnded)
        {
            Time.timeScale = 1f;
        }
    }

    private void StopCountdown()
    {
        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
        }

        isCountingDown = false;

        CountdownUI countdownUI = FindFirstObjectByType<CountdownUI>();
        if (countdownUI != null)
        {
            countdownUI.Hide();
        }
    }

    private void ApplyGhostLevelSpeed()
    {
        float speedBonus = level >= 5 ? 0f : (level - 1) * ghostSpeedIncreasePerLevel;
        float speedMultiplier = GetGhostSpeedMultiplier();
        foreach (GhostController ghostController in FindObjectsByType<GhostController>(FindObjectsSortMode.None))
        {
            ApplyGhostSpeed(ghostController, speedBonus, speedMultiplier);
        }

        ApplyGhostSpeed(whiteGhost, speedBonus, speedMultiplier);
        ApplyGhostSpeed(greenGhost, speedBonus, speedMultiplier);
    }

    private static void ApplyGhostSpeed(GhostController ghostController, float speedBonus, float speedMultiplier)
    {
        if (ghostController == null)
        {
            return;
        }

        ghostController.SetLevelSpeedBonus(speedBonus);
        ghostController.SetLevelSpeedMultiplier(speedMultiplier);
    }

    private float GetGhostSpeedMultiplier()
    {
        if (level <= 2)
        {
            return earlyLevelGhostSpeedMultiplier;
        }

        if (level >= 5)
        {
            return 1f + (level - 4) * lateLevelGhostSpeedIncrease;
        }

        return 1f;
    }

    private void ConfigureAudio()
    {
        if (pelletEatClip == null)
        {
            pelletEatClip = Resources.Load<AudioClip>("Audio/kobeman");
        }

        if (powerPelletClip == null)
        {
            powerPelletClip = Resources.Load<AudioClip>("Audio/powerpellet");
        }

        if (phantomPelletClip == null)
        {
            phantomPelletClip = Resources.Load<AudioClip>("Audio/phantompellet");
        }

        if (lightningPelletClip == null)
        {
            lightningPelletClip = Resources.Load<AudioClip>("Audio/lightning");
        }

        if (playerDeathClip == null)
        {
            playerDeathClip = Resources.Load<AudioClip>("Audio/playerdeath");
        }

        if (gameOverClip == null)
        {
            gameOverClip = Resources.Load<AudioClip>("Audio/gameover");
        }

        if (nextLevelClip == null)
        {
            nextLevelClip = Resources.Load<AudioClip>("Audio/nextlevel");
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
    }

    private void ConfigureGhostReleaseRules()
    {
        ApplyReleaseLock(ghost, false);
        ApplyReleaseLock(pinkGhost, !GetPinkReleaseRule().releaseAtLevelStart);
        ApplyReleaseLock(orangeGhost, !GetOrangeReleaseRule().releaseAtLevelStart);
        ApplyReleaseLock(cyanGhost, !GetCyanReleaseRule().releaseAtLevelStart);
        ApplyReleaseLock(whiteGhost, false);
        ApplyReleaseLock(greenGhost, false);
        UpdateGhostReleaseRules();
    }

    private void ConfigurePrimaryPlayer()
    {
        if (player == null)
        {
            return;
        }

        player.ConfigureControls(
            KeyCode.UpArrow,
            KeyCode.DownArrow,
            KeyCode.LeftArrow,
            KeyCode.RightArrow,
            KeyCode.Return,
            new Vector2(-0.5f, -7.5f),
            Vector2.right,
            Color.white,
            false);
    }

    private void EnsureSecondPlayer()
    {
        if (secondPlayer == null)
        {
            GameObject existing = GameObject.Find("Player 2");
            if (existing != null)
            {
                secondPlayer = existing.GetComponent<PlayerController>();
            }
        }

        if (secondPlayer == null)
        {
            GameObject playerObject = new GameObject("Player 2");
            playerObject.layer = player != null ? player.gameObject.layer : LayerMask.NameToLayer("Default");
            playerObject.transform.position = new Vector3(0.5f, -7.5f, 0f);
            playerObject.transform.localScale = player != null ? player.transform.localScale : Vector3.one * 0.8f;
            secondPlayer = playerObject.AddComponent<PlayerController>();
        }

        secondPlayer.ConfigureControls(
            KeyCode.W,
            KeyCode.S,
            KeyCode.A,
            KeyCode.D,
            KeyCode.Space,
            new Vector2(0.5f, -7.5f),
            Vector2.left,
            new Color(0.45f, 1f, 0.9f, 1f),
            false);
    }

    private void ApplyGameModePlayers()
    {
        bool twoPlayerMode = currentMode == GameMode.TwoPlayer;
        ConfigurePrimaryPlayer();
        EnsureSecondPlayer();

        if (secondPlayer != null)
        {
            secondPlayer.gameObject.SetActive(twoPlayerMode);
        }

        if (ghost != null)
        {
            ghost.SetPlayerControlled(false);
        }

        if (pinkGhost != null)
        {
            pinkGhost.SetPlayerControlled(false);
        }

        if (orangeGhost != null)
        {
            orangeGhost.SetPlayerControlled(false);
        }

        if (cyanGhost != null)
        {
            cyanGhost.SetPlayerControlled(false);
        }

        ConfigureExtraGhost(whiteGhost, twoPlayerMode && level >= 3);
        ConfigureExtraGhost(greenGhost, twoPlayerMode && level >= 4);
    }

    private static void ConfigureExtraGhost(GhostController ghostController, bool enabled)
    {
        if (ghostController == null)
        {
            return;
        }

        ghostController.gameObject.SetActive(enabled);
        ghostController.SetPlayerControlled(false);
        ghostController.SetReleaseLocked(false);
    }

    private void ApplyPlayerLifeState()
    {
        if (currentMode != GameMode.TwoPlayer)
        {
            return;
        }

        if (player != null && lives <= 0)
        {
            player.Eliminate();
        }

        if (secondPlayer != null && secondPlayerLives <= 0)
        {
            secondPlayer.Eliminate();
        }
    }

    private void UpdateGhostReleaseRules()
    {
        UpdateGhostRelease(pinkGhost, GetPinkReleaseRule());
        UpdateGhostRelease(orangeGhost, GetOrangeReleaseRule());
        UpdateGhostRelease(cyanGhost, GetCyanReleaseRule());
    }

    private void UpdateGhostRelease(GhostController ghostController, GhostReleaseRule rule)
    {
        if (ghostController == null)
        {
            return;
        }

        if (rule.releaseAtLevelStart || level >= 4 || GetEatenCollectibleRatio() >= rule.pelletRatio || levelElapsedTime >= rule.elapsedSeconds)
        {
            ApplyReleaseLock(ghostController, false);
        }
    }

    private float GetEatenCollectibleRatio()
    {
        if (levelCollectibleCount <= 0)
        {
            return 0f;
        }

        return Mathf.Clamp01((levelCollectibleCount - remainingCollectibles) / (float)levelCollectibleCount);
    }

    private GhostReleaseRule GetPinkReleaseRule()
    {
        if (level == 1)
        {
            return new GhostReleaseRule(false, 1f, 10f);
        }

        return new GhostReleaseRule(true, 0f, 0f);
    }

    private GhostReleaseRule GetOrangeReleaseRule()
    {
        if (level == 1)
        {
            return new GhostReleaseRule(false, 0.4f, 30f);
        }

        if (level == 2)
        {
            return new GhostReleaseRule(false, 0.3f, 25f);
        }

        if (level == 3)
        {
            return new GhostReleaseRule(false, 0.2f, 20f);
        }

        return new GhostReleaseRule(true, 0f, 0f);
    }

    private GhostReleaseRule GetCyanReleaseRule()
    {
        if (level == 1)
        {
            return new GhostReleaseRule(false, 0.7f, 50f);
        }

        if (level == 2 || level == 3)
        {
            return new GhostReleaseRule(false, 0.5f, 30f);
        }

        return new GhostReleaseRule(true, 0f, 0f);
    }

    private static void ApplyReleaseLock(GhostController ghostController, bool locked)
    {
        if (ghostController != null)
        {
            ghostController.SetReleaseLocked(locked);
        }
    }

    private void TickRandomPelletSpawn()
    {
        randomPelletSpawnTimer -= Time.deltaTime;
        if (randomPelletSpawnTimer > 0f)
        {
            return;
        }

        int pelletType = Random.Range(0, currentMode == GameMode.TwoPlayer ? 5 : 4);
        if (pelletType == 0)
        {
            SpawnRandomPowerPellet();
        }
        else if (pelletType == 1)
        {
            SpawnRandomEnergyPellet();
        }
        else if (pelletType == 2)
        {
            SpawnRandomPhantomPellet();
        }
        else if (pelletType == 3)
        {
            SpawnRandomBombPellet();
        }
        else
        {
            SpawnRandomLightningPellet();
        }

        ResetRandomPelletSpawnTimer();
    }

    private void TickPhantomDecoy()
    {
        if (activePhantomDecoy == null)
        {
            return;
        }

        phantomDecoyTimer -= Time.deltaTime;
        if (phantomDecoyTimer <= 0f)
        {
            ClearPhantomDecoy();
        }
    }

    private void TickPowerPelletTimer()
    {
        if (powerPelletTimer <= 0f)
        {
            return;
        }

        powerPelletTimer -= Time.deltaTime;
        if (powerPelletTimer <= 0f)
        {
            ClearPowerPelletEffect();
        }
    }

    private void ClearPowerPelletEffect()
    {
        powerPelletTimer = 0f;
        foreach (GhostController ghostController in FindObjectsByType<GhostController>(FindObjectsSortMode.None))
        {
            ghostController.ExitFrightenedMode();
        }
    }

    private void SpawnRandomPowerPellet()
    {
        Vector2Int tile = GetRandomUnoccupiedReachableTile();
        GameObject powerPelletObject = new GameObject("Random Power Pellet");
        powerPelletObject.transform.position = MazeGenerator.TileToWorldCenter(tile.x, tile.y);
        powerPelletObject.transform.localScale = Vector3.one * powerPelletScale * randomPelletIconScaleMultiplier;

        SpriteRenderer renderer = powerPelletObject.AddComponent<SpriteRenderer>();
        renderer.sprite = SimpleSprites.PowerPellet;
        renderer.color = Color.white;
        renderer.sortingOrder = 2;
        AddRandomPelletBackground(powerPelletObject.transform, new Color(1f, 0.78f, 0.12f, 0.92f));

        CircleCollider2D collider = powerPelletObject.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;
        collider.isTrigger = true;

        PowerPellet powerPellet = powerPelletObject.AddComponent<PowerPellet>();
        powerPellet.SetCountsTowardLevel(false);
    }

    private void SpawnRandomEnergyPellet()
    {
        Vector2Int tile = GetRandomUnoccupiedReachableTile();
        GameObject energyPelletObject = new GameObject("Random Energy Pellet");
        energyPelletObject.transform.position = MazeGenerator.TileToWorldCenter(tile.x, tile.y);
        energyPelletObject.transform.localScale = Vector3.one * energyPelletScale * randomPelletIconScaleMultiplier;

        SpriteRenderer renderer = energyPelletObject.AddComponent<SpriteRenderer>();
        renderer.sprite = SimpleSprites.EnergyPellet;
        renderer.color = Color.white;
        renderer.sortingOrder = 2;
        AddRandomPelletBackground(
            energyPelletObject.transform,
            new Color(0.45f, 0.9f, 1f, 0.92f),
            SimpleSprites.OriginalItemBackground);

        CircleCollider2D collider = energyPelletObject.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;
        collider.isTrigger = true;

        energyPelletObject.AddComponent<EnergyPellet>();
    }

    private void SpawnRandomPhantomPellet()
    {
        Vector2Int tile = GetRandomUnoccupiedReachableTile();
        GameObject phantomPelletObject = new GameObject("Random Phantom Pellet");
        phantomPelletObject.transform.position = MazeGenerator.TileToWorldCenter(tile.x, tile.y);
        phantomPelletObject.transform.localScale = Vector3.one * phantomPelletScale * randomPelletIconScaleMultiplier;

        SpriteRenderer renderer = phantomPelletObject.AddComponent<SpriteRenderer>();
        renderer.sprite = SimpleSprites.PhantomPellet;
        renderer.color = Color.white;
        renderer.sortingOrder = 2;
        AddRandomPelletBackground(phantomPelletObject.transform, new Color(0.65f, 0.25f, 1f, 0.92f));

        CircleCollider2D collider = phantomPelletObject.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;
        collider.isTrigger = true;

        phantomPelletObject.AddComponent<PhantomPellet>();
    }

    private void SpawnRandomBombPellet()
    {
        Vector2Int tile = GetRandomUnoccupiedReachableTile();
        GameObject bombPelletObject = new GameObject("Random Bomb Pellet");
        bombPelletObject.transform.position = MazeGenerator.TileToWorldCenter(tile.x, tile.y);
        bombPelletObject.transform.localScale = Vector3.one * bombPelletScale * randomPelletIconScaleMultiplier;

        SpriteRenderer renderer = bombPelletObject.AddComponent<SpriteRenderer>();
        renderer.sprite = SimpleSprites.BombPellet;
        renderer.color = Color.white;
        renderer.sortingOrder = 2;
        AddRandomPelletBackground(bombPelletObject.transform, new Color(1f, 0.08f, 0.06f, 0.92f));

        CircleCollider2D collider = bombPelletObject.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;
        collider.isTrigger = true;

        bombPelletObject.AddComponent<BombPellet>();
    }

    private void SpawnRandomLightningPellet()
    {
        Vector2Int tile = GetRandomUnoccupiedReachableTile();
        GameObject lightningPelletObject = new GameObject("Random Lightning Pellet");
        lightningPelletObject.transform.position = MazeGenerator.TileToWorldCenter(tile.x, tile.y);
        lightningPelletObject.transform.localScale = Vector3.one * lightningPelletScale * randomPelletIconScaleMultiplier;

        SpriteRenderer renderer = lightningPelletObject.AddComponent<SpriteRenderer>();
        renderer.sprite = SimpleSprites.LightningPellet;
        renderer.color = Color.white;
        renderer.sortingOrder = 2;
        AddRandomPelletBackground(lightningPelletObject.transform, new Color(0.1f, 0.95f, 1f, 0.92f));

        CircleCollider2D collider = lightningPelletObject.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;
        collider.isTrigger = true;

        lightningPelletObject.AddComponent<LightningPellet>();
    }

    private static void AddRandomPelletBackground(Transform parent, Color color, Sprite backgroundSprite = null)
    {
        GameObject background = new GameObject("Item Background");
        background.transform.SetParent(parent, false);
        background.transform.localPosition = new Vector3(0f, 0f, 0.05f);
        background.transform.localScale = Vector3.one * 1.18f;

        SpriteRenderer renderer = background.AddComponent<SpriteRenderer>();
        renderer.sprite = backgroundSprite != null ? backgroundSprite : SimpleSprites.ItemBackground;
        renderer.color = color;
        renderer.sortingOrder = 1;
    }

    private Vector2Int GetRandomUnoccupiedReachableTile()
    {
        var reachableTiles = MazeGenerator.GetReachableWalkableTiles();
        if (reachableTiles.Count == 0)
        {
            return MazeGenerator.WorldToTile(new Vector2(-0.5f, -7.5f));
        }

        int startIndex = Random.Range(0, reachableTiles.Count);
        for (int offset = 0; offset < reachableTiles.Count; offset++)
        {
            Vector2Int candidate = reachableTiles[(startIndex + offset) % reachableTiles.Count];
            if (!IsRandomPelletOnTile(candidate))
            {
                return candidate;
            }
        }

        return reachableTiles[startIndex];
    }

    private bool IsRandomPelletOnTile(Vector2Int tile)
    {
        foreach (PowerPellet pellet in FindObjectsByType<PowerPellet>(FindObjectsSortMode.None))
        {
            if (IsPelletOnTile(pellet, tile))
            {
                return true;
            }
        }

        foreach (EnergyPellet pellet in FindObjectsByType<EnergyPellet>(FindObjectsSortMode.None))
        {
            if (IsPelletOnTile(pellet, tile))
            {
                return true;
            }
        }

        foreach (PhantomPellet pellet in FindObjectsByType<PhantomPellet>(FindObjectsSortMode.None))
        {
            if (IsPelletOnTile(pellet, tile))
            {
                return true;
            }
        }

        foreach (BombPellet pellet in FindObjectsByType<BombPellet>(FindObjectsSortMode.None))
        {
            if (IsPelletOnTile(pellet, tile))
            {
                return true;
            }
        }

        foreach (LightningPellet pellet in FindObjectsByType<LightningPellet>(FindObjectsSortMode.None))
        {
            if (IsPelletOnTile(pellet, tile))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPelletOnTile(Component pellet, Vector2Int tile)
    {
        return pellet != null && MazeGenerator.WorldToTile(pellet.transform.position) == tile;
    }

    private static void ClearRandomPellets()
    {
        foreach (PowerPellet pellet in FindObjectsByType<PowerPellet>(FindObjectsSortMode.None))
        {
            Destroy(pellet.gameObject);
        }

        foreach (EnergyPellet pellet in FindObjectsByType<EnergyPellet>(FindObjectsSortMode.None))
        {
            Destroy(pellet.gameObject);
        }

        foreach (PhantomPellet pellet in FindObjectsByType<PhantomPellet>(FindObjectsSortMode.None))
        {
            Destroy(pellet.gameObject);
        }

        foreach (BombPellet pellet in FindObjectsByType<BombPellet>(FindObjectsSortMode.None))
        {
            Destroy(pellet.gameObject);
        }

        foreach (LightningPellet pellet in FindObjectsByType<LightningPellet>(FindObjectsSortMode.None))
        {
            Destroy(pellet.gameObject);
        }
    }

    private static void ClearLevelCollectibles()
    {
        foreach (Pellet pellet in FindObjectsByType<Pellet>(FindObjectsSortMode.None))
        {
            Destroy(pellet.gameObject);
        }

        foreach (PowerPellet pellet in FindObjectsByType<PowerPellet>(FindObjectsSortMode.None))
        {
            if (pellet.CountsTowardLevel)
            {
                Destroy(pellet.gameObject);
            }
        }
    }

    private void ClearPhantomDecoy()
    {
        if (activePhantomDecoy == null)
        {
            return;
        }

        Destroy(activePhantomDecoy);
        activePhantomDecoy = null;
        phantomDecoyTimer = 0f;
    }

    private void ClearLightningLink()
    {
        if (activeLightningLink == null)
        {
            return;
        }

        Destroy(activeLightningLink);
        activeLightningLink = null;
    }

    private static void ClearPlacedBombs()
    {
        foreach (BombController bomb in FindObjectsByType<BombController>(FindObjectsSortMode.None))
        {
            Destroy(bomb.gameObject);
        }
    }

    private void ResetRandomPelletSpawnTimer()
    {
        randomPelletSpawnTimer = Mathf.Max(0.1f, randomPelletSpawnInterval);
    }

    private void EnsurePinkGhost()
    {
        GameObject existing = GameObject.Find("PinkGhost");
        if (existing != null)
        {
            pinkGhost = existing.GetComponent<GhostController>();
            if (pinkGhost != null)
            {
                pinkGhost.SetTargetMode(GhostController.TargetMode.FourTilesAhead);
                pinkGhost.SetNormalSprite(SimpleSprites.PinkGhost);
            }

            return;
        }

        GameObject ghostObject = new GameObject("PinkGhost");
        ghostObject.layer = LayerMask.NameToLayer("Default");
        ghostObject.transform.position = new Vector3(-0.5f, 1.5f, 0f);
        ghostObject.transform.localScale = Vector3.one * 0.8f;

        pinkGhost = ghostObject.AddComponent<GhostController>();
        pinkGhost.SetTargetMode(GhostController.TargetMode.FourTilesAhead);
        pinkGhost.SetNormalSprite(SimpleSprites.PinkGhost);

        SpriteRenderer renderer = ghostObject.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sprite = SimpleSprites.PinkGhost;
            renderer.color = Color.white;
        }
    }

    private void EnsureOrangeGhost()
    {
        GameObject existing = GameObject.Find("OrangeGhost");
        if (existing != null)
        {
            orangeGhost = existing.GetComponent<GhostController>();
            if (orangeGhost != null)
            {
                orangeGhost.SetTargetMode(GhostController.TargetMode.ChaseUntilCloseThenRandom);
                orangeGhost.SetNormalSprite(SimpleSprites.OrangeGhost);
            }

            return;
        }

        GameObject ghostObject = new GameObject("OrangeGhost");
        ghostObject.layer = LayerMask.NameToLayer("Default");
        ghostObject.transform.position = new Vector3(0.5f, 1.5f, 0f);
        ghostObject.transform.localScale = Vector3.one * 0.8f;

        orangeGhost = ghostObject.AddComponent<GhostController>();
        orangeGhost.SetTargetMode(GhostController.TargetMode.ChaseUntilCloseThenRandom);
        orangeGhost.SetNormalSprite(SimpleSprites.OrangeGhost);

        SpriteRenderer renderer = ghostObject.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sprite = SimpleSprites.OrangeGhost;
            renderer.color = Color.white;
        }
    }

    private void EnsureCyanGhost()
    {
        GameObject existing = GameObject.Find("CyanGhost");
        if (existing != null)
        {
            cyanGhost = existing.GetComponent<GhostController>();
            if (cyanGhost != null)
            {
                cyanGhost.SetTargetMode(GhostController.TargetMode.InkyVector);
                cyanGhost.SetNormalSprite(SimpleSprites.CyanGhost);
            }

            return;
        }

        GameObject ghostObject = new GameObject("CyanGhost");
        ghostObject.layer = LayerMask.NameToLayer("Default");
        ghostObject.transform.position = new Vector3(1.5f, 1.5f, 0f);
        ghostObject.transform.localScale = Vector3.one * 0.8f;

        cyanGhost = ghostObject.AddComponent<GhostController>();
        cyanGhost.SetTargetMode(GhostController.TargetMode.InkyVector);
        cyanGhost.SetNormalSprite(SimpleSprites.CyanGhost);

        SpriteRenderer renderer = ghostObject.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sprite = SimpleSprites.CyanGhost;
            renderer.color = Color.white;
        }
    }

    private void EnsureWhiteGhost()
    {
        GameObject existing = GameObject.Find("WhiteGhost");
        if (existing != null)
        {
            whiteGhost = existing.GetComponent<GhostController>();
            if (whiteGhost != null)
            {
                whiteGhost.SetTargetMode(GhostController.TargetMode.RandomTurns);
                whiteGhost.SetNormalSprite(SimpleSprites.WhiteGhost);
            }

            return;
        }

        GameObject ghostObject = new GameObject("WhiteGhost");
        ghostObject.layer = LayerMask.NameToLayer("Default");
        ghostObject.transform.position = new Vector3(-1.5f, 0.5f, 0f);
        ghostObject.transform.localScale = Vector3.one * 0.8f;

        whiteGhost = ghostObject.AddComponent<GhostController>();
        whiteGhost.SetTargetMode(GhostController.TargetMode.RandomTurns);
        whiteGhost.SetNormalSprite(SimpleSprites.WhiteGhost);

        SpriteRenderer renderer = ghostObject.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sprite = SimpleSprites.WhiteGhost;
            renderer.color = Color.white;
        }

        ghostObject.SetActive(false);
    }

    private void EnsureGreenGhost()
    {
        GameObject existing = GameObject.Find("GreenGhost");
        if (existing != null)
        {
            greenGhost = existing.GetComponent<GhostController>();
            if (greenGhost != null)
            {
                greenGhost.SetTargetMode(GhostController.TargetMode.RandomTurns);
                greenGhost.SetNormalSprite(SimpleSprites.GreenGhost);
            }

            return;
        }

        GameObject ghostObject = new GameObject("GreenGhost");
        ghostObject.layer = LayerMask.NameToLayer("Default");
        ghostObject.transform.position = new Vector3(-0.5f, 0.5f, 0f);
        ghostObject.transform.localScale = Vector3.one * 0.8f;

        greenGhost = ghostObject.AddComponent<GhostController>();
        greenGhost.SetTargetMode(GhostController.TargetMode.RandomTurns);
        greenGhost.SetNormalSprite(SimpleSprites.GreenGhost);

        SpriteRenderer renderer = ghostObject.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sprite = SimpleSprites.GreenGhost;
            renderer.color = Color.white;
        }

        ghostObject.SetActive(false);
    }

    private static GhostController FindGhostByName(string objectName)
    {
        GameObject ghostObject = GameObject.Find(objectName);
        return ghostObject != null ? ghostObject.GetComponent<GhostController>() : null;
    }
}
