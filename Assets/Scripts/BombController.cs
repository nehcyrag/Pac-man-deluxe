using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BombController : MonoBehaviour
{
    [SerializeField] private float fuseDuration = 2f;
    [SerializeField] private float explosionSpeed = 24f;
    [SerializeField] private float explosionLifetime = 3f;
    [SerializeField] private float explosionClearDelay = 0.25f;

    private float timer;
    private bool hasExploded;
    private readonly HashSet<PlayerController> damagedPlayers = new HashSet<PlayerController>();
    private float clearExplosionTimer = -1f;
    private int stoppedExplosionWaveCount;
    private readonly List<BombExplosionWaveController> activeExplosionWaves = new List<BombExplosionWaveController>();
    private static AudioClip explosionClip;

    private void Awake()
    {
        timer = fuseDuration;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = SimpleSprites.Bomb;
        spriteRenderer.color = Color.white;
        spriteRenderer.sortingOrder = 4;
        transform.localScale = Vector3.one * 0.58f;
    }

    private void Update()
    {
        if (!hasExploded)
        {
            timer -= Time.deltaTime;
            if (timer > 0f)
            {
                return;
            }

            Explode();
            return;
        }

        if (clearExplosionTimer < 0f)
        {
            return;
        }

        clearExplosionTimer -= Time.deltaTime;
        if (clearExplosionTimer > 0f)
        {
            return;
        }

        ClearExplosionWaves();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        ClearExplosionWaves();
    }

    public void SetExplosionSpeed(float speed)
    {
        explosionSpeed = Mathf.Max(0f, speed);
    }

    public bool TryDamagePlayer()
    {
        return TryDamagePlayer(null);
    }

    public bool TryDamagePlayer(PlayerController player)
    {
        if (player != null && damagedPlayers.Contains(player))
        {
            return false;
        }

        if (player != null)
        {
            damagedPlayers.Add(player);
        }

        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.HandlePacManHitByBomb(player);
        }

        return true;
    }

    private void Explode()
    {
        hasExploded = true;
        PlayExplosionSound();

        SpriteRenderer bombRenderer = GetComponent<SpriteRenderer>();
        if (bombRenderer != null)
        {
            bombRenderer.enabled = false;
        }

        SpawnExplosionWave(Vector2.up);
        SpawnExplosionWave(Vector2.down);
        SpawnExplosionWave(Vector2.left);
        SpawnExplosionWave(Vector2.right);
    }

    private static void PlayExplosionSound()
    {
        if (explosionClip == null)
        {
            explosionClip = Resources.Load<AudioClip>("Audio/bombexplosion");
        }

        if (explosionClip == null)
        {
            return;
        }

        GameObject audioObject = new GameObject("Bomb Explosion Sound");
        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.clip = explosionClip;
        GameManager.SuppressPelletSoundsFor(explosionClip.length);
        source.Play();
        Destroy(audioObject, explosionClip.length + 0.1f);
    }

    private void SpawnExplosionWave(Vector2 direction)
    {
        GameObject wave = new GameObject("Bomb Explosion Wave");
        wave.transform.position = transform.position;

        BombExplosionWaveController waveController = wave.AddComponent<BombExplosionWaveController>();
        waveController.Launch(direction, explosionSpeed, explosionLifetime, this);
        activeExplosionWaves.Add(waveController);
    }

    public void NotifyExplosionWaveStopped(BombExplosionWaveController waveController)
    {
        if (waveController == null || !activeExplosionWaves.Contains(waveController))
        {
            return;
        }

        stoppedExplosionWaveCount++;
        if (stoppedExplosionWaveCount >= activeExplosionWaves.Count && clearExplosionTimer < 0f)
        {
            clearExplosionTimer = explosionClearDelay;
        }
    }

    private void ClearExplosionWaves()
    {
        foreach (BombExplosionWaveController waveController in activeExplosionWaves)
        {
            if (waveController != null)
            {
                waveController.ForceClear();
            }
        }

        activeExplosionWaves.Clear();
    }
}
