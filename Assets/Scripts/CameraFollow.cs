using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour
{
    public bool isFollowing = true;
    public Transform target;
    public Vector3 targetOffset = new Vector3(0, 1f, -10f);

    [Tooltip("Target orthographic size for 2D camera zoom (smaller value = closer/zoomed in). Default was 5.")]
    [SerializeField] private float orthographicSize = 4f;

    [Range(0f, 1.0f)] public float followRatio = 1f; // Smaller = smoother, 1.0 = instant

    [Header("Intro Pan")]
    public bool enableIntroPan = false;
    public Transform introStartTarget;
    public float introHoldTime = 1.0f;
    public float introPanDuration = 2.5f;
    public float introBossZoom = 5.5f;

    private bool isIntroPanning = false;

    void Awake()
    {
        Camera cam = GetComponent<Camera>();
        if (cam != null && cam.orthographic)
        {
            cam.orthographicSize = orthographicSize;
        }

        // Ensure AudioListener is attached to the camera so spatial sound pans left/right accurately
        if (GetComponent<AudioListener>() == null)
        {
            gameObject.AddComponent<AudioListener>();
        }
    }

    void Start()
    {
        if (target == null && PlayerController.Instance != null)
        {
            target = PlayerController.Instance.transform;
        }

        // Auto-detect boss in boss levels
        if (introStartTarget == null)
        {
            BossMushroom boss = FindAnyObjectByType<BossMushroom>();
            if (boss != null)
            {
                introStartTarget = boss.transform;
                enableIntroPan = true;
            }
        }

        if (enableIntroPan && introStartTarget != null)
        {
            StartCoroutine(IntroPanRoutine());
        }
    }

    private IEnumerator IntroPanRoutine()
    {
        isIntroPanning = true;
        isFollowing = false;

        Camera cam = GetComponent<Camera>();
        if (cam != null && cam.orthographic)
        {
            cam.orthographicSize = introBossZoom;
        }

        // Position camera right on the boss above
        Vector3 bossCamPos = introStartTarget.position + targetOffset;
        bossCamPos.z = -10f;
        transform.position = bossCamPos;

        // Freeze player movement during intro pan
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.canBeMoved = false;
        }

        // Hold view on the boss
        yield return new WaitForSeconds(introHoldTime);

        // Smoothly pan down to player while zooming back to standard player zoom (orthographicSize = 4)
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        float startZoom = cam != null ? cam.orthographicSize : introBossZoom;
        float targetZoom = orthographicSize > 0f ? orthographicSize : 4f;

        while (elapsed < introPanDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / introPanDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (target == null && PlayerController.Instance != null)
            {
                target = PlayerController.Instance.transform;
            }

            Vector3 destination = target != null ? (target.position + targetOffset) : transform.position;
            destination.z = -10f;

            transform.position = Vector3.Lerp(startPos, destination, smoothT);

            if (cam != null && cam.orthographic)
            {
                cam.orthographicSize = Mathf.Lerp(startZoom, targetZoom, smoothT);
            }

            yield return null;
        }

        // Snap precisely to target at end of pan
        if (target != null)
        {
            Vector3 finalPos = target.position + targetOffset;
            finalPos.z = -10f;
            transform.position = finalPos;
        }

        if (cam != null && cam.orthographic)
        {
            cam.orthographicSize = targetZoom;
        }

        // Restore instant follow speed like the other levels
        followRatio = 1f;

        // Restore player control & normal follow
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.canBeMoved = true;
        }

        isIntroPanning = false;
        isFollowing = true;
    }

    void LateUpdate()
    {
        if (isIntroPanning) return;

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