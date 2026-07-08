using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class InventoryManagement : MonoBehaviour
{
    [Header("Counters")]
    [SerializeField] private Transform countersParent;
    [SerializeField] private GameObject counterPrefab;
    [SerializeField] private int yOffset = 105;
    private List<ItemCounter> counters = new();
    private Vector3 up;
    private Vector3 down;

    private ResourcesManager rm;

    void OnEnable()
    {
        if (PlayerInventory.Instance != null && PlayerInventory.Instance.ForagingInventory != null)
        {
            PlayerInventory.Instance.ForagingInventory.OnInventoryChanged += RefreshCounters;
        }
    }

    void OnDisable()
    {
        if (PlayerInventory.Instance != null && PlayerInventory.Instance.ForagingInventory != null)
        {
            PlayerInventory.Instance.ForagingInventory.OnInventoryChanged -= RefreshCounters;
        }
    }

    void Start()
    {
        rm = ResourcesManager.instance;
        up = new Vector3(0, yOffset, 0);
        down = new Vector3(0, -yOffset, 0);

        foreach (ItemType item in Enum.GetValues(typeof(ItemType)))
        {
            ItemCounter counter = new ItemCounter(Instantiate(counterPrefab), item);
            counter.Disable();
            counter.transform.SetParent(countersParent, false);
            counter.image.sprite = rm != null ? rm.getResourceSprite(item) : null;
            counters.Add(counter);
        }

        RefreshCounters();
    }

    private void RefreshCounters()
    {
        if (PlayerInventory.Instance == null || PlayerInventory.Instance.ForagingInventory == null) return;
        
        foreach (ItemCounter counter in counters)
        {
            updateCounters(counter.type, PlayerInventory.Instance.ForagingInventory.GetAmount(counter.type));
        }
    }

    private void updateCounters(ItemType type, int amount)
    {
        ItemCounter counter = counters[(int)type];
        counter.text.text = amount.ToString();

        if (amount > 0 && !counter.enabled)
        {
            counter.Enable();
            for (int i = (int)type + 1; i < counters.Count; i++)
            {
                counters[i].transform.position += up;
            }
        }
        else if (amount == 0 && counter.enabled)
        {
            counter.Disable();
            bool last = true;
            for (int i = (int)type + 1; i < counters.Count; i++)
            {
                counters[i].transform.position += down;
                last = false;
            }

            if (last) counters[(int)type].transform.position += down;
        }
    }
}

class ItemCounter
{
    public Image image;
    public TextMeshProUGUI text;
    public Transform transform;
    public ItemType type;
    public bool enabled = true;

    public ItemCounter(GameObject counter, ItemType t)
    {
        transform = counter.transform;
        image = counter.GetComponentInChildren<Image>();
        text = counter.GetComponentInChildren<TextMeshProUGUI>();
        type = t;
    }

    public void Enable()
    {
        image.enabled = true;
        text.enabled = true;
        enabled = true;
    }

    public void Disable()
    {
        image.enabled = false;
        text.enabled = false;
        enabled = false;
    }
}
