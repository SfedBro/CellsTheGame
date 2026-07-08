using UnityEngine;

[CreateAssetMenu(fileName = "NewDoubleCannonModule", menuName = "Module/DoubleCannon")]
public class ModuleDoubleCannon : PlayerModule, IModuleCannon
{
    #region fields

    [Header("Shooting settings")]
    [SerializeField] private GameObject attackPrefab;
    [SerializeField] private float shootCoolDownMultiplier = 1.5f;
    [SerializeField] private int shootCost = 2;
    private float nextShootTime = 0f;

    #endregion


    #region attack

    private GameObject lastAttackObject1;
    private GameObject lastAttackObject2; 
    private IAttack lastAttack1;
    private IAttack lastAttack2;

    public int AttackStart(AttackData data, Transform parent, float rotation)
    {
        if (Time.time < nextShootTime) return 0;
        
        nextShootTime = Time.time + data.attakCoolDown * shootCoolDownMultiplier;

        lastAttackObject1 = Instantiate(attackPrefab, parent.position + parent.up.normalized * 0.1f, Quaternion.identity);
        lastAttackObject2 = Instantiate(attackPrefab, parent.position - parent.up.normalized * 0.1f, Quaternion.identity);
        lastAttack1 = lastAttackObject1.GetComponent<IAttack>();
        lastAttack2 = lastAttackObject2.GetComponent<IAttack>();
        lastAttack1.Initialize(data, parent, rotation);
        lastAttack2.Initialize(data, parent, rotation);
        
        SpendCharges(1);

        return shootCost;
    }

    public void AttackEnd()
    {
        if (lastAttackObject1 != null) Destroy(lastAttackObject1);
        if (lastAttackObject2 != null) Destroy(lastAttackObject2);
        lastAttackObject1 = null;
        lastAttackObject2 = null;
        lastAttack1 = null;
        lastAttack2 = null;
    }

    #endregion
}
