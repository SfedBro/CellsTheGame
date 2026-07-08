using System;
using UnityEngine;

public class LootItem : MonoBehaviour
{
    private CollectableData collectableData;
    private int amount;
    private Action<CollectableData> onCollected;
    private bool isLooted = false;

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Setup(CollectableData data, Action<CollectableData> action, int a)
    {
        collectableData = data;
        spriteRenderer.sprite = data.GetSprite();
        GetComponent<CircleCollider2D>().radius = data.GetRadius();

        onCollected = action;
        amount = a;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isLooted) return;

        if (collision.gameObject.CompareTag("CollectAura"))
        {
            isLooted = true;

            if (PlayerInventory.Instance != null && collectableData != null)
            {
                for (int i = 0; i < amount; i++)
                {
                    PlayerInventory.Instance.ForagingInventory.AddItem(collectableData.GetItemType());
                }
            }

            if (onCollected != null) onCollected(collectableData);

            Destroy(gameObject);
        }
    }
}
