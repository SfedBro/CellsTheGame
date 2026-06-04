using UnityEngine;
using UnityEngine.InputSystem;

public class DeathScreen : MonoBehaviour
{
    public PlayerConfig player;
    public InputSystem_Actions inputActions;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void RespawnPlayer()
    {
        gameObject.SetActive(false);
        player.Respawn();
    }
}
