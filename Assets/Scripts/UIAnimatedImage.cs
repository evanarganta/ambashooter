using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UIAnimatedImage : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Target playback speed in frames per second (10 fps = 100ms per frame).")]
    public float fps = 10f;

    [Tooltip("Whether the animation loops continuously.")]
    public bool loop = true;

    [Tooltip("Folder in Resources to automatically load frames from if 'frames' is empty.")]
    public string resourceFolder = "Omedeto";

    [Header("Frames")]
    public Sprite[] frames;

    private Image targetImage;
    private float timer = 0f;
    private int currentFrameIndex = -1;

    void Awake()
    {
        targetImage = GetComponent<Image>();

        if (frames == null || frames.Length == 0)
        {
            LoadFramesFromResources();
        }

        if (frames != null && frames.Length > 0 && targetImage != null)
        {
            targetImage.sprite = frames[0];
            currentFrameIndex = 0;
        }
    }

    void Start()
    {
        if (targetImage == null) targetImage = GetComponent<Image>();
        if (frames == null || frames.Length == 0)
        {
            LoadFramesFromResources();
            if (frames != null && frames.Length > 0 && targetImage != null)
            {
                targetImage.sprite = frames[0];
            }
        }
    }

    void OnEnable()
    {
        timer = 0f;
        currentFrameIndex = -1;
    }

    private void LoadFramesFromResources()
    {
        if (string.IsNullOrEmpty(resourceFolder)) return;

        Sprite[] loaded = Resources.LoadAll<Sprite>(resourceFolder);
        if (loaded != null && loaded.Length > 0)
        {
            List<Sprite> list = new List<Sprite>(loaded);
            list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            frames = list.ToArray();
        }
    }

    void Update()
    {
        if (frames == null || frames.Length == 0 || targetImage == null) return;

        timer += Time.unscaledDeltaTime;
        int index = Mathf.FloorToInt(timer * Mathf.Max(0.01f, fps));

        if (loop)
        {
            index %= frames.Length;
        }
        else
        {
            index = Mathf.Min(index, frames.Length - 1);
        }

        if (index != currentFrameIndex)
        {
            currentFrameIndex = index;
            targetImage.sprite = frames[currentFrameIndex];
        }
    }
}
