using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;


public class PlayerUpgrade : MonoBehaviour
{
    [Header("UI")]
    public TMPro.TextMeshProUGUI levelUI;
    public TMPro.TextMeshProUGUI resourceAmountUI;
    public Image resourceType;
    public Button upgradeBtn;

    [Header("Resources")]
    public Sprite[] resources;
    public InventoryManagement inventory;

    [Header("Upgrading")]
    public ItemType[] resourceTypes;
    public int[] resourceAmount;
    public UnityEvent upgrade;

    private int curLevel = 0;
    private int maxLevel;

    private int curRequirement;
    private ItemType curResource;

    public void Start()
    {
        maxLevel = Mathf.Max(resourceAmount.Length, resourceTypes.Length);

        nextLevelPreparation();
    }

    public void nextLevelPreparation()
    {
        levelUI.text = (curLevel + 1).ToString();

        curRequirement = resourceAmount[curLevel];
        resourceAmountUI.text = curRequirement.ToString();
        resourceAmountUI.color = Color.yellow;

        curResource = resourceTypes[curLevel];
        resourceType.sprite = resources[((int)curResource)];

    }

    public void FixedUpdate()
    {
        if (curRequirement > inventory.getResourceAmount(curResource))
        {
            resourceAmountUI.color = Color.red;
        }
        else
        {
            resourceAmountUI.color = Color.black;
        }
    }

    public void onUpgradeTap()
    {
        if (curRequirement <= inventory.getResourceAmount(curResource))
        {
            upgrade.Invoke();
            curLevel++;
            inventory.addRes(curResource, -curRequirement);


            if (curLevel >= maxLevel)
            {
                upgradeBtn.enabled = false;
            }

            nextLevelPreparation();
        }
    }
}
