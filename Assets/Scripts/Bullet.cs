using UnityEngine;
using System.Collections;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float damage = 2f;
    [SerializeField] private float dieTime = 3f; // Despawns after 3 seconds if it doesn't hit anything

    public string targetTag = "Enemy";

    private Vector2 direction;
    private Rigidbody2D rb2;

    void Start()
    {
        rb2 = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        rb2.linearVelocity = direction.normalized * speed;

        dieTime -= Time.deltaTime;
        if (dieTime <= 0)
        {
            Destroy(gameObject);
        }
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