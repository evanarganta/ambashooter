using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Volume Balances")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.7f;
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
    private AudioClip bossEnrageClip;

    private AudioSource audioSource2D;
    private AudioSource playerHurtSource;
    private AudioSource musicSource;

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
        audioSource2D.spatialBlend = 0f;

        playerHurtSource = gameObject.AddComponent<AudioSource>();
        playerHurtSource.playOnAwake = false;
        playerHurtSource.spatialBlend = 0f;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.loop = true;

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
        bossEnrageClip    = Resources.Load<AudioClip>("Audio/Enrage");
    }
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

        playerHurtSource.clip = playerHurtClip;
        playerHurtSource.volume = playerVolume * masterVolume;
        playerHurtSource.Stop();
        playerHurtSource.Play();
    }

    public void PlayPlayerDie()
    {
        if (playerHurtSource != null)
        {
            playerHurtSource.Stop();
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
        StopMusic();
        Play2D(gameOverClip, uiVolume);
    }

    public void PlayLevelComplete()
    {
        StopMusic();
        Play2D(levelCompleteClip, uiVolume);
    }

    public void PlayBossEnrage(AudioClip customClip = null)
    {
        AudioClip clipToPlay = customClip != null ? customClip : bossEnrageClip;
        if (clipToPlay == null) clipToPlay = Resources.Load<AudioClip>("Audio/Enrage");
        Play2D(clipToPlay, 1.0f);
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null || musicSource == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = musicVolume * masterVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }

    private void Play2D(AudioClip clip, float volumeScale)
    {
        if (clip == null) return;
        audioSource2D.PlayOneShot(clip, volumeScale * masterVolume);
    }

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

        float distance = 0f;
        float pan = 0f;

        Transform playerTransform = PlayerController.Instance != null ? PlayerController.Instance.transform : null;
        if (playerTransform != null)
        {
            Vector2 playerPos2D = playerTransform.position;
            Vector2 sourcePos2D = worldPos;
            distance = Vector2.Distance(playerPos2D, sourcePos2D);

            float dx = sourcePos2D.x - playerPos2D.x;
            pan = Mathf.Clamp(dx / Mathf.Max(1f, soundMaxDistance * 0.5f), -1f, 1f) * panStereoFactor;
        }

        float distanceFactor = 1f;
        if (distance > soundMinDistance)
        {
            float t = (distance - soundMinDistance) / Mathf.Max(0.001f, soundMaxDistance - soundMinDistance);
            distanceFactor = Mathf.Clamp01(1f - t);
            distanceFactor = distanceFactor * distanceFactor;
        }

        float finalVolume = volumeScale * masterVolume * distanceFactor;
        if (finalVolume <= 0.001f) return;

        GameObject tempAudio = new GameObject("TempAudio_" + clip.name);
        worldPos.z = 0f;
        tempAudio.transform.position = worldPos;

        AudioSource source = tempAudio.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = finalVolume;
        source.panStereo = pan;
        source.spatialBlend = 0f;
        source.playOnAwake = false;

        source.Play();
        Destroy(tempAudio, clip.length + 0.1f);
    }
}

