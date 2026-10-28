using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float damage = 2f;
    [SerializeField] private float dieTime = 3f; // Despawns after 3 seconds if it doesn't hit anything

    public string targetTag = "Enemy";

    private Vector2 direction;
    private Rigidbody2D rb2;
    private Collider2D bulletCollider;
    private bool hasHit;

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
}