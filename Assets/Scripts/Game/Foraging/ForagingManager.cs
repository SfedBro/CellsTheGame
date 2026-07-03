using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ForagingManager : MonoBehaviour
{
    #region fields

    [Header("Controller")]
    private InputSystem_Actions inputActions;
    
    [Header("References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private PauseController pauseController;

    [Header("Managers")]
    [SerializeField] private UpgradingManager upgradingManager;
    [SerializeField] private PlayerExperienceManager playerExperienceManager;

    [Header("Tutorial")]
    [SerializeField] private TutorialController tutorialController;
    [SerializeField] private HintsController hintsController;

    #endregion


    #region gettersNsetters

    public InputSystem_Actions GetInputSystem() => inputActions;

    #endregion

    #region initialization

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        tutorialController.Hide();
    }

    void Start()
    {
        upgradingManager.correctPlayerStats(player);
    }

    void OnEnable()
    {
        inputActions.Player.Jump.performed += respawnPlayer;
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Player.Jump.performed -= respawnPlayer;
        inputActions.Disable();
    }

    #endregion


    #region playerDeath

    private bool isPlayerDead = false;

    private void respawnPlayer(InputAction.CallbackContext context)
    {
        if (!isPlayerDead) return;

        isPlayerDead = false;
        player.Respawn();
    }

    public void onPlayerDeath(EnemyBase killer)
    {
        isPlayerDead = true;

        // Notify managers
        upgradingManager.onPlayerDeath();
        upgradingManager.correctPlayerStats(player);
        playerExperienceManager.onPlayerDeath(killer);
        ResourcesManager.instance.onPlayerDeath(killer);
        killer.onPlayerKilled();
    }

    #endregion


    #region pause menu

    public void ExitScene()
    {
        pauseController.Resume();
        if (AutoSaveManager.Instance != null)
        {
            AutoSaveManager.Instance.SaveAll();
        }
        SceneManager.LoadScene("MainMenu");
    }

    public void StartTutorial()
    {
        pauseController.enabled = false;
        hintsController.SetPause(false);
        tutorialController.Show();
    }

    public void EndTutorial()
    {
        tutorialController.Hide();
        hintsController.SetPause(true);
        pauseController.enabled = true;
    }

    #endregion
}
