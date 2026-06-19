using UnityEngine;

[CreateAssetMenu(fileName = "NewDoubleCannonModule", menuName = "Module/DoubleCannon")]
public class ModuleDoubleCannon : PlayerModule, IModuleCannon
{
    [Header("Shooting settings")]
    [SerializeField] private float shootCoolDown = 1;
    private float nextShootTime = 0f;

    public int AttackStart(GameObject attackPrefab, AttackData data, Transform parent, float rotation)
    {
        if (Time.time < nextShootTime) return 0;
        
        nextShootTime = Time.time + shootCoolDown;

        IAttack attack1 = Instantiate(attackPrefab, parent.position + parent.up.normalized * 0.1f, Quaternion.identity).GetComponent<IAttack>();
        IAttack attack2 = Instantiate(attackPrefab, parent.position - parent.up.normalized * 0.1f, Quaternion.identity).GetComponent<IAttack>();
        attack1.Initialize(data, parent, rotation);
        attack2.Initialize(data, parent, rotation);
        
        SpendCharges(1);

        return 3;
    }

    public override void Disable()
    {
        controller.Disable(this);
    }

    public override void Enable()
    {
        controller.Enable(this);
    }

    public float GetNextHit()
    {
        return nextShootTime;
    }

    public void SetNextHit(float next)
    {
        nextShootTime = next;
    }
}
