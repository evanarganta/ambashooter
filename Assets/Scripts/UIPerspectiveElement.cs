using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(Graphic))]
[DisallowMultipleComponent]
public class UIPerspectiveElement : BaseMeshEffect
{
    [HideInInspector]
    public UIPerspective rootPerspective;

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh == null || vh.currentVertCount == 0) return;

        if (rootPerspective == null)
        {
            rootPerspective = GetComponentInParent<UIPerspective>();
        }
        if (rootPerspective == null || !rootPerspective.enabled) return;

        RectTransform rootRect = rootPerspective.GetComponent<RectTransform>();
        if (rootRect == null) return;

        Rect rect = rootRect.rect;
        if (rect.width <= 0.001f || rect.height <= 0.001f) return;

        Transform elemTransform = transform;
        Transform rootTransform = rootPerspective.transform;

        UIVertex vert = new UIVertex();
        int count = vh.currentVertCount;

        for (int i = 0; i < count; i++)
        {
            vh.PopulateUIVertex(ref vert, i);

            // 1. Transform vertex to root Healthbar coordinate space
            Vector3 worldPos = elemTransform.TransformPoint(vert.position);
            Vector3 rootPos = rootTransform.InverseTransformPoint(worldPos);

            // 2. Compute horizontal progress t (0.0 = left edge, 1.0 = right edge)
            float t = Mathf.Clamp01((rootPos.x - rect.xMin) / rect.width);
            float tCurve = Mathf.Pow(t, rootPerspective.perspectivePower);

            // 3. Perspective height foreshortening (tapers towards vanishing point on the right)
            float scaleY = Mathf.Lerp(1.0f, rootPerspective.rightScale, tCurve);
            float centerY = rect.center.y;
            rootPos.y = centerY + (rootPos.y - centerY) * scaleY;

            // 4. Downward vanishing slant
            rootPos.y += tCurve * rootPerspective.ySlant;

            // 5. Forward shear / bevel angle
            float normalizedY = (rootPos.y - rect.yMin) / rect.height;
            rootPos.x += (normalizedY - 0.5f) * rootPerspective.xShear;

            // 6. Transform back into local Graphic vertex space
            worldPos = rootTransform.TransformPoint(rootPos);
            vert.position = elemTransform.InverseTransformPoint(worldPos);

            vh.SetUIVertex(vert, i);
        }
    }
}

