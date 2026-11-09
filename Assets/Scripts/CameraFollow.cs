using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour
{
    public bool isFollowing = true;
    public Transform target;
    public Vector3 targetOffset = new Vector3(0, 1f, -10f);

    [Range(0f, 1.0f)] public float followRatio = 0.1f; // Smaller = smoother, 1.0 = instant

    void LateUpdate()
    {
        if (target != null && isFollowing)
        {
            Vector3 targetPos = target.position + targetOffset;

            // Smoothly interpolate camera position towards target position
            Vector3 newPos = Vector3.Lerp(transform.position, targetPos, followRatio);
            newPos.z = -10f; // Keep camera at standard 2D depth

            transform.position = newPos;
        }
    }
}