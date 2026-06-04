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
            self.GetDamage(collision.gameObject.GetComponent<Bullet>().dmg);
            Destroy(collision.gameObject);
        }
    }
}
