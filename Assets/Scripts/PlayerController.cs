using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Properties")]
    [SerializeField] private SpriteRenderer graphic;

    [Header("Status")]
    public float health = 100f;
    public float attack = 5f;
    public bool canBeMoved = true;
    public float healthMax { get; private set; }

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;

    [Header("Shooting")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private float shootingTimeMax = 0.3f; // Time between shots
    [SerializeField] private Vector2 gunOffset = new Vector2(0.75f, -0.2f); // Spawn position relative to player

    private float shootingTime = 0f;
    private bool isShooting = false;
    private Rigidbody2D rb2;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        rb2 = GetComponent<Rigidbody2D>();
        shootingTime = 0f;
        healthMax = health;
    }

    void Update()
    {
        if (canBeMoved)
        {
            MovementController();
            ShootController();
        }
    }

    void MovementController()
    {
        float x = Input.GetAxisRaw("Horizontal");

        Vector2 velocity = rb2.linearVelocity;
        velocity.x = x * moveSpeed;
        rb2.linearVelocity = velocity;

        // Flip sprite based on direction
        if (velocity.x < 0)
        {
            graphic.flipX = true;
        }
        else if (velocity.x > 0)
        {
            graphic.flipX = false;
        }
    }

    void ShootController()
    {
        // Press or hold Z to shoot
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
        // 1 = facing right, -1 = facing left
        int direction = graphic.flipX ? -1 : 1;

        // Calculate gun spawn point based on player face direction
        Vector2 gunPos = new Vector2(
            transform.position.x + (gunOffset.x * direction),
            transform.position.y + gunOffset.y
        );

        // Spawn bullet
        GameObject bulletObj = Instantiate(bulletPrefab, gunPos, Quaternion.identity);
        Bullet bullet = bulletObj.GetComponent<Bullet>();

        if (bullet != null)
        {
            bullet.Launch(new Vector2(direction, 0), "Enemy", bulletSpeed, attack);
        }
    }
}