using UnityEngine;
using UnityEngine.SceneManagement;

public class KomeaOneShotDeath : MonoBehaviour

{
    public Animator animator;

    public void KillPlayer()
    {
        // Play death SFX/anim here if you want, then reload.
        //animator.Play("Dead");
        //animator.SetBool("isDead", true);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
