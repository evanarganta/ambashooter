using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletRainSpawner : MonoBehaviour
{
    [Header("Bullet Configuration")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletDamage = 2f;
    [SerializeField] private float minSpeed = 3.5f;
    [SerializeField] private float maxSpeed = 6.0f;

    [Header("Rain Settings")]
    [Tooltip("Bullets spawned per second")]
    [SerializeField] private float spawnRate = 8f;
    [SerializeField] private float minX = -9.2f;
    [SerializeField] private float maxX = 9.2f;
    [SerializeField] private float spawnHeightAboveCamera = 6.5f;
    [SerializeField] private float maxCeilingY = 36.5f;

    private float spawnTimer = 0f;
    private float soundTimer = 0f;
    private int waveCounter = 0;
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;

        if (bulletPrefab == null)
        {
            bulletPrefab = Resources.Load<GameObject>("Prefabs/EnemyBullet");
        }
    }

    void Update()
    {
        if (GameManager.isGameOver || GameManager.isLevelComplete)
        {
            return;
        }

        if (PlayerController.Instance != null && !PlayerController.Instance.canBeMoved)
        {
            return;
        }

        if (soundTimer > 0f)
        {
            soundTimer -= Time.deltaTime;
        }

        spawnTimer += Time.deltaTime;
        float interval = 1f / Mathf.Max(spawnRate, 1f);

        while (spawnTimer >= interval)
        {
            spawnTimer -= interval;
            SpawnRainBullet();
        }
    }

    private void SpawnRainBullet()
    {
        if (bulletPrefab == null) return;

        float targetY = (mainCam != null ? mainCam.transform.position.y : 15f) + spawnHeightAboveCamera;
        targetY = Mathf.Min(targetY, maxCeilingY);

        waveCounter++;
        int pattern = (waveCounter / 16) % 3;

        float posX;
        Vector2 direction;

        switch (pattern)
        {
            case 0:
                posX = Random.Range(minX, maxX);
                float angleVariance = Random.Range(-25f, 25f);
                float rad = (-90f + angleVariance) * Mathf.Deg2Rad;
                direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
                break;

            case 1:
                posX = Random.Range(minX, maxX);
                bool slantRight = (waveCounter % 2 == 0);
                float slantAngle = slantRight ? -65f : -115f;
                float slantRad = slantAngle * Mathf.Deg2Rad;
                direction = new Vector2(Mathf.Cos(slantRad), Mathf.Sin(slantRad)).normalized;
                break;

            default:
                float t = (waveCounter % 20) / 20f;
                posX = Mathf.Lerp(minX, maxX, t);
                direction = Vector2.down;
                break;
        }

        Vector3 spawnPos = new Vector3(posX, targetY, 0f);
        GameObject bulletObj = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        Bullet bullet = bulletObj.GetComponent<Bullet>();

        if (bullet != null)
        {
            float speed = Random.Range(minSpeed, maxSpeed);
            bullet.Launch(direction, "Player", speed, bulletDamage);
            bullet.ignoreObstacles = true;
            bullet.SetDieTime(6f);
        }

        if (soundTimer <= 0f && AudioManager.Instance != null)
        {
            soundTimer = 0.35f;
            AudioManager.Instance.PlayEnemyShoot(spawnPos);
        }
    }
}

