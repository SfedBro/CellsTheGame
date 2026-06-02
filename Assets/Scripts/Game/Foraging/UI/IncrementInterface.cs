using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

public class IncrementInterface : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private TextMeshProUGUI level;
    [SerializeField] private Button btn;
    [SerializeField] private GameObject requirementPrefab;
    [SerializeField] private Transform requirementParent;
    [SerializeField] private int xOffset = 155;

    private Vector3 left;
    private List<GameObject> requirements = new();
    private List<TextMeshProUGUI> texts = new();

    public UpgradeData upgradeData;
    public Func<UpgradeData, bool> upgrade;

    void OnEnable()
    {
        left = new Vector3(xOffset, 0, 0);
    }

    public void UpdateUI()
    {
        foreach (GameObject g in requirements)
        {
            Destroy(g);
        }
        requirements.Clear();
        texts.Clear();

        label.text = upgradeData.GetStatType().ToString();
        level.text = upgradeData.curLevel.ToString();

        if (upgradeData.curLevel == upgradeData.maxLevel) return;

        int i = 0;
        foreach (ResourceCost cost in upgradeData.getCurCost())
        {
            GameObject req = Instantiate(requirementPrefab);
            req.transform.SetParent(requirementParent, false);
            req.transform.position += i * left;
            req.GetComponentInChildren<Image>().sprite = ResourcesManager.instance.getResourceSprite(cost.resource);
            TextMeshProUGUI text = req.GetComponentInChildren<TextMeshProUGUI>();
            text.text = cost.amount.ToString();
            i++;

            requirements.Add(req);
            texts.Add(text);

            if (ResourcesManager.instance.getResourceAmount(cost.resource) < cost.amount)
            {
                text.color = Color.red;
            }
            else
            {
                text.color = Color.black;
            }
        }
    }

    public void onUpgradeTap()
    {
        if (upgrade(upgradeData))
        {
            UpdateUI();
        }        
    }

    public void UpdateRequirements(ItemType type, int amount)
    {
        int i = 0;
        foreach (ResourceCost cost in upgradeData.getCurCost())
        {
            if (cost.resource == type)
            {
                if (amount < cost.amount)
                {
                    texts[i].color = Color.red;
                }
                else
                {
                    texts[i].color = Color.black;
                }
                return;
            }
            i++;
        }
    }
}
