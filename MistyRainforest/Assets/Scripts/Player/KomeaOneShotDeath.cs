using UnityEngine;
using UnityEngine.SceneManagement;

public class KomeaOneShotDeath : MonoBehaviour
{
    public void KillPlayer()
    {
        // Play death SFX/anim here if you want, then reload.
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
