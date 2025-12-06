using UnityEngine;
using UnityEngine.SceneManagement;

public class InstructionsToLevelOne : MonoBehaviour
{
    [SerializeField] private string sceneName = "Level_01";

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
