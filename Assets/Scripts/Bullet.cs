using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float damage = 2f;
    [SerializeField] private float dieTime = 3f;

    public string targetTag = "Enemy";
    public bool ignoreObstacles = false;

    [Header("Boss Bullet Features")]
    public bool isBossBullet = false;
    public float bossReflectDamage = 25f;
    public float bossParryHeal = 30f;

    private Vector2 direction;
    private Rigidbody2D rb2;
    private Collider2D bulletCollider;
    private bool hasHit;
    private Transform homingTarget;

    void Awake()
    {
        rb2 = GetComponent<Rigidbody2D>();
        bulletCollider = GetComponent<Collider2D>();
    }

    void Update()
    {
        dieTime -= Time.deltaTime;
        if (dieTime <= 0)
        {
            Destroy(gameObject);
        }
    }

    void FixedUpdate()
    {
        if (homingTarget != null)
        {
            Vector2 toTarget = ((Vector2)homingTarget.position - rb2.position).normalized;
            direction = Vector2.Lerp(direction, toTarget, 14f * Time.fixedDeltaTime).normalized;
        }

        Vector2 normalizedDirection = direction.normalized;
        if (normalizedDirection == Vector2.zero)
        {
            return;
        }

        float distance = speed * Time.fixedDeltaTime;
        float radius = Mathf.Max(bulletCollider.bounds.extents.x, bulletCollider.bounds.extents.y);
        int targetLayer = LayerMask.GetMask(targetTag);

        Collider2D overlappingTarget = Physics2D.OverlapCircle(rb2.position, radius, targetLayer);
        if (overlappingTarget != null)
        {
            TryHitTarget(overlappingTarget);
            return;
        }

        RaycastHit2D hit = Physics2D.CircleCast(rb2.position, radius, normalizedDirection, distance, targetLayer);
        if (hit.collider != null)
        {
            TryHitTarget(hit.collider);
            return;
        }

        if (!ignoreObstacles)
        {
            int obstacleLayer = LayerMask.GetMask("Obstacle");
            RaycastHit2D wallHit = Physics2D.CircleCast(rb2.position, radius, normalizedDirection, distance, obstacleLayer);
            if (wallHit.collider != null)
            {
                hasHit = true;
                bulletCollider.enabled = false;
                Destroy(gameObject);
                return;
            }
        }

        rb2.MovePosition(rb2.position + normalizedDirection * distance);
    }

    public void TryHitTarget(Collider2D target)
    {
        if (hasHit)
        {
            return;
        }

        if (targetTag == "Enemy")
        {
            EnemyController enemy = target.GetComponentInParent<EnemyController>();
            if (enemy == null)
            {
                return;
            }

            enemy.DamagedBy(damage);
        }
        else if (targetTag == "Player")
        {
            PlayerController player = target.GetComponentInParent<PlayerController>();
            if (player == null)
            {
                return;
            }

            player.DamagedBy(damage);

            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            CapsuleCollider2D playerCol = player.GetComponent<CapsuleCollider2D>();
            float playerBottom = playerCol != null ? playerCol.bounds.min.y : player.transform.position.y;

            bool isJumpingOnBullet = playerRb != null && playerRb.linearVelocity.y < -0.1f && playerBottom >= (transform.position.y - 0.05f);

            if (isJumpingOnBullet)
            {
                player.Bounce();
            }
        }
        else
        {
            return;
        }

        hasHit = true;
        bulletCollider.enabled = false;
        Destroy(gameObject);
    }

    public void Launch(Vector2 direction, string targetTag, float speed, float damage)
    {
        this.direction = direction;
        this.speed = speed;
        this.damage = damage;
        this.targetTag = targetTag;
    }

    public float GetDamage()
    {
        return damage;
    }

    public void SetDieTime(float t)
    {
        dieTime = t;
    }

    public void ParryBoost(Vector2 newDirection, string newTargetTag, float speedMultiplier = 1f, float damageBonus = 1.5f)
    {
        if (hasHit) return;
        direction = newDirection;
        targetTag = newTargetTag;
        speed *= speedMultiplier;
        damage *= damageBonus;
        dieTime = 3f;
    }

    public void LaunchBossBullet(Vector2 direction, float speed, float damage)
    {
        isBossBullet = true;
        ignoreObstacles = true;
        this.direction = direction;
        this.speed = speed;
        this.damage = damage;
        this.targetTag = "Player";

        transform.localScale = Vector3.one * 2.0f;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = new Color(1f, 0.28f, 0.4f, 1f);
        }

        if (bulletCollider is CircleCollider2D circle)
        {
            circle.radius *= 1.35f;
        }
    }

    public void ReflectBossBullet()
    {
        if (hasHit) return;

        isBossBullet = true;
        ignoreObstacles = true;
        targetTag = "Enemy";
        damage = bossReflectDamage;
        speed = Mathf.Max(speed * 1.6f, 12f);
        dieTime = 4f;

        if (BossMushroom.Instance != null && !BossMushroom.Instance.isDead)
        {
            homingTarget = BossMushroom.Instance.transform;
            direction = ((Vector2)BossMushroom.Instance.transform.position - rb2.position).normalized;
        }
        else
        {
            direction = -direction;
        }

        transform.localScale = Vector3.one * 2.5f;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = new Color(0f, 1f, 1f, 1f);
        }
    }

    public void Reflect(string newTargetTag, float speedMultiplier = 1f)
    {
        if (hasHit) return;
        direction = -direction;
        targetTag = newTargetTag;
        speed *= speedMultiplier;
        dieTime = 3f;
    }
}