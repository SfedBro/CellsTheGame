using UnityEngine;

[CreateAssetMenu(fileName = "NewBasicCannonModule", menuName = "Module/BasicCannon")]
public class ModulleBasicCannon : PlayerModule, IModuleCannon
{
    [Header("Shooting settings")]
    [SerializeField] private float shootCoolDown = 1;
    private float nextShootTime = 0f;

    public int AttackStart(GameObject attackPrefab, AttackData data, Transform parent, float rotation)
    {
        if (Time.time < nextShootTime) return 0;
        
        nextShootTime = Time.time + shootCoolDown;

        IAttack attack = Instantiate(attackPrefab, parent.position, Quaternion.identity).GetComponent<IAttack>();
        attack.Initialize(data, parent, rotation);

        return 1;
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
