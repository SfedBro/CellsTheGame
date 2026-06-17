using UnityEngine;
using UnityEngine.InputSystem;

public class ForagingManager : MonoBehaviour
{
    #region fields

    [Header("Controller")]
    private InputSystem_Actions inputActions;
    
    [Header("References")]
    [SerializeField] private PlayerController player;

    #endregion


    #region gettersNsetters

    public InputSystem_Actions GetInputSystem() => inputActions;

    #endregion

    #region initialization

    void Awake()
    {
        inputActions = new InputSystem_Actions();
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

    public void onPlayerDeath()
    {
        isPlayerDead = true;
    }

    #endregion
}
