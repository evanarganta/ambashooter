using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMushroom : EnemyController
{
    [Header("Mushroom")]
    [SerializeField] private SpriteRenderer graphic;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 6f;
    [SerializeField] private float attackMinDistance = 8f;
    [SerializeField] private float attackTimeMax = 1.5f;
    [SerializeField] private Vector3 shootOffset = new Vector3(0.5f, 0f, 0f);

    private float attackTime = 0f;

    void Start()
    {
        if (attackTarget == null && PlayerController.Instance != null)
        {
            attackTarget = PlayerController.Instance.transform;
        }
    }

    protected override void Movement()
    {
        if (attackTarget != null)
        {
            float distance = Vector2.Distance(transform.position, attackTarget.position);

            if (distance < attackMinDistance)
            {
                if (attackTarget.position.x < transform.position.x)
                {
                    graphic.flipX = true;
                }
                else
                {
                    graphic.flipX = false;
                }

                attackTime -= Time.deltaTime;
                if (attackTime <= 0)
                {
                    Shoot();
                    attackTime = attackTimeMax;
                }
            }
        }
    }

    void Shoot()
    {
        int direction = graphic.flipX ? -1 : 1;

        Vector3 spawnOffset = shootOffset;
        if (direction < 0)
        {
            spawnOffset.x *= -1;
        }

        Vector3 shootPos = transform.position + spawnOffset;

        GameObject bulletObj = Instantiate(bulletPrefab, shootPos, Quaternion.identity);
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.Launch(new Vector2(direction, 0), "Player", bulletSpeed, attack);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemyShoot(transform.position);
        }
    }
}