using UnityEngine;
using UnityEngine.SceneManagement;

public class InstructionsToLevelOne : MonoBehaviour
{
    [SerializeField] private string sceneName = "Level_01";


    // THIS MUST BE PUBLIC so the button can see it
    public void LoadLevel()
    {
        // Start the game timer
        if (GameTimer.Instance != null)
        {
            GameTimer.Instance.StartTimer();
        }

        // Load Level 1
        SceneManager.LoadScene(sceneName);
    }
}
