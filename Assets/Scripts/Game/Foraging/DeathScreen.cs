using UnityEngine;
using UnityEngine.InputSystem;

public class DeathScreen : MonoBehaviour
{
    public PlayerConfig player;
    public InputSystem_Actions inputActions;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        gameObject.SetActive(false);
    }

    public void RespawnPlayer()
    {
        if (!isActiveAndEnabled) return;
        
        gameObject.SetActive(false);
        player.Respawn();
    }
}
