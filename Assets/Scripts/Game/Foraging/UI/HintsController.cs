using UnityEngine;

public class HintsController : MonoBehaviour
{
    #region fields

    [Header("Death Settings")]
    [SerializeField] private GameObject deathScreen;

    [Header("Factory Entering")]
    [SerializeField] private GameObject hintFactoryEntering;

    [Header("Player Health")]
    [SerializeField] private TMPro.TextMeshProUGUI hpIndicatorUI;

    #endregion


    #region initialization

    void Awake()
    {
        deathScreen.SetActive(false);
        hintFactoryEntering.SetActive(true);
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
}
