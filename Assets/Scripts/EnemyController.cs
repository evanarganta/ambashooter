using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Status")]
    public float health = 20f;
    public float attack = 5f;
    public Transform attackTarget;

    [Header("Configuration")]
    [SerializeField] private float moveSpeed = 2.5f;
    private bool isDead;

    void Update()
    {
        Movement();
        FallDie();
    }

    protected virtual void Movement()
    {
        // To be overridden by specific enemy types
    }

    public void DamagedBy(float damage)
    {
        if (isDead)
        {
            return;
        }

        health -= damage;
        if (health <= 0)
        {
            health = 0;
            Die();
        }
    }

    void FallDie()
    {
        // Destroy if it falls off the map
        if (transform.position.y < -20)
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