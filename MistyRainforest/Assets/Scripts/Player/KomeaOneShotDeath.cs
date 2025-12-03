using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KomeaOneShotDeath : MonoBehaviour
{
    [Header("Death Settings")]
    public Animator animator;
    public float deathDelay = 1.0f;          // time to show death anim
    public string respawnSceneName = "Level_01"; // <- set this in Inspector if needed

    private bool isDying = false;            // prevent double-kill

    private Level1AudioManager audioManager2;

    void Start()
    {
        audioManager2 = GameObject.FindGameObjectWithTag("L1Music").GetComponent<Level1AudioManager>();

    }


    public void KillPlayer()
    {

        if (isDying) return;
        isDying = true;

        // 1) Trigger death animation and sound effect
        if (animator != null)
        {
            animator.SetBool("isDead", true);   // makes Animator go to Dead state
        }

        // 2) Play death sound effect - DEBUG.LOG to see what's wrong
        // once fixed, should be just "audioManager2.PlaySFX(audioManager2.damageSound);"
        if (audioManager2 != null)
        {
            Debug.Log("[KomeaOneShotDeath] Attempting to play damage sound...");
            if (audioManager2.damageSound != null)
            {
                Debug.Log("[KomeaOneShotDeath] damageSound clip is assigned, playing now!");
                audioManager2.PlaySFX(audioManager2.damageSound);
            }
            else
            {
                Debug.LogError("[KomeaOneShotDeath] damageSound AudioClip is NULL! Assign it in the Inspector.");
            }
        }
        else
        {
            Debug.LogError("[KomeaOneShotDeath] audioManager is NULL! Can't play sound.");
        }


        // 3) Disable movement script so player can't move while dead
        var controller = GetComponent<KomeaMovement2>();   // or whatever your script is called
        if (controller != null) controller.enabled = false;

        // 4) Start delayed respawn
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        // Let the death animation play
        yield return new WaitForSeconds(deathDelay);

        // Safety check: scene name set?
        if (string.IsNullOrWhiteSpace(respawnSceneName))
        {
            Debug.LogError("[KomeaOneShotDeath] respawnSceneName is empty! Cannot load Level 1.");
            yield break;
        }

        // Optional: in case you ever pause time on death
        Time.timeScale = 1f;

        // Always go back to Level_01 (or whatever name you assign)
        SceneManager.LoadScene(respawnSceneName, LoadSceneMode.Single);
    }
}
