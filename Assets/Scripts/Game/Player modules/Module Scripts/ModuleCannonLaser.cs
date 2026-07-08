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
    private IAttack lastAttack;

    public int AttackStart(AttackData data, Transform parent, float rotation)
    {
        lastAttackObject = Instantiate(attackPrefab, parent.position, Quaternion.identity);
        lastAttack = lastAttackObject.GetComponent<IAttack>();
        lastAttack.Initialize(data, parent, rotation);

        return 1;
    }

    public void AttackEnd()
    {
        Destroy(lastAttackObject);
        lastAttackObject = null;
        lastAttack = null;
    }

    #endregion
}
