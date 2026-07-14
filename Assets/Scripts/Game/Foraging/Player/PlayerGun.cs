using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGun : MonoBehaviour
{
    #region fields

    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Camera playerCamera;
    public PlayerStats curPlayerStats = new(0);

    [Header("Fight")]
    private AttackData attackData;
    private bool isAttacking;
    private float attackCoolDownTimer = 0f;

    private float curAngle = 0;
    private float targetAngle = 0;

    #endregion


    #region getters n setters

    public void SetAttackData(AttackData data)
    {
        attackData = data;
    }

    private float ActiveCoolDown
    {
        get
        {
            float cd = curPlayerStats.attackCoolDown;
            if (playerController != null && playerController.CurrentWeapon != null)
            {
                cd *= playerController.CurrentWeapon.CoolDownMultiplier;
            }
            return cd;
        }
    }

    #endregion


    #region initialization

    void Awake()
    {
        if (playerController == null)
        {
            playerController = GetComponentInParent<PlayerController>();
        }
        
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    #endregion


    #region lifecycle

    void Update()
    {
        // ROTATION
        if (playerCamera != null)
        {
            Vector2 direction = (playerCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - transform.position).normalized;
            targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            curAngle = Mathf.MoveTowardsAngle(curAngle, targetAngle, curPlayerStats.rotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.AngleAxis(curAngle - 90, Vector3.forward);
        }

        // ATTACK COOLDOWN & TICK LOOP
        float activeCD = ActiveCoolDown;
        if (isAttacking)
        {
            if (attackCoolDownTimer >= activeCD)
            {
                attackCoolDownTimer = 0f;
                FireTick();
            }
        }

        if (attackCoolDownTimer < activeCD)
        {
            attackCoolDownTimer += Time.deltaTime;
        }
    }

    #endregion


    #region attack

    public void AttackStart()
    {
        isAttacking = true;
        
        if (playerController != null && playerController.CurrentWeapon != null)
        {
            playerController.CurrentWeapon.StartAttack(playerController, curAngle);
        }

        // Fire first tick immediately if cooldown is ready
        if (attackCoolDownTimer >= ActiveCoolDown)
        {
            attackCoolDownTimer = 0f;
            FireTick();
        }
    }

    public void AttackEnd()
    {
        isAttacking = false;
        attackCoolDownTimer = 0f;

        if (playerController != null && playerController.CurrentWeapon != null)
        {
            playerController.CurrentWeapon.EndAttack(playerController);
        }
    }

    private void FireTick()
    {
        if (playerController != null && playerController.CurrentWeapon != null)
        {
            playerController.CurrentWeapon.AttackTick(playerController);
        }
    }

    #endregion
}
