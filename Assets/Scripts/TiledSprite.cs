using UnityEngine;

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

