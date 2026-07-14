using UnityEngine;


[CreateAssetMenu(fileName = "NewModuleCannonLaser", menuName = "Module/CannonLaser")]
public class ModuleCannonLaser : PlayerModule, IModuleCannon
{
    #region fields

    [Header("Shooting settings")]
    [SerializeField] private GameObject attackPrefab;

    #endregion


    #region attack
    private GameObject lastAttackObject;
    private Laser lastAttack;
    private AttackData attackData;
    private Transform parent;

    public void Initialize(AttackData attackData, Transform parent)
    {
        this.attackData = attackData;
        this.parent = parent;
    }

    public void AttackStart()
    {
        lastAttackObject = Instantiate(attackPrefab, parent.position, Quaternion.identity);
        lastAttack = lastAttackObject.GetComponent<Laser>();
        lastAttack.Initialize(attackData, parent);
    }

    public void AttackActivate()
    {
        lastAttack.ActivateAttack();
    }

    public void AttackEnd()
    {
        Destroy(lastAttackObject);
        lastAttackObject = null;
        lastAttack = null;
    }

    #endregion
}
