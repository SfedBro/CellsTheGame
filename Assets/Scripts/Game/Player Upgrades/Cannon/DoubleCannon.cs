using UnityEngine;

public class DoubleCannon : Cannon
{
    public DoubleCannon(GameObject bullet, float coolDown) : base(bullet, coolDown)
    {
    }

    public override void Shoot(Transform player, bool flip, int dmg)
    {
        if (Time.time < nextShootTime) return;
        
        nextShootTime = Time.time + curCoolDown;

        GameObject b1 = Object.Instantiate(bulletPrefab, player.position, player.rotation);
        GameObject b2 = Object.Instantiate(bulletPrefab, player.position, player.rotation);
        b1.transform.Rotate(new Vector3(0, flip? 180 : 0, 0));
        b2.transform.Rotate(new Vector3(0, flip? 180 : 0, 0));
        b1.transform.position += b1.transform.up.normalized * 0.1f;
        b2.transform.position += b2.transform.up.normalized * -0.1f;
        b1.transform.parent = player;
        b2.transform.parent = player;
        b1.transform.localScale = new Vector3((dmg + 3f) / 8f, (dmg + 3f) / 8f, 1);
        b2.transform.localScale = new Vector3((dmg + 3f) / 8f, (dmg + 3f) / 8f, 1);
        Bullet bullet1 = b1.GetComponent<Bullet>();
        bullet1.dmg = dmg;
        Bullet bullet2 = b2.GetComponent<Bullet>();
        bullet2.dmg = dmg;
    }
}
