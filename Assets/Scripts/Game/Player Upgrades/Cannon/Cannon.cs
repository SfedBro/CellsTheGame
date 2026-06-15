using UnityEngine;

public abstract class Cannon
{
    protected GameObject bulletPrefab;


    private float initialShootCoolDown;
    protected float curCoolDown;
    protected float nextShootTime = 0;

    public Cannon(GameObject bullet, float coolDown)
    {
        bulletPrefab = bullet;
        initialShootCoolDown = coolDown;
        curCoolDown = coolDown;
    }

    public void SetCoolDown(float percent)
    {
        curCoolDown = initialShootCoolDown * percent;
    }

    public abstract void Shoot(Transform player, bool flip, int dmg);
}
