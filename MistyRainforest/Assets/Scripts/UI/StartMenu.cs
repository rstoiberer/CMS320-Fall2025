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

    private void Start()
    {
        // Auto-select the button for immediate Enter/Submit support
        if (defaultSelectable != null)
        {
            EventSystem.current.SetSelectedGameObject(defaultSelectable.gameObject);
        }
    }

    // Hook this to Button.onClick
    public void StartGame()
    {
        // Simple load (instant)
        SceneManager.LoadScene(sceneToLoad);
    }

    // Optional: allow pressing Escape to quit on desktop
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }
}
