using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossMushroom : EnemyController
{
    public static BossMushroom Instance { get; private set; }

    [Header("Boss Specifics")]
    public string bossName = "ELDER MUSHROOM";
    public float maxHealth = 200f;

    [Header("Combat Settings")]
    [SerializeField] private SpriteRenderer graphic;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 6.0f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float homeSpeed = 1.7f;
    [SerializeField] private float climbThresholdY = 25f;

    private float attackTimer = 1.0f;
    private int attackPatternIndex = 0;
    private bool isInCombatPhase = false;
    private bool isEnraged = false;

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
        health = maxHealth;
        isContactDamageImmune = true; // Boss takes NO damage from bumps/stomps
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Start()
    {
        if (attackTarget == null && PlayerController.Instance != null)
        {
            attackTarget = PlayerController.Instance.transform;
        }

        if (rb2 != null)
        {
            rb2.gravityScale = 0f;
            rb2.linearVelocity = Vector2.zero;
        }
    }

    // Boss cannot be knocked back
    public override void ApplyKnockback(Vector2 direction, float force)
    {
        // Immune to knockback
    }

    protected override void Update()
    {
        base.Update();

        if (isDead) return;

        if (rb2 != null)
        {
            rb2.linearVelocity = Vector2.zero;
        }

        // Phase 2 enrage
        if (!isEnraged && health <= maxHealth * 0.5f)
        {
            isEnraged = true;
            attackCooldown = 1.0f;
            homeSpeed = 2.4f;
            bulletSpeed = 7.0f;
            if (graphic != null)
            {
                graphic.color = new Color(1f, 0.35f, 0.35f);
            }
        }
    }

    protected override void Movement()
    {
        if (attackTarget == null)
        {
            if (PlayerController.Instance != null)
            {
                attackTarget = PlayerController.Instance.transform;
            }
            return;
        }

        // Engage combat phase once player climbs to summit
        if (!isInCombatPhase && attackTarget.position.y >= climbThresholdY)
        {
            isInCombatPhase = true;
        }

        if (graphic != null)
        {
            graphic.flipX = attackTarget.position.x < transform.position.x;
        }

        if (isInCombatPhase)
        {
            // Arena Combat Phase: Continuously drift and home directly towards the player
            transform.position = Vector2.MoveTowards(transform.position, attackTarget.position, homeSpeed * Time.deltaTime);

            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                ExecuteCrazyBossAttack();
                attackTimer = attackCooldown;
            }
        }
        else
        {
            // Climb Phase: Hover at summit and fire downward barrages
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                ExecuteClimbBarrage();
                attackTimer = 1.6f;
            }
        }
    }

    // Climb phase downward barrage
    private void ExecuteClimbBarrage()
    {
        int count = isEnraged ? 8 : 6;
        float startAngle = -145f;
        float endAngle = -35f;
        float step = (endAngle - startAngle) / (count - 1);

        for (int i = 0; i < count; i++)
        {
            float angleDeg = startAngle + step * i;
            float rad = angleDeg * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;

            SpawnBullet(transform.position + new Vector3(dir.x * 0.9f, dir.y * 0.9f, 0f), dir, bulletSpeed * 0.9f);
        }

        PlayShootSound();
    }

    // Bullet Hell Combat Attacks
    private void ExecuteCrazyBossAttack()
    {
        switch (attackPatternIndex % 4)
        {
            case 0:
                // Pattern 0: Interlocking Double Nova (32 to 48 bullets in dual rings)
                StartCoroutine(FireDoubleNova(isEnraged ? 24 : 16));
                break;

            case 1:
                // Pattern 1: Rapid 2-Arm Spinning Spiral Galaxy (28 to 36 bullets)
                StartCoroutine(FireSpinningSpiralGalaxy(isEnraged ? 36 : 26));
                break;

            case 2:
                // Pattern 2: 8-Way Cardinal/Diagonal Laser Streams + Targeted Shotgun Burst
                StartCoroutine(FireEightWayStreamsWithTargetedBurst());
                break;

            case 3:
                // Pattern 3: Four-Way Cross Petal Bloom (expanding flower pattern)
                StartCoroutine(FireCrossBloom());
                break;
        }

        attackPatternIndex++;
    }

    // Pattern 0: Interlocking Double Nova Ring
    private IEnumerator FireDoubleNova(int ringBullets)
    {
        PlayShootSound();
        FireRing(ringBullets, 0f, bulletSpeed);

        yield return new WaitForSeconds(0.14f);

        if (!isDead)
        {
            PlayShootSound();
            float offsetAngle = (360f / ringBullets) * 0.5f;
            FireRing(ringBullets, offsetAngle, bulletSpeed);
        }
    }

    // Pattern 1: Spinning Galaxy Spiral (Fires dual opposing streams spinning 360 degrees)
    private IEnumerator FireSpinningSpiralGalaxy(int totalPairs)
    {
        float angle = 0f;
        float angleStep = 360f / totalPairs * 2.2f;

        for (int i = 0; i < totalPairs; i++)
        {
            if (isDead) yield break;

            float rad1 = angle * Mathf.Deg2Rad;
            float rad2 = (angle + 180f) * Mathf.Deg2Rad;

            Vector2 dir1 = new Vector2(Mathf.Cos(rad1), Mathf.Sin(rad1)).normalized;
            Vector2 dir2 = new Vector2(Mathf.Cos(rad2), Mathf.Sin(rad2)).normalized;

            SpawnBullet(transform.position + (Vector3)(dir1 * 0.85f), dir1, bulletSpeed);
            SpawnBullet(transform.position + (Vector3)(dir2 * 0.85f), dir2, bulletSpeed);

            if (i % 3 == 0) PlayShootSound();

            angle += angleStep;
            yield return new WaitForSeconds(0.045f);
        }
    }

    // Pattern 2: 8-Way Stream + Direct Player Shotgun Blast
    private IEnumerator FireEightWayStreamsWithTargetedBurst()
    {
        // 8-way bursts in 3 sequential waves (laser streams)
        for (int wave = 0; wave < 3; wave++)
        {
            if (isDead) yield break;

            PlayShootSound();
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;
                float rad = angle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
                SpawnBullet(transform.position + (Vector3)(dir * 0.9f), dir, bulletSpeed * 1.05f);
            }

            yield return new WaitForSeconds(0.12f);
        }

        // Targeted 5-bullet shotgun fan towards the player
        if (!isDead && attackTarget != null)
        {
            PlayShootSound();
            Vector2 toPlayer = (attackTarget.position - transform.position).normalized;
            float baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

            for (int j = -2; j <= 2; j++)
            {
                float angle = (baseAngle + j * 12f) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
                SpawnBullet(transform.position + (Vector3)(dir * 0.9f), dir, bulletSpeed * 1.25f);
            }
        }
    }

    // Pattern 3: 4-Way Cross Petal Bloom (Flower pattern branching into quadrants)
    private IEnumerator FireCrossBloom()
    {
        PlayShootSound();

        for (int branch = 0; branch < 4; branch++)
        {
            float branchCenterAngle = branch * 90f + 45f;

            for (int petal = -2; petal <= 2; petal++)
            {
                float angle = (branchCenterAngle + petal * 14f) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
                SpawnBullet(transform.position + (Vector3)(dir * 0.9f), dir, bulletSpeed);
            }
        }

        yield return null;
    }

    private void FireRing(int count, float startAngleOffset, float speed)
    {
        float step = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = i * step + startAngleOffset;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;

            SpawnBullet(transform.position + (Vector3)(dir * 0.9f), dir, speed);
        }
    }

    private void SpawnBullet(Vector3 pos, Vector2 dir, float speed)
    {
        if (bulletPrefab == null) return;

        GameObject bulletObj = Instantiate(bulletPrefab, pos, Quaternion.identity);
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.Launch(dir, "Player", speed, attack);
        }
    }

    private void PlayShootSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemyShoot(transform.position);
        }
    }
}
