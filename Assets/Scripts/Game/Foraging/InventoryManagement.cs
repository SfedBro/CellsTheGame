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


    private ResourcesManager rm = ResourcesManager.instance;

    void OnEnable()
    {
        ResourcesManager.instance.Subscrive(updateCounters);
    }

    void OnDisable()
    {
        ResourcesManager.instance.Unsubscrive(updateCounters);
    }

    void Start()
    {
        up = new Vector3(0, yOffset, 0);
        down = new Vector3(0, -yOffset, 0);

        foreach (ItemType item in Enum.GetValues(typeof(ItemType)))
        {
            ItemCounter counter = new ItemCounter(Instantiate(counterPrefab), item);
            counter.Disable();
            counter.transform.SetParent(countersParent);
            counter.image.sprite = rm.getResourceSprite(item);
            counters.Add(counter);
        }

        foreach (ItemCounter counter in counters)
        {
            updateCounters(counter.type, rm.getResourceAmount(counter.type));
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
            for (int i = (int)type; i < counters.Count; i++)
            {
                counters[i].transform.position += down;
            }
        }
    }

    public int getResourceAmount(ItemType t)
    {
        return rm.getResourceAmount(t);
    }

    public void addRes(ItemType t, int amount)
    {
        rm.addResourceAmount(t, amount);
    }

    public void onPlayerDeath()
    {
        int r1 = rm.getResourceAmount(ItemType.TestOre) / -2;
        addRes(ItemType.TestOre, r1);

        int r2 = rm.getResourceAmount(ItemType.TestPlate) / -2;
        addRes(ItemType.TestPlate, r2);
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