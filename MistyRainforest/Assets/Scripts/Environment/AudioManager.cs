using UnityEngine;

public class AudioManager : MonoBehaviour
    {
        [SerializeField] AudioSource musicSource;
        public AudioClip backgroundMusic;

private void Start()
    {
        musicSource.clip = backgroundMusic;
        musicSource.Play();
    }

}

