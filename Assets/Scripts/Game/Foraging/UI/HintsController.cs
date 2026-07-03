using UnityEngine;

public class HintsController : MonoBehaviour, IPausable
{
    #region fields

    [Header("Death Settings")]
    [SerializeField] private GameObject deathScreen;

    [Header("Factory Entering")]
    [SerializeField] private GameObject hintFactoryEntering;

    [Header("Player Health")]
    [SerializeField] private TMPro.TextMeshProUGUI hpIndicatorUI;

    [Header("Pause Menu")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private PauseController pauseController;

    #endregion


    #region initialization

    void Awake()
    {
        deathScreen.SetActive(false);
        hintFactoryEntering.SetActive(true);
        pausePanel.SetActive(false);
    }

    void Start()
    {
        pauseController.Subscribe(this);
    }

    #endregion


    #region deathScreen

    public void OnPlayerDeath()
    {
        deathScreen.SetActive(true);
    }

    public void OnPlayerRespawn()
    {
        deathScreen.SetActive(false);
    }

    #endregion


    #region enteringFactory

    public void OnPlayerInDanger()
    {
        hintFactoryEntering.SetActive(false);
    }

    public void OnPlayerLeaveDanger()
    {
         if (hintFactoryEntering != null) hintFactoryEntering.SetActive(true);
    }

    #endregion


    #region  playerHP

    public void SetPlayerHP(float hp) => hpIndicatorUI.text = hp.ToString();

    #endregion


    #region pause

    public void SetPause(bool pause)
    {
        pausePanel.SetActive(pause);
    }

    #endregion
}
