using System.Collections.Generic;
using UnityEngine;

public class MineManager : MonoBehaviour, IPausable
{
    [Header("Dependencies")]
    [SerializeField] private PlayerExperienceManager playerExperienceManager;
    [SerializeField] private ResourceGenerator resourceGenerator;
    [SerializeField] private PauseController pauseController;

    [Header("Resource generation")]
    [SerializeField] private GameObject minePrefab;
    [SerializeField] private List<MineData> minesData;
    [SerializeField] private List<int> amount;
    [SerializeField] private float mineTick = 1f;
    private List<Mine> mines = new();
    private float nextTick = 0f;
    private bool pause = false;

    void Start()
    {
        for (int i = 0; i < minesData.Count; i++)
        {
            MineData data = minesData[i];

            for (int j = 0; j < amount[i]; j++)
            {
                Vector2 newPos = data.getSelfPosition();
                Mine m = Instantiate(minePrefab, new Vector3(newPos.x, newPos.y, 2), Quaternion.identity).GetComponent<Mine>();
                m.transform.SetParent(transform, false);
                m.Setup(data, spawnLoot, pauseController);
                mines.Add(m);
            }
        }
    }

    void Update()
    {
        if (nextTick < Time.time)
        {
            nextTick += mineTick;
            Tick();
        }
    }

    private void Tick()
    {
        if (pause) return;
        
        foreach (Mine m in mines)
        {
            m.Tick();
        }
    }

    private void spawnLoot(List<LootAmount> loot, Vector3 pos)
    {
        foreach (LootAmount l in loot)
        {
            if (l.GetItemType() == ItemType.Default)
            {
                playerExperienceManager.spawnExperience(pos + (Vector3)(Random.insideUnitCircle * 0.5f), l.amount);
                continue;
            }
            resourceGenerator.spawnLoot(pos + (Vector3)(Random.insideUnitCircle * 0.5f), l.GetItemType(), l.amount);
        }
    }

    public void SetPause(bool pause)
    {
        this.pause = pause;
    }
}
