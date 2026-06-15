using UnityEngine;

public class BasicCannon : Cannon
{
    public BasicCannon(GameObject bullet, float coolDown) : base(bullet, coolDown)
    {
    }

    public override void Shoot(Transform player, bool flip, int dmg)
    {
        if (Time.time < nextShootTime) return;
        
        nextShootTime = Time.time + curCoolDown;

        GameObject b = Object.Instantiate(bulletPrefab, player.position, player.rotation);
        b.transform.Rotate(new Vector3(0, flip? 180 : 0, 0));
        b.transform.parent = player;
        b.transform.localScale = new Vector3((dmg + 3f) / 8f, (dmg + 3f) / 8f, 1);
        Bullet bullet = b.GetComponent<Bullet>();
        bullet.dmg = dmg;
    }
}
