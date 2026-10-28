using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Sprite healthFillSprite;
    [SerializeField] private Sprite healthBackgroundSprite;
    [SerializeField] private Sprite healthBorderSprite;

    private PlayerController player;
    private Text enemyProgressText;
    private Image playerHealthBar;
    private GameObject gameOverUI;
    private GameObject levelCompleteUI;

    void Start()
    {
        player = FindAnyObjectByType<PlayerController>();
        CreateCanvas();
    }

    void Update()
    {
        if (player != null && playerHealthBar != null && player.healthMax > 0f)
        {
            playerHealthBar.fillAmount = Mathf.Clamp01(player.health / player.healthMax);
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
    }

    private void CreateCanvas()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 600f);
        scaler.matchWidthOrHeight = 0.5f;

        CreateHealthBar(canvas.transform);
        enemyProgressText = CreateText(canvas.transform, "EnemyProgressText", "0 of 0", 24, TextAnchor.UpperRight);
        SetTopRight(enemyProgressText.rectTransform, new Vector2(-16f, -16f), new Vector2(180f, 36f));

        gameOverUI = CreatePanel(canvas.transform, "GameOverPanel", "GAME OVER\nPress SPACE to Retry", new Color(0.18f, 0.02f, 0.02f, 0.86f));
        levelCompleteUI = CreatePanel(canvas.transform, "LevelCompletePanel", "LEVEL COMPLETE!\nPress SPACE to Continue", new Color(0.02f, 0.16f, 0.04f, 0.86f));
        gameOverUI.SetActive(false);
        levelCompleteUI.SetActive(false);
    }

    private void CreateHealthBar(Transform canvas)
    {
        GameObject healthBar = new GameObject("Healthbar", typeof(RectTransform));
        RectTransform root = healthBar.GetComponent<RectTransform>();
        root.SetParent(canvas, false);
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = new Vector2(16f, -16f);
        root.sizeDelta = new Vector2(100f, 30f);

        CreateImage(root, "Healthbar_Bg", healthBackgroundSprite, new Vector2(78f, 12f));

        playerHealthBar = CreateImage(root, "Healthbar_Fill", healthFillSprite, new Vector2(74f, 8f));
        playerHealthBar.type = Image.Type.Filled;
        playerHealthBar.fillMethod = Image.FillMethod.Horizontal;
        playerHealthBar.fillOrigin = (int)Image.OriginHorizontal.Left;

        CreateImage(root, "Healthbar_Border", healthBorderSprite, new Vector2(87f, 21f));
    }

    private GameObject CreatePanel(Transform canvas, string panelName, string message, Color color)
    {
        GameObject panel = new GameObject(panelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.SetParent(canvas, false);
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = color;

        Text messageText = CreateText(panelRect, panelName + "Text", message, 36, TextAnchor.MiddleCenter);
        RectTransform textRect = messageText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        messageText.color = Color.white;

        return panel;
    }

    private Image CreateImage(Transform parent, string imageName, Sprite sprite, Vector2 size)
    {
        GameObject imageObject = new GameObject(imageName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = false;
        return image;
    }

    private Text CreateText(Transform parent, string objectName, string content, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private void SetTopRight(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
