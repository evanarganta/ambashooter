using UnityEngine;

/// <summary>
/// Central audio manager that auto-loads audio clips from Resources/Audio.
/// Supports 2D stereo/UI sounds as well as spatialized 2D/3D sounds for enemies
/// with distance falloff, stereo panning (left/right hearing), and volume controls.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Volume Balances")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float playerVolume = 0.9f;
    [Range(0f, 1f)] public float enemyVolume = 0.6f;
    [Range(0f, 1f)] public float uiVolume = 0.85f;

    [Header("Spatial Audio (2D Distance & Pan)")]
    [Tooltip("Distance from player where sound starts to fade. Closer = louder.")]
    public float soundMinDistance = 1.0f;
    [Tooltip("Distance from player where sound becomes completely quiet.")]
    public float soundMaxDistance = 14.0f;
    [Tooltip("Max stereo pan factor: -1 (full left) to 1 (full right).")]
    public float panStereoFactor = 0.85f;

    // Loaded Audio Clips
    private AudioClip parrySuccessClip;
    private AudioClip parryMissClip;
    private AudioClip playerShootClip;
    private AudioClip playerJumpClip;
    private AudioClip playerHurtClip;
    private AudioClip playerDieClip;
    private AudioClip enemyShootClip;
    private AudioClip enemyHurtClip;
    private AudioClip enemyDieClip;
    private AudioClip gameOverClip;
    private AudioClip levelCompleteClip;

    private AudioSource audioSource2D;
    private AudioSource playerHurtSource;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance == null)
        {
            GameObject obj = new GameObject("AudioManager");
            DontDestroyOnLoad(obj);
            obj.AddComponent<AudioManager>();
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource2D = gameObject.AddComponent<AudioSource>();
        audioSource2D.playOnAwake = false;
        audioSource2D.spatialBlend = 0f; // 2D flat sound

        playerHurtSource = gameObject.AddComponent<AudioSource>();
        playerHurtSource.playOnAwake = false;
        playerHurtSource.spatialBlend = 0f;

        LoadClips();
    }

    private void LoadClips()
    {
        parrySuccessClip  = Resources.Load<AudioClip>("Audio/parry_success");
        parryMissClip     = Resources.Load<AudioClip>("Audio/parry_miss");
        playerShootClip   = Resources.Load<AudioClip>("Audio/player_shoot");
        playerJumpClip    = Resources.Load<AudioClip>("Audio/player_jump");
        playerHurtClip    = Resources.Load<AudioClip>("Audio/player_hurt");
        playerDieClip     = Resources.Load<AudioClip>("Audio/player_die");
        enemyShootClip    = Resources.Load<AudioClip>("Audio/enemy_shoot");
        enemyHurtClip     = Resources.Load<AudioClip>("Audio/enemy_hurt");
        enemyDieClip      = Resources.Load<AudioClip>("Audio/enemy_die");
        gameOverClip      = Resources.Load<AudioClip>("Audio/game_over");
        levelCompleteClip = Resources.Load<AudioClip>("Audio/level_complete");
    }

    // --- 2D Audio (Player & UI) ---

    public void PlayPlayerShoot()
    {
        Play2D(playerShootClip, playerVolume);
    }

    public void PlayPlayerJump()
    {
        Play2D(playerJumpClip, playerVolume);
    }

    public void PlayPlayerHurt()
    {
        if (playerHurtClip == null || playerHurtSource == null) return;

        // If player hurt sound is already playing, restart it cleanly instead of duplicating/overlapping
        playerHurtSource.clip = playerHurtClip;
        playerHurtSource.volume = playerVolume * masterVolume;
        playerHurtSource.Stop();
        playerHurtSource.Play();
    }

    public void PlayPlayerDie()
    {
        if (playerHurtSource != null)
        {
            playerHurtSource.Stop(); // Cut off hurt sound if player dies
        }
        Play2D(playerDieClip, playerVolume);
    }

    public void PlayParrySuccess()
    {
        Play2D(parrySuccessClip, playerVolume * 1.1f);
    }

    public void PlayParryMiss()
    {
        Play2D(parryMissClip, playerVolume * 0.7f);
    }

    public void PlayGameOver()
    {
        Play2D(gameOverClip, uiVolume);
    }

    public void PlayLevelComplete()
    {
        Play2D(levelCompleteClip, uiVolume);
    }

    private void Play2D(AudioClip clip, float volumeScale)
    {
        if (clip == null) return;
        audioSource2D.PlayOneShot(clip, volumeScale * masterVolume);
    }

    // --- Spatialized Audio (Enemies: Left/Right stereo panning & distance) ---

    public void PlayEnemyShoot(Vector3 worldPos)
    {
        PlayAtPosition(enemyShootClip, worldPos, enemyVolume);
    }

    public void PlayEnemyHurt(Vector3 worldPos)
    {
        PlayAtPosition(enemyHurtClip, worldPos, enemyVolume);
    }

    public void PlayEnemyDie(Vector3 worldPos)
    {
        PlayAtPosition(enemyDieClip, worldPos, enemyVolume);
    }

    public void PlayAtPosition(AudioClip clip, Vector3 worldPos, float volumeScale)
    {
        if (clip == null) return;

        // Calculate distance and horizontal pan relative to Player position (true 2D hearing)
        float distance = 0f;
        float pan = 0f;

        Transform playerTransform = PlayerController.Instance != null ? PlayerController.Instance.transform : null;
        if (playerTransform != null)
        {
            Vector2 playerPos2D = playerTransform.position;
            Vector2 sourcePos2D = worldPos;
            distance = Vector2.Distance(playerPos2D, sourcePos2D);

            // Left/Right panning based on horizontal offset from player (-1 = left, +1 = right)
            float dx = sourcePos2D.x - playerPos2D.x;
            pan = Mathf.Clamp(dx / Mathf.Max(1f, soundMaxDistance * 0.5f), -1f, 1f) * panStereoFactor;
        }

        // Distance attenuation: sound is louder the closer the player is to the source
        float distanceFactor = 1f;
        if (distance > soundMinDistance)
        {
            float t = (distance - soundMinDistance) / Mathf.Max(0.001f, soundMaxDistance - soundMinDistance);
            distanceFactor = Mathf.Clamp01(1f - t);
            // Logarithmic/quad falloff for natural hearing
            distanceFactor = distanceFactor * distanceFactor;
        }

        float finalVolume = volumeScale * masterVolume * distanceFactor;
        if (finalVolume <= 0.001f) return; // Too far to hear

        // Create independent temporary AudioSource so multiple identical SFX freely overlap
        GameObject tempAudio = new GameObject("TempAudio_" + clip.name);
        worldPos.z = 0f;
        tempAudio.transform.position = worldPos;

        AudioSource source = tempAudio.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = finalVolume;
        source.panStereo = pan; // Crisp Left/Right separation
        source.spatialBlend = 0f; // Handled directly in 2D space for exact distance/pan control
        source.playOnAwake = false;

        source.Play();
        Destroy(tempAudio, clip.length + 0.1f);
    }
}

