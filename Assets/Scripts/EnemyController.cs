using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Status")]
    public float health = 20f;
    public float attack = 5f;
    public Transform attackTarget;

    [Header("Configuration")]
    [SerializeField] private float moveSpeed = 2.5f;
    public bool isDead { get; private set; }
    public bool isContactDamageImmune = false;

    [Header("Damage Blink")]
    [SerializeField] private float invulnerabilityDuration = 0.4f;

    protected Rigidbody2D rb2;
    protected float knockbackTimer = 0f;
    private float invulnerabilityTimer = 0f;
    private SpriteRenderer cachedSr;

    protected virtual void Awake()
    {
        rb2 = GetComponent<Rigidbody2D>();
        cachedSr = GetComponentInChildren<SpriteRenderer>();
    }

    protected virtual void Update()
    {
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.deltaTime;
            if (rb2 != null)
            {
                float newX = Mathf.MoveTowards(rb2.linearVelocity.x, 0f, 15f * Time.deltaTime);
                rb2.linearVelocity = new Vector2(newX, rb2.linearVelocity.y);
            }
        }
        else
        {
            Movement();
        }

        if (invulnerabilityTimer > 0f)
        {
            invulnerabilityTimer -= Time.deltaTime;
            if (cachedSr != null)
            {
                float alpha = (Mathf.FloorToInt(invulnerabilityTimer * 20f) % 2 == 0) ? 0.35f : 1f;
                Color c = cachedSr.color;
                c.a = alpha;
                cachedSr.color = c;
            }
        }
        else if (cachedSr != null && cachedSr.color.a < 1f)
        {
            Color c = cachedSr.color;
            c.a = 1f;
            cachedSr.color = c;
        }

        FallDie();
    }

    protected virtual void Movement()
    {
    }

    public virtual void ApplyKnockback(Vector2 direction, float force)
    {
        if (isDead)
        {
            return;
        }

        if (rb2 == null)
        {
            rb2 = GetComponent<Rigidbody2D>();
        }

        if (rb2 != null)
        {
            rb2.linearVelocity = new Vector2(direction.x * force, 2.5f);
            knockbackTimer = 0.25f;
        }
    }

    public void DamagedBy(float damage)
    {
        if (isDead || invulnerabilityTimer > 0f)
        {
            return;
        }

        health -= damage;
        if (health <= 0)
        {
            health = 0;
            Die();
        }
        else
        {
            invulnerabilityTimer = invulnerabilityDuration;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayEnemyHurt(transform.position);
            }
        }
    }

    void FallDie()
    {
        if (transform.position.y < -20)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemyDie(transform.position);
        }
        StopAllCoroutines();
        ScoreManager.DefeatEnemy();
        Destroy(gameObject);
    }

    public float GetAttackDamage()
    {
        return attack;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Bullet"))
        {
            Bullet bullet = collision.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.TryHitTarget(collision);
            }
        }
    }
}
