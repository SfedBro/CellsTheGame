using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGun : MonoBehaviour
{
    #region fields

    [Header("references")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Camera playerCamera;
    public PlayerStats curPlayerStats = new(0);
    private float curAngle = 0;
    private float targetAngle = 0;

    #endregion


    #region lifecycle

    void Update()
    {
        // ROTATION
        Vector2 direction = (playerCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - transform.position).normalized;
        targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        curAngle = Mathf.MoveTowardsAngle(curAngle, targetAngle, curPlayerStats.rotationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.AngleAxis(curAngle - 90, Vector3.forward);
    }

    #endregion
}
