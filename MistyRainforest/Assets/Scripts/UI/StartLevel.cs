using UnityEngine;
using UnityEngine.SceneManagement;

public class InstructionsToLevelOne : MonoBehaviour
{
    [SerializeField] private string sceneName = "Level_01";

    public void LoadLevel()
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[InstructionsToLevelOne] Scene name is empty.");
            return;
        }

        Debug.Log($"[InstructionsToLevelOne] Loading '{sceneName}'…");
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
