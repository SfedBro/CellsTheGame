using UnityEngine;

public class EnemyBody : MonoBehaviour
{
    public EnemyBase self;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerConfig p = collision.gameObject.GetComponent<PlayerConfig>();
            p.getDMG(self.GetAttack());
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Bullet"))
        {
            Bullet b = collision.gameObject.GetComponent<Bullet>();
            if (!b.isEnemy) {
                self.GetDamage(b.dmg);
                Destroy(collision.gameObject);
            }
        }
    }
}
