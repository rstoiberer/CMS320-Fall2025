using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StartMenu : MonoBehaviour
{
    [Header("Target Scene")]
    [SerializeField] private string sceneToLoad = "Scene1";

    [Header("Optional: Select a default button for keyboard/controller")]
    [SerializeField] private Selectable defaultSelectable;

    // For AudioManager
    AudioManager audioManager;

    private void Start()
    {

        if (defaultSelectable != null)
        {
            EventSystem.current.SetSelectedGameObject(defaultSelectable.gameObject);
            audioManager = GameObject.FindGameObjectWithTag("Music").GetComponent<AudioManager>();
        }
    }

    public void StartGame()
    {
        // Simple load (instant)
        SceneManager.LoadScene(sceneToLoad);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }
}
