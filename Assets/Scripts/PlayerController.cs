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

    [Header("Configuration")]
    [SerializeField] private float moveSpeed = 2.5f;

    private Rigidbody2D rb2;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        rb2 = GetComponent<Rigidbody2D>();
        healthMax = health;
    }

    void Update()
    {
        if (canBeMoved)
        {
            MovementController();
        }
    }

    void MovementController()
    {
        float x = Input.GetAxisRaw("Horizontal");

        Vector2 velocity = rb2.linearVelocity;
        velocity.x = x * moveSpeed;
        rb2.linearVelocity = velocity;

        // Flip sprite based on movement direction
        if (velocity.x < 0)
        {
            graphic.flipX = true;
        }
        else if (velocity.x > 0)
        {
            graphic.flipX = false;
        }
    }
}