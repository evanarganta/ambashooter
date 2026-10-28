using UnityEngine;

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

    private float shootingTime = 0f;
    private bool isShooting = false;
    private bool isGrounded = true;
    private bool isDead;
    private Rigidbody2D rb2;

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
    }

    void Update()
    {
        if (canBeMoved)
        {
            MovementController();
            JumpController();
            ShootController();
        }

        FallDie();
        AnimationController();
    }

    void MovementController()
    {
        float x = Input.GetAxisRaw("Horizontal");

        Vector2 velocity = rb2.linearVelocity;
        velocity.x = x * moveSpeed;
        rb2.linearVelocity = velocity;

        if (velocity.x < 0)
        {
            graphic.flipX = true;
        }
        else if (velocity.x > 0)
        {
            graphic.flipX = false;
        }
    }

    void JumpController()
    {
        // Cast a ray downward to check for ground
        RaycastHit2D ray = Physics2D.Raycast(transform.position, Vector2.down, 10f, LayerMask.GetMask("Obstacle"));

        if (ray.collider != null && ray.distance < minGroundDistance)
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }

        // Jump when Up Arrow, W, or Spacebar is pressed
        if ((Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space)) && isGrounded)
        {
            Jump();
        }
    }

    void Jump()
    {
        // Reset vertical velocity before jumping for consistent jump height
        rb2.linearVelocity = new Vector2(rb2.linearVelocity.x, 0);
        rb2.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
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
        isShooting = Input.GetKey(KeyCode.Z);
        shootingTime -= Time.deltaTime;

        if (isShooting && shootingTime <= 0)
        {
            shootingTime = shootingTimeMax;
            Shoot();
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
    }

    public void DamagedBy(float damage)
    {
        if (isDead)
        {
            return;
        }

        health = Mathf.Max(0f, health - damage);
        if (health <= 0f)
        {
            Die();
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
            graphic.enabled = false;
        }

        canBeMoved = false;
        if (rb2 != null)
        {
            rb2.linearVelocity = Vector2.zero;
        }

        GameManager.GameOver();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        EnemyController enemy = collision.collider.GetComponentInParent<EnemyController>();
        if (enemy != null)
        {
            DamagedBy(enemy.GetAttackDamage());
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