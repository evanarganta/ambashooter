using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Properties")]
    [SerializeField] private SpriteRenderer graphic;
    [SerializeField] private Animator animator;

    [Header("Status")]
    public float health = 100f;
    public float attack = 5f;
    public bool canBeMoved = true;
    public float healthMax { get; private set; }

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float minGroundDistance = 1.2f;

    [Header("Shooting")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private float shootingTimeMax = 0.3f;
    [SerializeField] private Vector2 gunOffset = new Vector2(0.75f, -0.2f);

    [Header("Parry")]
    [SerializeField] private float parryRadius = 0.85f;
    [SerializeField] private float parryCooldown = 1f;
    [SerializeField] private float parrySpeedMultiplier = 2f;
    [SerializeField] private Sprite parrySprite;
    [SerializeField] private float parrySpriteHoldTime = 0.18f;

    [Header("Knockback & Contact")]
    [SerializeField] private float knockbackForceX = 3f;
    [SerializeField] private float knockbackForceY = 3.5f;
    [SerializeField] private float enemyKnockbackForce = 4.5f;
    [SerializeField] private float invulnerabilityDuration = 0.4f;
    [SerializeField] private float contactDamage = 5f;

    private float shootingTime = 0f;
    private bool isShooting = false;
    private bool isGrounded = true;
    private bool isDead;
    private float knockbackTimer = 0f;
    private float invulnerabilityTimer = 0f;
    private float parryTimer = 0f;
    private Rigidbody2D rb2;

    public float ParryCooldown => parryCooldown;
    public float ParryTimer => parryTimer;
    public float ParryCooldownProgress => parryCooldown > 0f ? Mathf.Clamp01(1f - (parryTimer / parryCooldown)) : 1f;
    public bool IsParryReady => parryTimer <= 0f;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Start()
    {
        rb2 = GetComponent<Rigidbody2D>();
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        healthMax = health;

        ApplyFrictionlessMaterial();

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Level 6" || FindObjectOfType<BossMushroom>() != null)
        {
            SetInvulnerable(5f);
        }
    }

    private static PhysicsMaterial2D cachedFrictionlessMat;

    private void ApplyFrictionlessMaterial()
    {
        if (cachedFrictionlessMat == null)
        {
            cachedFrictionlessMat = new PhysicsMaterial2D("PlayerNoFriction")
            {
                friction = 0f,
                bounciness = 0f
            };
        }

        if (rb2 != null)
        {
            rb2.sharedMaterial = cachedFrictionlessMat;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.sharedMaterial = cachedFrictionlessMat;
        }
    }

    void Update()
    {
        if (invulnerabilityTimer > 0f)
        {
            invulnerabilityTimer -= Time.deltaTime;
            if (graphic != null && !isDead)
            {
                float alpha = (Mathf.FloorToInt(invulnerabilityTimer * 20f) % 2 == 0) ? 0.35f : 1f;
                Color c = graphic.color;
                c.a = alpha;
                graphic.color = c;
            }
        }
        else if (graphic != null && !isDead && graphic.color.a < 1f)
        {
            Color c = graphic.color;
            c.a = 1f;
            graphic.color = c;
        }

        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.deltaTime;
        }

        if (canBeMoved)
        {
            MovementController();
            JumpController();
            ShootController();
            ParryController();
        }

        FallDie();
        AnimationController();
    }

    void MovementController()
    {
        if (knockbackTimer > 0f)
        {
            return;
        }

        float x = Input.GetAxisRaw("Horizontal");

        Vector2 velocity = rb2.linearVelocity;
        if (x != 0f)
        {
            if (!isGrounded && IsPushingAgainstWall(x))
            {
                velocity.x = 0f;
            }
            else
            {
                velocity.x = x * moveSpeed;
            }
        }
        else
        {
            velocity.x = Mathf.MoveTowards(velocity.x, 0f, moveSpeed * 15f * Time.deltaTime);
        }
        rb2.linearVelocity = velocity;

        if (x < 0)
        {
            graphic.flipX = true;
        }
        else if (x > 0)
        {
            graphic.flipX = false;
        }
    }

    private bool IsPushingAgainstWall(float dirX)
    {
        Vector2 direction = Vector2.right * Mathf.Sign(dirX);
        float checkDistance = 0.35f;
        int obstacleMask = LayerMask.GetMask("Obstacle");

        Vector2 upperOrigin = (Vector2)transform.position + Vector2.up * 0.25f;
        Vector2 lowerOrigin = (Vector2)transform.position + Vector2.down * 0.15f;

        RaycastHit2D hitUpper = Physics2D.Raycast(upperOrigin, direction, checkDistance, obstacleMask);
        RaycastHit2D hitLower = Physics2D.Raycast(lowerOrigin, direction, checkDistance, obstacleMask);

        return (hitUpper.collider != null || hitLower.collider != null);
    }

    void JumpController()
    {
        if (knockbackTimer > 0f)
        {
            return;
        }

        RaycastHit2D ray = Physics2D.Raycast(transform.position, Vector2.down, 10f, LayerMask.GetMask("Obstacle"));

        if (ray.collider != null && ray.distance < minGroundDistance)
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }

        if ((Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space)) && isGrounded)
        {
            Jump();
        }
    }

    void Jump()
    {
        rb2.linearVelocity = new Vector2(rb2.linearVelocity.x, 0);
        rb2.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPlayerJump();
        }
    }

    void AnimationController()
    {
        if (animator == null || rb2 == null)
        {
            return;
        }

        int velocityX = rb2.linearVelocity.x > 0f ? 1 : rb2.linearVelocity.x < 0f ? -1 : 0;
        int velocityY = rb2.linearVelocity.y > 0f ? 1 : rb2.linearVelocity.y < 0f ? -1 : 0;

        animator.SetBool("isGrounded", isGrounded);
        animator.SetInteger("velocityX", velocityX);
        animator.SetInteger("velocityY", velocityY);
    }

    void ShootController()
    {
        isShooting = Input.GetKey(KeyCode.Z) || Input.GetMouseButton(0);
        shootingTime -= Time.deltaTime;

        if (isShooting && shootingTime <= 0)
        {
            shootingTime = shootingTimeMax;
            Shoot();
        }
    }

    void ParryController()
    {
        if (parryTimer > 0f)
        {
            parryTimer -= Time.deltaTime;
        }

        bool parryPressed = Input.GetKeyDown(KeyCode.X) || Input.GetMouseButtonDown(1);
        if (!parryPressed || parryTimer > 0f)
        {
            return;
        }

        parryTimer = parryCooldown;

        float facingSign = (graphic != null && graphic.flipX) ? -1f : 1f;

        bool parried = false;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, parryRadius);

        Bullet closestBullet = null;
        float closestDistSq = float.MaxValue;

        Bullet closestFriendly = null;
        float closestFriendlyDistSq = float.MaxValue;

        foreach (Collider2D col in hits)
        {
            if (!col.CompareTag("Bullet"))
            {
                continue;
            }

            Bullet bullet = col.GetComponent<Bullet>();
            if (bullet == null)
            {
                continue;
            }

            float bulletSide = col.transform.position.x - transform.position.x;
            if (bulletSide * facingSign < 0f)
            {
                continue;
            }

            float distSq = ((Vector2)col.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (bullet.targetTag == "Player")
            {
                if (distSq < closestDistSq)
                {
                    closestDistSq = distSq;
                    closestBullet = bullet;
                }
            }
            else if (bullet.targetTag == "Enemy")
            {
                if (distSq < closestFriendlyDistSq)
                {
                    closestFriendlyDistSq = distSq;
                    closestFriendly = bullet;
                }
            }
        }

        if (closestBullet == null)
        {
            closestBullet = closestFriendly;
        }

        if (closestBullet != null)
        {
            if (closestBullet.targetTag == "Player")
            {
                if (closestBullet.isBossBullet)
                {
                    Heal(closestBullet.bossParryHeal);
                    closestBullet.ReflectBossBullet();
                }
                else
                {
                    Heal(Mathf.Max(closestBullet.GetDamage() * 2f, 10f));
                    closestBullet.Reflect("Enemy", parrySpeedMultiplier);
                }
                parried = true;
            }
            else if (closestBullet.targetTag == "Enemy")
            {
                Vector2 boostDir = new Vector2(facingSign, 0f);
                closestBullet.ParryBoost(boostDir, "Enemy", parrySpeedMultiplier, 1.5f);
                parried = true;
            }
        }

        if (parried)
        {
            UIManager.TriggerParryFlash();
            StartCoroutine(ParryVisual());
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayParrySuccess();
            }
        }
        else
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayParryMiss();
            }
        }
    }

    private static bool HasAnimatorParam(Animator anim, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in anim.parameters)
            if (p.name == name && p.type == type) return true;
        return false;
    }

    private IEnumerator ParryVisual()
    {
        if (animator != null && HasAnimatorParam(animator, "parry", AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger("parry");
        }

        if (graphic != null && parrySprite != null)
        {
            Sprite original = graphic.sprite;
            bool animWasEnabled = animator != null && animator.enabled;

            if (animWasEnabled) animator.enabled = false;
            graphic.sprite = parrySprite;

            yield return new WaitForSeconds(parrySpriteHoldTime);

            graphic.sprite = original;
            if (animWasEnabled) animator.enabled = true;
        }
        else
        {
            yield return null;
        }
    }

    void Shoot()
    {
        int direction = graphic.flipX ? -1 : 1;

        Vector2 gunPos = new Vector2(
            transform.position.x + (gunOffset.x * direction),
            transform.position.y + gunOffset.y
        );

        GameObject bulletObj = Instantiate(bulletPrefab, gunPos, Quaternion.identity);
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.Launch(new Vector2(direction, 0), "Enemy", bulletSpeed, attack);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPlayerShoot();
        }
    }

    public void Bounce()
    {
        if (isDead || rb2 == null)
        {
            return;
        }

        rb2.linearVelocity = new Vector2(rb2.linearVelocity.x, 0f);
        rb2.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPlayerJump();
        }
    }

    public void Heal(float amount)
    {
        if (isDead || amount <= 0f) return;
        health = Mathf.Min(healthMax, health + amount);
    }

    public void SetInvulnerable(float duration)
    {
        invulnerabilityTimer = Mathf.Max(invulnerabilityTimer, duration);
    }

    public void DamagedBy(float damage)
    {
        if (isDead || invulnerabilityTimer > 0f)
        {
            return;
        }

        health = Mathf.Max(0f, health - damage);
        invulnerabilityTimer = invulnerabilityDuration;
        if (health <= 0f)
        {
            Die();
        }
        else
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPlayerHurt();
            }
        }
    }

    void FallDie()
    {
        if (transform.position.y < -20f)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        if (graphic != null)
        {
            Color c = graphic.color;
            c.a = 1f;
            graphic.color = c;
            graphic.enabled = false;
        }

        canBeMoved = false;
        if (rb2 != null)
        {
            rb2.linearVelocity = Vector2.zero;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPlayerDie();
        }

        GameManager.GameOver();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        EnemyController enemy = collision.collider.GetComponentInParent<EnemyController>();
        if (enemy != null)
        {
            HandleEnemyCollision(collision, enemy);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (invulnerabilityTimer > 0f)
        {
            return;
        }

        EnemyController enemy = collision.collider.GetComponentInParent<EnemyController>();
        if (enemy != null)
        {
            HandleEnemyCollision(collision, enemy);
        }
    }

    private void HandleEnemyCollision(Collision2D collision, EnemyController enemy)
    {
        if (isDead || enemy == null || enemy.isDead)
        {
            return;
        }

        bool isSteppedOn = false;
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);
            if (contact.normal.y > 0.5f)
            {
                isSteppedOn = true;
                break;
            }
        }

        if (!isSteppedOn && transform.position.y > enemy.transform.position.y + 0.3f && rb2.linearVelocity.y <= 0.5f)
        {
            isSteppedOn = true;
        }

        if (invulnerabilityTimer <= 0f)
        {
            float playerDamageToTake = enemy.GetAttackDamage();
            if (isSteppedOn && !enemy.isContactDamageImmune)
            {
                playerDamageToTake *= 0.5f;
            }

            DamagedBy(playerDamageToTake);

            if (!enemy.isContactDamageImmune)
            {
                float enemyDamageToTake = contactDamage > 0f ? contactDamage : attack;
                enemy.DamagedBy(enemyDamageToTake);
            }
        }

        if (enemy.isContactDamageImmune)
        {
            if (!isDead)
            {
                float pushDirX = transform.position.x < enemy.transform.position.x ? -1f : 1f;
                if (Mathf.Abs(transform.position.x - enemy.transform.position.x) < 0.05f)
                {
                    pushDirX = graphic != null && graphic.flipX ? 1f : -1f;
                }

                knockbackTimer = 0.65f;
                rb2.linearVelocity = new Vector2(pushDirX * knockbackForceX, knockbackForceY);
            }
        }
        else if (isSteppedOn)
        {
            if (!isDead)
            {
                Bounce();
            }
        }
        else
        {
            if (!isDead)
            {
                float pushDirX = transform.position.x < enemy.transform.position.x ? -1f : 1f;
                if (Mathf.Abs(transform.position.x - enemy.transform.position.x) < 0.05f)
                {
                    pushDirX = graphic != null && graphic.flipX ? 1f : -1f;
                }

                knockbackTimer = 0.65f;
                rb2.linearVelocity = new Vector2(pushDirX * knockbackForceX, knockbackForceY);
            }

            float enemyPushDirX = transform.position.x < enemy.transform.position.x ? 1f : -1f;
            enemy.ApplyKnockback(new Vector2(enemyPushDirX, 0f), enemyKnockbackForce);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Bullet"))
        {
            return;
        }

        Bullet bullet = collision.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.TryHitTarget(collision);
        }
    }
}