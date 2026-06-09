using System;
using UnityEngine;

public class ExperienceParticle : MonoBehaviour
{
    private int amount;
    private Action<int> onCollected;

    public void Setup(Action<int> action, int a)
    {
        onCollected = action;
        amount = a;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("CollectAura"))
        {
            if (onCollected != null) onCollected(amount);

            Destroy(gameObject);
        }
    }
}
