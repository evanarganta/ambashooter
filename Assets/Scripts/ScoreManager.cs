using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public static int currentEnemyProgress { get; private set; }
    public static int targetEnemyProgress { get; private set; }

    void Awake()
    {
        Instance = this;
        currentEnemyProgress = 0;
        targetEnemyProgress = 0;
    }

    void Start()
    {
        targetEnemyProgress = FindObjectsByType<EnemyController>().Length;
    }

    public static void DefeatEnemy()
    {
        if (currentEnemyProgress >= targetEnemyProgress)
        {
            return;
        }

        currentEnemyProgress++;
        if (currentEnemyProgress >= targetEnemyProgress)
        {
            GameManager.CompleteLevel();
        }
    }
}
