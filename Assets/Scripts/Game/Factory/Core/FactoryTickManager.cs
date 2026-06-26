using System.Collections.Generic;
using UnityEngine;

public class FactoryTickManager : MonoBehaviour, IGameService
{
    public static FactoryTickManager Instance;

    public float TickRate = 0.125f;
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
            
            // Phase 1: Internal movement
            for (int i = activeBlocks.Count - 1; i >= 0; i--)
            {
                if (activeBlocks[i] != null)
                {
                    activeBlocks[i].PreTick();
                }
                else
                {
                    activeBlocks.RemoveAt(i);
                }
            }

            // Phase 2: Push items out
            for (int i = activeBlocks.Count - 1; i >= 0; i--)
            {
                if (activeBlocks[i] != null)
                {
                    activeBlocks[i].Tick();
                }
            }
        }
    }
}
