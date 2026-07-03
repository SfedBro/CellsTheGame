using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    [SerializeField] private GameObject pauseMenuUI;
    private bool isPaused = false;
    public bool IsPaused => isPaused;

    private InputSystem_Actions inputActions;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        inputActions = new InputSystem_Actions();
        
        // Встроенный экшен Cancel идеально подходит для паузы
        inputActions.UI.Cancel.performed += ctx => Toggle();
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    public void Toggle()
    {
        if (isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Resume()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
        Time.timeScale = 1f;
        isPaused = false;
        if (TutorialManager.Instance != null) TutorialManager.Instance.NotifyAction("Action_Resume");
    }

    public void Pause()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
        }
        
        // Закрываем окна механизмов и хранилищ
        if (StorageWindow.Instance != null) StorageWindow.Instance.Close();
        if (CraftingWindow.Instance != null) CraftingWindow.Instance.Close();
        if (BuildMenuWindow.Instance != null) BuildMenuWindow.Instance.Close();
        
        Time.timeScale = 0f;
        isPaused = true;
        if (TutorialManager.Instance != null) TutorialManager.Instance.NotifyAction("Action_Pause");
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
