using UnityEngine;
using UnityEngine.Events;

public class PlayerTrigger : MonoBehaviour
{
    public UnityEvent onHubEnter;
    public UnityEvent onHubExit;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("HUB"))
        {
            onHubEnter.Invoke();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("HUB"))
        {
            onHubExit.Invoke();
        }
    }
}
