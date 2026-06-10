using System.Collections.Generic;
using UnityEngine;

public class MineManager : MonoBehaviour
{
    [Header("Resource generation")]
    [SerializeField] private GameObject minePrefab;
    [SerializeField] private List<MineData> minesData;
    [SerializeField] private List<int> amount;
    [SerializeField] private float mineTick = 1f;
    private List<Mine> mines = new();
    private float nextTick = 0f;

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
                m.Setup(data);
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
        foreach (Mine m in mines)
        {
            m.Tick();
        }
    }
}
