using UnityEngine;

public class EnemyBody : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            print("Collide with player");
        }
    }
}
