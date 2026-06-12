using System.Collections.Generic;
using UnityEngine;

public class FactoryTickManager : MonoBehaviour, IGameService
{
    public static FactoryTickManager Instance;

    public float TickRate = 0.1f;
    private float timer;

    private List<FactoryBlock> activeBlocks = new List<FactoryBlock>();

    public void InitializeService()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;
    }

    public void StartService()
    {
    }

    public void RegisterBlock(FactoryBlock block)
    {
        if (!activeBlocks.Contains(block))
            activeBlocks.Add(block);
    }

    public void UnregisterBlock(FactoryBlock block)
    {
        activeBlocks.Remove(block);
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= TickRate)
        {
            timer -= TickRate;
            
            // To handle modifications during iteration (e.g. destruction) safely,
            // we iterate backwards or use a copy, but backwards is generally faster if blocks remove themselves.
            for (int i = activeBlocks.Count - 1; i >= 0; i--)
            {
                if (activeBlocks[i] != null)
                {
                    activeBlocks[i].Tick();
                }
                else
                {
                    activeBlocks.RemoveAt(i);
                }
            }
        }
    }
}
