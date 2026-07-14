using System.Collections;
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
    [SerializeField] private PlayerModule baseCannonModule;
    private AttackData attackData;
    private IModuleCannon baseCannon;
    private IModuleCannon curCannon;
    private bool isAttacking;
    private float attackCoolDownTiemer = 0f;

    private float curAngle = 0;
    private float targetAngle = 0;

    #endregion


    #region getters n setters

    public void SetCannon(IModuleCannon cannon)
    {
        if (cannon == null)
        {
            curCannon = baseCannon;
        } else
        {
            curCannon = cannon;
            curCannon.Initialize(attackData, transform);
        }
    }

    public void SetAttackData(AttackData data)
    {
        attackData = data;
        baseCannon.Initialize(data, transform);
    }

    #endregion


    #region initialization

    void Awake()
    {
        if (baseCannonModule is IModuleCannon)
        {
            baseCannon = (IModuleCannon)Instantiate(baseCannonModule);
            curCannon = baseCannon;
        } 
        else
        {
            Debug.LogError("Invalid Base Cannon Set in Player -> PlayerController!");
        }
    }

    #endregion


    #region lifecycle

    void Update()
    {
        // ROTATION
        Vector2 direction = (playerCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - transform.position).normalized;
        targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        curAngle = Mathf.MoveTowardsAngle(curAngle, targetAngle, curPlayerStats.rotationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.AngleAxis(curAngle - 90, Vector3.forward);

        if (attackCoolDownTiemer > curPlayerStats.attackCoolDown)
        {
            if (isAttacking) {
                attackCoolDownTiemer = 0f;
                curCannon.AttackActivate();
            }
        } else
        {
            attackCoolDownTiemer += Time.deltaTime;
        }
    }

    #endregion


    #region attack

    public void AttackStart()
    {
        curCannon.AttackStart();
        isAttacking = true;
    }

    public void AttackEnd()
    {
        curCannon.AttackEnd();
        isAttacking = false;
        attackCoolDownTiemer = 0f;
    }

    #endregion
}
