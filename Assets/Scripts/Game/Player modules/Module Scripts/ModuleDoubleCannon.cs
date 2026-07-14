using UnityEngine;

[CreateAssetMenu(fileName = "NewDoubleCannonModule", menuName = "Module/DoubleCannon")]
public class ModuleDoubleCannon : PlayerModule, IModuleCannon
{
    #region fields

    [Header("Shooting settings")]
    [SerializeField] private GameObject attackPrefab;

    #endregion


    #region attack

    private GameObject lastAttackObject1;
    private GameObject lastAttackObject2; 
    private IAttack lastAttack1;
    private IAttack lastAttack2;
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
        lastAttackObject1 = Instantiate(attackPrefab, parent.position + parent.right.normalized * 0.1f, Quaternion.identity);
        lastAttackObject2 = Instantiate(attackPrefab, parent.position - parent.right.normalized * 0.1f, Quaternion.identity);
        lastAttack1 = lastAttackObject1.GetComponent<IAttack>();
        lastAttack2 = lastAttackObject2.GetComponent<IAttack>();
        lastAttack1.Initialize(attackData, parent);
        lastAttack2.Initialize(attackData, parent);
    }

        public void AttackEnd() {}

    #endregion
}
