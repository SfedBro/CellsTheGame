using UnityEngine;

[CreateAssetMenu(fileName = "NewBasicCannonModule", menuName = "Module/BasicCannon")]
public class ModulleBasicCannon : PlayerModule, IModuleCannon
{
    #region fields

    [Header("Shooting settings")]
    [SerializeField] private GameObject attackPrefab;

    #endregion


    #region attack
    private GameObject lastAttackObject;
    private IAttack lastAttack;
    private AttackData attackData;
    private Transform parent;

    public void Initialize(AttackData attackData, Transform parent)
    {
        this.attackData = attackData;
        this.parent = parent;
    }

    public void AttackStart() {}

    public void AttackActivate()
    {
        lastAttackObject = Instantiate(attackPrefab, parent.position, Quaternion.identity);
        lastAttack = lastAttackObject.GetComponent<IAttack>();
        lastAttack.Initialize(attackData, parent);
    }

    public void AttackEnd() {}

    #endregion
}
