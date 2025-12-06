using UnityEngine;

public class Level1AudioManager : MonoBehaviour
    {
        [SerializeField] AudioSource musicSource1;
        [SerializeField] AudioSource sfxSource1;
        [SerializeField] AudioSource gateSfxSource1;
        [SerializeField] AudioSource damageSfxSource1;
        public AudioClip backgroundMusic1;
        public AudioClip gameOverSound;
        public AudioClip gateSound;
        public AudioClip damageSound;

private void Start()
    {
        musicSource1.clip = backgroundMusic1;
        musicSource1.Play();
    }
public void PlaySFX(AudioClip clip)

    {
        sfxSource1.PlayOneShot(clip);
    }

// Unique version for damage
public void PlayOnce(AudioClip clip)

    {
        damageSfxSource1.PlayOneShot(clip);
    }

// Method for playing sound effect only and pausing BG music
   public void PlaySoundSolo(AudioClip clip)
    {
        if (clip == null) return;  // Early exit if no clip provided

        // Pause the background music
        if (musicSource1 != null)
        {
            musicSource1.Pause();
        }

        // Play the sound on its dedicated source
        gateSfxSource1.PlayOneShot(clip);

        // Resume music after the sound finishes
        StartCoroutine(ResumeMusicAfterDelay(clip.length));
    }


    private System.Collections.IEnumerator ResumeMusicAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (musicSource1 != null)
        {
            musicSource1.UnPause();
        }
    }

    public void PauseMusic()
    {
        if (musicSource1 != null)
        {
            musicSource1.Pause();
        }
    }

    public void ResumeMusic()
    {
        if (musicSource1 != null)
        {
            musicSource1.UnPause();
        }
    }
}

