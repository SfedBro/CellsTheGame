using System;
using UnityEngine;

public class LootItem : MonoBehaviour
{
    private CollectableData collectableData;
    private int amount;
    private Action<CollectableData> onCollected;

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
        if (collision.gameObject.CompareTag("CollectAura"))
        {
            ResourcesManager.instance.addResourceAmount(collectableData.GetItemType(), amount);

            if (onCollected != null) onCollected(collectableData);

            Destroy(gameObject);
        }
    }
}
