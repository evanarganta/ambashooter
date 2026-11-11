using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossMushroom : EnemyController
{
    public static BossMushroom Instance { get; private set; }

    [Header("Boss Specifics")]
    public string bossName = "ELDER MUSHROOM";
    public float maxHealth = 200f;
    [SerializeField] private AudioClip bossMusic;
    [SerializeField] private AudioClip enrageSound;

    [Header("Combat Settings")]
    [SerializeField] private SpriteRenderer graphic;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 6.0f;
    [SerializeField] private float attackCooldown = 5.0f;

    [Header("Spike Visuals (Editable)")]
    [SerializeField] private Sprite spikeSprite;
    [SerializeField] private Color spikeColor = new Color(0.85f, 0.85f, 0.9f, 0.85f);
    [SerializeField] private Vector2 spikeScale = new Vector2(1f, 1f);
    [SerializeField] private int spikeCount = 18;
    [SerializeField] private bool autoTileAcrossIndicator = true;
    [SerializeField] private int spikeSortingOrder = -1;
    [SerializeField] private float spikeZOffset = 0.5f;
    [SerializeField] private float spikeYOffset = 0f;

    [Header("Zone Attack Positions (from scene)")]
    [SerializeField] private float arenaFloorSurfaceY  = 20.03f;
    [SerializeField] private float platformSurfaceY    = 21.75f;
    [SerializeField] private float platformLeftX       = -5.5f;
    [SerializeField] private float platformRightX      =  5.5f;
    [SerializeField] private float arenaHalfWidth      =  4.5f;
    [SerializeField] private float platformHalfWidth   =  2.0f;

    private float attackTimer = 1.5f;
    private int   attackIndex = 0;
    private bool  isEnraged   = false;
    private bool  isZoneAttackActive = false;

    private Vector3 spawnPosition;
    private static Sprite boxSprite;

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
        health = maxHealth;
        isContactDamageImmune = true;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (bossMusic != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
        }
    }

    void Start()
    {
        spawnPosition = transform.position;

        if (bossMusic != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(bossMusic, loop: true);
        }

        if (attackTarget == null && PlayerController.Instance != null)
            attackTarget = PlayerController.Instance.transform;

        if (rb2 != null)
        {
            rb2.gravityScale = 0f;
            rb2.linearVelocity = Vector2.zero;
            rb2.constraints = RigidbodyConstraints2D.FreezeAll;
        }
    }

    public override void ApplyKnockback(Vector2 direction, float force) { }

    protected override void Update()
    {
        base.Update();
        if (isDead) return;

        transform.position = spawnPosition;
        if (rb2 != null) rb2.linearVelocity = Vector2.zero;

        if (graphic != null && attackTarget != null)
            graphic.flipX = attackTarget.position.x < transform.position.x;

        if (!isEnraged && health <= maxHealth * 0.5f)
        {
            isEnraged = true;
            attackCooldown = 2.5f;
            bulletSpeed    = 7.5f;
            if (graphic != null)
                graphic.color = new Color(1f, 0.35f, 0.35f);

            PlayEnrageSound();
        }
    }

    protected override void Movement()
    {
        if (isZoneAttackActive) return;

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            RunNextAttack();
            attackTimer = attackCooldown;
        }
    }

    private void RunNextAttack()
    {
        switch (attackIndex % 6)
        {
            case 0: StartCoroutine(ZoneFloorEruption());       break;
            case 1: StartCoroutine(FireDoubleNova());           break;
            case 2: StartCoroutine(ZonePlatformBombardment()); break;
            case 3: StartCoroutine(FireSpiralGalaxy());         break;
            case 4: StartCoroutine(FireEightWayBurst());        break;
            case 5: StartCoroutine(FireCrossBloom());           break;
        }
        attackIndex++;
    }

    private IEnumerator ZoneFloorEruption()
    {
        isZoneAttackActive = true;

        float telegraphTime = isEnraged ? 1.0f : 1.4f;
        float strikeTime    = isEnraged ? 3.2f : 2.5f;

        float indH = 0.5f;
        Vector3 indPos  = new Vector3(0f, arenaFloorSurfaceY + indH * 0.5f, 0f);
        Vector2 indSize = new Vector2(arenaHalfWidth * 2f, indH);

        GameObject warn = CreateZoneBox(indPos, indSize,
            new Color(1f, 0.2f, 0.0f, 0.10f), flashSpeed: 5f);

        yield return new WaitForSeconds(telegraphTime);
        Destroy(warn);

        List<GameObject> spikes = new List<GameObject>();
        Sprite sprToUse = spikeSprite != null ? spikeSprite : GetBoxSprite();

        float spikeUnitWidth;
        if (spikeSprite != null && spikeSprite.bounds.size.x > 0.001f)
        {
            spikeUnitWidth = spikeSprite.bounds.size.x * spikeScale.x;
        }
        else
        {
            spikeUnitWidth = Mathf.Max(0.1f, spikeScale.x);
        }

        float totalSpan = arenaHalfWidth * 2f;

        int count;
        if (autoTileAcrossIndicator)
        {
            count = Mathf.Max(1, Mathf.RoundToInt(totalSpan / spikeUnitWidth));
        }
        else
        {
            count = Mathf.Max(1, spikeCount);
        }

        if (isEnraged) count = Mathf.RoundToInt(count * 1.15f);

        float step = (count > 1) ? (totalSpan - spikeUnitWidth) / (count - 1) : 0f;
        float startX = (count > 1) ? (-arenaHalfWidth + spikeUnitWidth * 0.5f) : 0f;

        for (int i = 0; i < count; i++)
        {
            float sx = (count > 1) ? (startX + i * step) : 0f;
            float sh = Random.Range(0.85f, 1.15f);
            GameObject spike = new GameObject("Spike");

            float baseHeight;
            if (spikeSprite != null && spikeSprite.bounds.size.y > 0.001f)
            {
                baseHeight = spikeSprite.bounds.size.y * spikeScale.y;
            }
            else
            {
                baseHeight = spikeScale.y;
            }

            float sy = arenaFloorSurfaceY + (baseHeight * 0.5f) + spikeYOffset;
            spike.transform.position = new Vector3(sx, sy, spikeZOffset);

            if (spikeSprite != null)
            {
                spike.transform.localScale = new Vector3(spikeScale.x, spikeScale.y * sh, 1f);
            }
            else
            {
                spike.transform.localScale = new Vector3(spikeUnitWidth, spikeScale.y * sh, 1f);
            }

            SpriteRenderer sr = spike.AddComponent<SpriteRenderer>();
            sr.sprite       = sprToUse;
            sr.color        = spikeColor;
            sr.sortingOrder = spikeSortingOrder;
            spikes.Add(spike);
        }

        PlayShootSound();

        bool wasInZone = false;
        float elapsed  = 0f;
        float tickInterval = 0.08f;
        float onEnterDamage = 60f;

        while (elapsed < strikeTime)
        {
            if (isDead)
            {
                spikes.ForEach(s => { if (s) Destroy(s); });
                yield break;
            }

            if (attackTarget != null)
            {
                float py    = attackTarget.position.y;
                float px    = Mathf.Abs(attackTarget.position.x);
                bool inZone = py < arenaFloorSurfaceY + 1.3f && px < arenaHalfWidth;

                if (inZone && !wasInZone)
                    attackTarget.GetComponent<PlayerController>()?.DamagedBy(onEnterDamage);

                wasInZone = inZone;
            }

            elapsed += tickInterval;
            yield return new WaitForSeconds(tickInterval);
        }

        spikes.ForEach(s => { if (s) Destroy(s); });
        isZoneAttackActive = false;
        attackTimer = 5.0f;
    }

    private IEnumerator ZonePlatformBombardment()
    {
        isZoneAttackActive = true;

        float telegraphTime = isEnraged ? 1.1f : 1.5f;
        float strikeTime    = isEnraged ? 3.0f : 2.4f;

        float platIndH = 0.5f;
        Vector2 platSize = new Vector2(platformHalfWidth * 2f, platIndH);
        Vector3 posL = new Vector3(platformLeftX,  platformSurfaceY + platIndH * 0.5f, 0f);
        Vector3 posR = new Vector3(platformRightX, platformSurfaceY + platIndH * 0.5f, 0f);

        Color warnColor = new Color(1f, 0.5f, 0.0f, 0.10f);

        GameObject warnL = CreateZoneBox(posL, platSize, warnColor, flashSpeed: 6f);
        GameObject warnR = CreateZoneBox(posR, platSize, warnColor, flashSpeed: 6f);

        yield return new WaitForSeconds(telegraphTime);
        Destroy(warnL);
        Destroy(warnR);

        Color hazardColor = new Color(1f, 0.4f, 0.0f, 0.20f);
        GameObject hazL = CreateZoneBox(posL, platSize, hazardColor, flashSpeed: 18f);
        GameObject hazR = CreateZoneBox(posR, platSize, hazardColor, flashSpeed: 18f);

        float elapsed = 0f;
        while (elapsed < strikeTime)
        {
            if (isDead) { Destroy(hazL); Destroy(hazR); yield break; }

            int count = isEnraged ? 3 : 2;
            for (int i = 0; i < count; i++)
            {
                float lx = Random.Range(platformLeftX  - platformHalfWidth, platformLeftX  + platformHalfWidth);
                float rx = Random.Range(platformRightX - platformHalfWidth, platformRightX + platformHalfWidth);
                float spawnY = platformSurfaceY + 3f;
                SpawnBullet(new Vector3(lx, spawnY, 0f), Vector2.down, bulletSpeed * 1.4f, ignoreObstacles: false);
                SpawnBullet(new Vector3(rx, spawnY, 0f), Vector2.down, bulletSpeed * 1.4f, ignoreObstacles: false);
            }
            PlayShootSound();

            if (attackTarget != null)
            {
                float py = attackTarget.position.y;
                float px = attackTarget.position.x;
                bool onLeft  = py > platformSurfaceY - 0.5f && Mathf.Abs(px - platformLeftX)  < platformHalfWidth + 0.3f;
                bool onRight = py > platformSurfaceY - 0.5f && Mathf.Abs(px - platformRightX) < platformHalfWidth + 0.3f;
                if (onLeft || onRight)
                    attackTarget.GetComponent<PlayerController>()?.DamagedBy(18f * 0.12f);
            }

            elapsed += 0.12f;
            yield return new WaitForSeconds(0.12f);
        }

        Destroy(hazL);
        Destroy(hazR);
        isZoneAttackActive = false;
        attackTimer = 5.0f;
    }

    private IEnumerator FireDoubleNova()
    {
        int count = isEnraged ? 24 : 16;
        PlayShootSound();
        FireRing(count, 0f, bulletSpeed);
        yield return new WaitForSeconds(0.14f);
        if (!isDead)
        {
            PlayShootSound();
            FireRing(count, 360f / count * 0.5f, bulletSpeed);
        }
    }

    private IEnumerator FireSpiralGalaxy()
    {
        int pairs = isEnraged ? 36 : 26;
        float angle = 0f;
        float step  = 360f / pairs * 2.2f;
        for (int i = 0; i < pairs; i++)
        {
            if (isDead) yield break;
            float r1 = angle * Mathf.Deg2Rad;
            float r2 = (angle + 180f) * Mathf.Deg2Rad;
            SpawnBullet(transform.position, new Vector2(Mathf.Cos(r1), Mathf.Sin(r1)), bulletSpeed);
            SpawnBullet(transform.position, new Vector2(Mathf.Cos(r2), Mathf.Sin(r2)), bulletSpeed);
            if (i % 3 == 0) PlayShootSound();
            angle += step;
            yield return new WaitForSeconds(0.045f);
        }
    }

    private IEnumerator FireEightWayBurst()
    {
        for (int wave = 0; wave < 3; wave++)
        {
            if (isDead) yield break;
            PlayShootSound();
            for (int i = 0; i < 8; i++)
            {
                float rad = i * 45f * Mathf.Deg2Rad;
                SpawnBullet(transform.position, new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)), bulletSpeed * 1.05f);
            }
            yield return new WaitForSeconds(0.12f);
        }

        if (!isDead && attackTarget != null)
        {
            PlayShootSound();
            Vector2 toPlayer = (attackTarget.position - transform.position).normalized;
            float baseRad    = Mathf.Atan2(toPlayer.y, toPlayer.x);
            for (int j = -2; j <= 2; j++)
            {
                float rad = baseRad + j * 12f * Mathf.Deg2Rad;
                SpawnBullet(transform.position, new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)), bulletSpeed * 1.3f);
            }
        }
    }

    private IEnumerator FireCrossBloom()
    {
        PlayShootSound();
        for (int branch = 0; branch < 4; branch++)
        {
            float center = branch * 90f + 45f;
            for (int petal = -2; petal <= 2; petal++)
            {
                float rad = (center + petal * 14f) * Mathf.Deg2Rad;
                SpawnBullet(transform.position, new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)), bulletSpeed);
            }
        }
        yield return null;
    }

    private void FireRing(int count, float startAngleDeg, float spd)
    {
        float step = 360f / count;
        for (int i = 0; i < count; i++)
        {
            float rad = (i * step + startAngleDeg) * Mathf.Deg2Rad;
            SpawnBullet(transform.position, new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)), spd);
        }
    }

    private void SpawnBullet(Vector3 pos, Vector2 dir, float spd, bool ignoreObstacles = true)
    {
        if (bulletPrefab == null) return;
        GameObject go = Instantiate(bulletPrefab, pos, Quaternion.identity);
        Bullet b = go.GetComponent<Bullet>();
        if (b != null)
        {
            b.LaunchBossBullet(dir, spd, attack);
            b.ignoreObstacles = ignoreObstacles;
        }
    }

    private void PlayShootSound()
    {
        AudioManager.Instance?.PlayEnemyShoot(transform.position);
    }

    private void PlayEnrageSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBossEnrage(enrageSound);
        }
    }

    private GameObject CreateZoneBox(Vector3 center, Vector2 size, Color color, float flashSpeed = 8f)
    {
        GameObject obj = new GameObject("BossZone");
        obj.transform.position   = center;
        obj.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite       = GetBoxSprite();
        sr.color        = color;
        sr.sortingOrder = 30;

        TelegraphFlashing tf = obj.AddComponent<TelegraphFlashing>();
        tf.sr    = sr;
        tf.speed = flashSpeed;

        return obj;
    }

    private static Sprite GetBoxSprite()
    {
        if (boxSprite != null) return boxSprite;
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixels(new[] { Color.white });
        tex.Apply();
        boxSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return boxSprite;
    }
}
