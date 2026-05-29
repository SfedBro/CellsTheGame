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
    public ResourceGenerator resourceGenerator;

    [Header("Upgrading")]
    public int[] resourceTypes;
    public int[] resourceAmount;
    public UnityEvent upgrade;

    private int curLevel = 0;
    private int maxLevel;

    private int curRequirement;
    private int curResource;

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
        resourceType.sprite = resources[curResource];

    }

    public void FixedUpdate()
    {
        if (curRequirement > resourceGenerator.getResourceAmount(curResource))
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
        if (curRequirement <= resourceGenerator.getResourceAmount(curResource))
        {
            upgrade.Invoke();
            curLevel++;
            resourceGenerator.removeResource(curResource, curRequirement);


            if (curLevel >= maxLevel)
            {
                upgradeBtn.enabled = false;
            }

            nextLevelPreparation();
        }
    }
}
