using UnityEngine;

public class TelegraphFlashing : MonoBehaviour
{
    public SpriteRenderer sr;
    public float speed = 8f;

    void Update()
    {
        if (sr != null)
        {
            float a = Mathf.PingPong(Time.time * speed, 0.5f) + 0.25f;
            Color c = sr.color;
            c.a = a;
            sr.color = c;
        }
    }
}

