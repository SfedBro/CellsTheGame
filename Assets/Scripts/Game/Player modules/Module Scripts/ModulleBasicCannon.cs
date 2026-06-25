using UnityEngine;

[CreateAssetMenu(fileName = "NewBasicCannonModule", menuName = "Module/BasicCannon")]
public class ModulleBasicCannon : PlayerModule, IModuleCannon
{
    #region fields

    [Header("Shooting settings")]
    [SerializeField] private float shootCoolDownMultiplier = 1;
    [SerializeField] private int shootCost = 1;
    private float nextShootTime = 0f;

    #endregion


    #region getters/setters

    public float GetNextHit()
    {
        return nextShootTime;
    }

    public void SetNextHit(float next)
    {
        nextShootTime = next;
    }

    #endregion


    #region attack
    private GameObject lastAttackObject;
    private IAttack lastAttack;

    public int AttackStart(GameObject attackPrefab, AttackData data, Transform parent, float rotation)
    {
        if (Time.time < nextShootTime) return 0;
        
        nextShootTime = Time.time + data.attakCoolDown * shootCoolDownMultiplier;

        lastAttackObject = Instantiate(attackPrefab, parent.position, Quaternion.identity);
        lastAttack = lastAttackObject.GetComponent<IAttack>();
        lastAttack.Initialize(data, parent, rotation);

        return shootCost;
    }

    public int ActivateAttack()
    {
        if (lastAttackObject != null) lastAttack.Activate();

        return shootCost;
    }

    public void AttackEnd(bool destroy)
    {
        if (destroy) Destroy(lastAttackObject);
        lastAttackObject = null;
        lastAttack = null;
    }

    #endregion
}
