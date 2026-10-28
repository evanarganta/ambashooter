using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static bool isGameOver { get; private set; }
    public static bool isLevelComplete { get; private set; }

    void Awake()
    {
        isGameOver = false;
        isLevelComplete = false;
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Space))
        {
            return;
        }

        if (isGameOver)
        {
            RetryGame();
        }
        else if (isLevelComplete)
        {
            NextLevel();
        }
    }

    public static void GameOver()
    {
        if (!isLevelComplete)
        {
            isGameOver = true;
        }
    }

    public static void CompleteLevel()
    {
        if (!isGameOver)
        {
            isLevelComplete = true;
        }
    }

    public static void RetryGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public static void NextLevel()
    {
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextIndex);
        }
        else
        {
            SceneManager.LoadScene("EndLevel");
        }
    }
}
