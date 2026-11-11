using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("UI Canvas Prefab (Loads from Assets/Prefabs/UI Canvas.prefab)")]
    [SerializeField] public GameObject uiCanvasPrefab;

    [Header("Player Health Bar")]
    [SerializeField] public GameObject playerHealthBarRoot;
    [SerializeField] public Image playerHealthBar;
    [SerializeField] public Text playerHealthText;

    [Header("Parry Cooldown UI")]
    [SerializeField] public Image[] parryCooldownBars;
    [SerializeField] public Image parryCooldownFill;
    [SerializeField] public Color parryReadyColor = new Color(0f, 0.95f, 1f, 1f);
    [SerializeField] public Color parryRechargeColor = new Color(0.08f, 0.55f, 0.65f, 0.85f);
    [SerializeField] public Color parryEmptyColor = new Color(0.04f, 0.12f, 0.16f, 0.55f);

    [Header("Enemy Progress Counter")]
    [SerializeField] public Text enemyProgressText;

    [Header("Boss Health Bar")]
    [SerializeField] public GameObject bossHealthBarRoot;
    [SerializeField] public Image bossHealthBarFill;
    [SerializeField] public Text bossNameText;
    [SerializeField] public Text bossHealthValueText;

    [Header("Game State Panels")]
    [SerializeField] public GameObject gameOverUI;
    [SerializeField] public GameObject levelCompleteUI;

    [Header("Parry Vignette")]
    [SerializeField] public Image parryFlashImage;

    private PlayerController player;
    private static UIManager instance;

    private float parryFlashTimer = 0f;
    private const float ParryFlashDuration = 0.3f;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        player = FindAnyObjectByType<PlayerController>();

        EnsureCanvasInstance();

        if (gameOverUI != null) gameOverUI.SetActive(false);
        if (levelCompleteUI != null) levelCompleteUI.SetActive(false);
        if (bossHealthBarRoot != null) bossHealthBarRoot.SetActive(false);
    }

    void Update()
    {
        if (player == null)
        {
            player = FindAnyObjectByType<PlayerController>();
        }

        if (player != null && player.healthMax > 0f)
        {
            if (playerHealthBar != null)
            {
                playerHealthBar.fillAmount = Mathf.Clamp01(player.health / player.healthMax);
            }
            if (playerHealthText != null)
            {
                playerHealthText.text = Mathf.CeilToInt(player.health).ToString();
            }
        }

        if (player != null)
        {
            float pProgress = player.ParryCooldownProgress;
            bool isReady = player.IsParryReady;

            if (parryCooldownBars != null && parryCooldownBars.Length > 0)
            {
                int count = parryCooldownBars.Length;
                for (int i = 0; i < count; i++)
                {
                    if (parryCooldownBars[i] == null) continue;

                    float segStart = (float)i / count;
                    float segEnd = (float)(i + 1) / count;
                    float segFill = Mathf.Clamp01((pProgress - segStart) / (segEnd - segStart));

                    if (parryCooldownBars[i].type == Image.Type.Filled)
                    {
                        parryCooldownBars[i].fillAmount = segFill;
                        parryCooldownBars[i].color = isReady ? parryReadyColor : parryRechargeColor;
                    }
                    else
                    {
                        if (segFill >= 0.99f)
                        {
                            parryCooldownBars[i].color = isReady ? parryReadyColor : parryRechargeColor;
                        }
                        else
                        {
                            parryCooldownBars[i].color = Color.Lerp(parryEmptyColor, parryRechargeColor, segFill);
                        }
                    }
                }
            }

            if (parryCooldownFill != null)
            {
                parryCooldownFill.fillAmount = pProgress;
                parryCooldownFill.color = isReady ? parryReadyColor : parryRechargeColor;
            }
        }

        if (enemyProgressText != null)
        {
            enemyProgressText.text = $"{ScoreManager.currentEnemyProgress} of {ScoreManager.targetEnemyProgress}";
        }

        if (gameOverUI != null)
        {
            gameOverUI.SetActive(GameManager.isGameOver);
        }

        if (levelCompleteUI != null)
        {
            levelCompleteUI.SetActive(!GameManager.isGameOver && GameManager.isLevelComplete);
        }

        BossMushroom boss = BossMushroom.Instance;
        if (boss != null && !boss.isDead && bossHealthBarRoot != null)
        {
            if (!bossHealthBarRoot.activeSelf) bossHealthBarRoot.SetActive(true);
            if (bossHealthBarFill != null && boss.maxHealth > 0f)
            {
                bossHealthBarFill.fillAmount = Mathf.Clamp01(boss.health / boss.maxHealth);
            }
            if (bossHealthValueText != null && bossHealthValueText.gameObject.activeSelf)
            {
                bossHealthValueText.gameObject.SetActive(false);
            }
            if (bossNameText != null)
            {
                bossNameText.text = boss.bossName;
            }
        }
        else if (bossHealthBarRoot != null && bossHealthBarRoot.activeSelf)
        {
            bossHealthBarRoot.SetActive(false);
        }

        if (parryFlashTimer > 0f && parryFlashImage != null)
        {
            parryFlashTimer -= Time.deltaTime;
            float alpha = Mathf.Clamp01(parryFlashTimer / ParryFlashDuration);
            Color c = parryFlashImage.color;
            c.a = alpha;
            parryFlashImage.color = c;
        }
    }

    public static void TriggerParryFlash()
    {
        if (instance == null || instance.parryFlashImage == null) return;
        instance.parryFlashTimer = ParryFlashDuration;
        Color c = instance.parryFlashImage.color;
        c.a = 1f;
        instance.parryFlashImage.color = c;
    }

    public void EnsureCanvasInstance()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            if (uiCanvasPrefab == null)
            {
                uiCanvasPrefab = Resources.Load<GameObject>("UI Canvas");
            }

#if UNITY_EDITOR
            if (uiCanvasPrefab == null)
            {
                uiCanvasPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI Canvas.prefab");
                if (uiCanvasPrefab == null)
                {
                    uiCanvasPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI Canvas.prefab");
                }
            }
#endif

            if (uiCanvasPrefab != null)
            {
                uiCanvasPrefab.transform.localScale = Vector3.one;
                GameObject canvasObj = Instantiate(uiCanvasPrefab);
                canvasObj.name = "UI Canvas";
                canvasObj.transform.localScale = Vector3.one;
                canvas = canvasObj.GetComponent<Canvas>();
            }
            else
            {
                Debug.LogWarning("UIManager: UI Canvas prefab could not be loaded from Resources!");
            }
        }

        if (canvas != null)
        {
            BindExistingUI(canvas);
        }
    }

    public void BindExistingUI(Canvas canvas = null)
    {
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }
        if (canvas == null) return;

        Transform t = canvas.transform;

        if (playerHealthBar == null)
        {
            Transform fillT = FindDeepChild(t, "Healthbar_Fill");
            if (fillT != null) playerHealthBar = fillT.GetComponent<Image>();
        }

        if (playerHealthText == null)
        {
            Transform txtT = FindDeepChild(t, "HealthNumberText");
            if (txtT == null) txtT = FindDeepChild(t, "HealthText");
            if (txtT != null) playerHealthText = txtT.GetComponent<Text>();
        }

        if (playerHealthBarRoot == null)
        {
            Transform rootT = FindDeepChild(t, "Healthbar");
            if (rootT != null) playerHealthBarRoot = rootT.gameObject;
        }

        if (parryCooldownBars == null || parryCooldownBars.Length == 0)
        {
            Transform p1 = FindDeepChild(t, "ParryBar_1");
            if (p1 == null) p1 = FindDeepChild(t, "Stamina_1");

            Transform p2 = FindDeepChild(t, "ParryBar_2");
            if (p2 == null) p2 = FindDeepChild(t, "Stamina_2");

            Transform p3 = FindDeepChild(t, "ParryBar_3");
            if (p3 == null) p3 = FindDeepChild(t, "Stamina_3");

            if (p1 != null || p2 != null || p3 != null)
            {
                List<Image> list = new List<Image>();
                if (p1 != null) list.Add(p1.GetComponent<Image>());
                if (p2 != null) list.Add(p2.GetComponent<Image>());
                if (p3 != null) list.Add(p3.GetComponent<Image>());
                parryCooldownBars = list.ToArray();
            }
        }

        if (parryCooldownFill == null)
        {
            Transform pFill = FindDeepChild(t, "ParryCooldown_Fill");
            if (pFill != null) parryCooldownFill = pFill.GetComponent<Image>();
        }

        if (enemyProgressText == null)
        {
            Transform textT = FindDeepChild(t, "EnemyProgressText");
            if (textT != null) enemyProgressText = textT.GetComponent<Text>();
        }

        if (bossHealthBarRoot == null)
        {
            Transform bossT = FindDeepChild(t, "BossHealthBar");
            if (bossT != null) bossHealthBarRoot = bossT.gameObject;
        }

        if (bossHealthBarFill == null)
        {
            Transform bFillT = FindDeepChild(t, "BossHealth_Fill");
            if (bFillT != null) bossHealthBarFill = bFillT.GetComponent<Image>();
        }

        if (bossNameText == null)
        {
            Transform bNameT = FindDeepChild(t, "BossName");
            if (bNameT != null) bossNameText = bNameT.GetComponent<Text>();
        }

        if (bossHealthValueText == null)
        {
            Transform bValT = FindDeepChild(t, "BossHealthValue");
            if (bValT != null) bossHealthValueText = bValT.GetComponent<Text>();
        }

        if (gameOverUI == null)
        {
            Transform goT = FindDeepChild(t, "GameOverPanel");
            if (goT != null) gameOverUI = goT.gameObject;
        }

        if (levelCompleteUI == null)
        {
            Transform lcT = FindDeepChild(t, "LevelCompletePanel");
            if (lcT != null) levelCompleteUI = lcT.gameObject;
        }

        if (parryFlashImage == null)
        {
            Transform pfT = FindDeepChild(t, "ParryFlash");
            if (pfT != null)
            {
                parryFlashImage = pfT.GetComponent<Image>();
                Shader vignetteShader = Resources.Load<Shader>("UIInvert");
                if (vignetteShader != null && parryFlashImage != null)
                {
                    parryFlashImage.material = new Material(vignetteShader);
                }
            }
        }
    }

    private Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent.name == childName) return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindDeepChild(parent.GetChild(i), childName);
            if (result != null) return result;
        }
        return null;
    }
}
