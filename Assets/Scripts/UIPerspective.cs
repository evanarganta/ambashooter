using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
public class UIPerspective : MonoBehaviour
{
    [Header("Perspective Foreshortening")]
    [Tooltip("Height multiplier at the far right edge (0.5 to 0.8 creates strong 3D perspective foreshortening)")]
    [Range(0.3f, 1.0f)]
    public float rightScale = 0.582f;

    [Tooltip("Downward or upward slant in pixels from left to right along the vanishing line")]
    [Range(-40f, 40f)]
    public float ySlant = -0.6f;

    [Tooltip("Horizontal shear / forward bevel angle (slants vertical lines like Ultrakill HUD)")]
    [Range(-30f, 30f)]
    public float xShear = -8.5f;

    [Tooltip("Non-linear perspective curve (1 = linear, >1 = accelerated foreshortening into distance)")]
    [Range(0.6f, 2.0f)]
    public float perspectivePower = 1.039f;

    [Header("Exclusions")]
    [Tooltip("If true, Text graphics under this container will NOT be perspective-warped")]
    public bool excludeText = false;

    void OnEnable()
    {
        if (Application.isPlaying)
        {
            RefreshChildren();
        }
        else
        {
            NotifyDirty();
        }
    }

    void OnValidate()
    {
        NotifyDirty();
    }

    void Start()
    {
        if (Application.isPlaying)
        {
            RefreshChildren();
        }
    }

    public void NotifyDirty()
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        foreach (Graphic g in graphics)
        {
            if (g != null) g.SetVerticesDirty();
        }
    }

    [ContextMenu("Refresh Children")]
    public void RefreshChildren()
    {
        if (!Application.isPlaying) return;

        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        foreach (Graphic g in graphics)
        {
            if (g == null) continue;

            if (excludeText && g is Text)
            {
                UIPerspectiveElement existing = g.GetComponent<UIPerspectiveElement>();
                if (existing != null)
                {
                    existing.enabled = false;
                    Destroy(existing);
                }
                g.SetVerticesDirty();
                continue;
            }

            UIPerspectiveElement elem = g.GetComponent<UIPerspectiveElement>();
            if (elem == null)
            {
                elem = g.gameObject.AddComponent<UIPerspectiveElement>();
            }
            if (elem != null)
            {
                elem.enabled = true;
                elem.rootPerspective = this;
            }
            g.SetVerticesDirty();
        }
    }
}
