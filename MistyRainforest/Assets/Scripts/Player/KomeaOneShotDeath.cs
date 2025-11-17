using System.Collections;                     // <-- add this
using UnityEngine;
using UnityEngine.SceneManagement;

public class KomeaOneShotDeath : MonoBehaviour
{
    public Animator animator;
    public float deathDelay = 1.0f;          // time to show death anim
    private bool isDying = false;            // prevent double-kill

    public void KillPlayer()
    {
        if (isDying) return;
        isDying = true;

        // 1) Trigger death animation
        if (animator != null)
        {
            animator.SetBool("isDead", true);   // makes Animator go to Dead state
        }

        // 2) Optional: disable movement script so player can't move while dead
        var controller = GetComponent<KomeaMovement2>();   // or whatever your script is called
        if (controller != null) controller.enabled = false;

        // 3) Start delayed reload
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        yield return new WaitForSeconds(deathDelay);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
