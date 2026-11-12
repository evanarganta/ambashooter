using UnityEngine;

public class EndLevelMusic : MonoBehaviour
{
    [SerializeField] private AudioClip music;
    [SerializeField] private bool loop = true;

    void Start()
    {
        if (music == null)
        {
            music = Resources.Load<AudioClip>("Audio/789-audio-extractor");
        }

        if (music != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(music, loop);
    }
}

