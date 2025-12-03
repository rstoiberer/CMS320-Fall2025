using UnityEngine;

public class Level1AudioManager : MonoBehaviour
    {
        [SerializeField] AudioSource musicSource1;
        [SerializeField] AudioSource sfxSource1;
        [SerializeField] AudioSource gateSfxSource1;
        public AudioClip backgroundMusic1;
        public AudioClip damageSound;
        public AudioClip gateSound;

private void Start()
    {
        musicSource1.clip = backgroundMusic1;
        musicSource1.Play();
    }
public void PlaySFX(AudioClip clip)

    {
        sfxSource1.PlayOneShot(clip);
    }


}