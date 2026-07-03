using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class PlayerExperienceManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI upgradePointsUI;
    [SerializeField] private TextMeshProUGUI curLevelUI;
    [SerializeField] private TextMeshProUGUI nextLevelUI;
    [SerializeField] private Image experienceIndicator;
    private int upgradePoints = 0;
    private int curLevel = 0;

    [Header("Spawn")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private int generationMaxAttemp = 5;
    [SerializeField] private GameObject experiancePrefab;
    [SerializeField] private CollectableData data;
    [SerializeField] private float spawnCoolDown = 3f;
    [SerializeField] private int maxExperienceOnMap = 100;
    [SerializeField] private int experienceOnMap = 10;
    private Rect cameraView;
    private int curLevelRequirement = 7;
    private int curExperience = 0;
    private float spawnTimer = 0f;

    private PlayerLevelManager plm = PlayerLevelManager.instance;

    private List<Action<ItemType, int>> observers = new();

    public int GetUpgradePoints() => upgradePoints;

    public void RemoveUpgradePoints(int amount)
    {
        upgradePoints -= amount;
        plm.curUpgradePoints = upgradePoints;
        upgradePointsUI.text = upgradePoints.ToString();

        foreach (Action<ItemType, int> action in observers)
        {
            action(ItemType.Default, upgradePoints);
        }
    }

    void Start()
    {
        float height = 2f * playerCamera.orthographicSize;
        float width = height * playerCamera.aspect;
        Vector3 bottomLeft = playerCamera.transform.position - new Vector3(width/2, height/2, 0);
        cameraView = new Rect(bottomLeft.x, bottomLeft.y, width, height);

        curExperience = plm.curExperience;
        curLevel = plm.curPlayerLevel;
        upgradePoints = plm.curUpgradePoints;

        curLevelRequirement = 2 * curLevel + 5;

        upgradePointsUI.text = upgradePoints.ToString();
        curLevelUI.text = curLevel.ToString();
        nextLevelUI.text = (curLevel + 1).ToString();
        experienceIndicator.fillAmount = Mathf.Clamp01(curExperience / curLevelRequirement);

        for (int i = 0; i < experienceOnMap; i++)
        {
            spawnExperience();
        }
    }

    void Update()
    {
        if (experienceOnMap < maxExperienceOnMap)
        {
            spawnTimer += Time.deltaTime;

            if (spawnTimer >= spawnCoolDown)
            {
                spawnTimer = 0;
                spawnExperience();
            }
        }
    }

    private void spawnExperience()
    {
        cameraView.center = playerCamera.transform.position;
        Vector2 newPos = data.getNewPosition();

        for (int i = 0; i < generationMaxAttemp; i++)
        {
            if (cameraView.Contains(newPos))
            {
                newPos = data.getNewPosition();
            }
            else
            {
                break;
            }
        }

        ExperienceParticle spawned = Instantiate(experiancePrefab, newPos, Quaternion.identity).GetComponent<ExperienceParticle>();
        spawned.Setup(onCollected, 1);
        spawned.transform.SetParent(transform, false);
    }

    public void spawnExperience(Vector3 pos, int amount)
    {
        ExperienceParticle spawned = Instantiate(experiancePrefab, pos, Quaternion.identity).GetComponent<ExperienceParticle>();
        spawned.Setup(onCollected, amount);
        spawned.transform.SetParent(transform, false);
        float scale = getExpScale(amount);
        spawned.transform.localScale = new Vector3(scale, scale, 1);
    }

    private float getExpScale(int x)
    {
        return Math.Clamp(0.02f * x + 0.88f, 0.5f, 2f) * 0.5f;
    }

    private void onCollected(int amount)
    {
        experienceOnMap--;

        curExperience += amount;
        if (curExperience >= curLevelRequirement)
        {
            curExperience -= curLevelRequirement;

            curLevel++;
            plm.curPlayerLevel = curLevel;

            upgradePoints++;
            plm.curUpgradePoints = upgradePoints;

            curLevelRequirement = 2 * curLevel + 5;

            upgradePointsUI.text = upgradePoints.ToString();
            curLevelUI.text = curLevel.ToString();
            nextLevelUI.text = (curLevel + 1).ToString();

            foreach (Action<ItemType, int> action in observers)
            {
                action(ItemType.Default, upgradePoints);
            }
        }

        plm.curExperience = curExperience;
        experienceIndicator.fillAmount = Mathf.Clamp01(((float) curExperience) / curLevelRequirement);
    }

    public void CheatAddExperience()
    {
        experienceOnMap++;
        onCollected(1);
    }

    public void Subscribe(Action<ItemType, int> action)
    {
        observers.Add(action);
    }

    public void onPlayerDeath(EnemyBase killer)
    {
        killer.AddLoot(ItemType.Default, curExperience);

        curExperience = 0;
        plm.curExperience = 0;
        curLevel = 0;
        plm.curPlayerLevel = 0;
        upgradePoints = 0;
        plm.curUpgradePoints = 0;

        curLevelRequirement = 7;

        upgradePointsUI.text = upgradePoints.ToString();
        curLevelUI.text = curLevel.ToString();
        nextLevelUI.text = (curLevel + 1).ToString();
        experienceIndicator.fillAmount = Mathf.Clamp01(curExperience / curLevelRequirement);

        foreach (Action<ItemType, int> action in observers)
        {
            action(ItemType.Default, upgradePoints);
        }
    }
}
