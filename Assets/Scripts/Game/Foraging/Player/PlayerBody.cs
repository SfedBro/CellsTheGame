using UnityEngine;

public class PlayerBody : MonoBehaviour
{
    [SerializeField] private PlayerConfig player;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Bullet"))
        {
            Bullet b = collision.gameObject.GetComponent<Bullet>();
            if (b.isEnemy) {
                player.getDMG(b.dmg);
                Destroy(collision.gameObject);
            }
            return;
        }

        Interactable i = collision.GetComponent<Interactable>();
        if (i != null) {
            i.onZoneEnter();
            player.onInteractZoneEnter(i.getTime(), i.onInteract);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        Interactable i = collision.GetComponent<Interactable>();
        if (i != null) {
            i.onZoneExit();
            player.onInteractZoneExit();
        }
    }
}
