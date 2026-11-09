using UnityEngine;

/// <summary>
/// Drop this on any SpriteRenderer ground/platform object.
/// It switches the renderer to Tiled mode so the texture repeats
/// instead of stretching when you resize via the Size handles.
///
/// HOW TO RESIZE: use the "Size" fields in SpriteRenderer (shown when
/// Draw Mode = Tiled), NOT Transform Scale. Transform Scale will scale
/// the whole tiled pattern (i.e. zoom the tiles), which is usually
/// not what you want.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class TiledSprite : MonoBehaviour
{
    private SpriteRenderer sr;

    void OnEnable()
    {
        Apply();
    }

#if UNITY_EDITOR
    void Update()
    {
        // Keep settings correct while tweaking in the Editor
        Apply();
    }
#endif

    void Apply()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        if (sr.drawMode != SpriteDrawMode.Tiled)
        {
            sr.drawMode = SpriteDrawMode.Tiled;
        }

        if (sr.tileMode != SpriteTileMode.Continuous)
        {
            sr.tileMode = SpriteTileMode.Continuous;
        }
    }
}

