using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CraftingMachine : FactoryBlock
{
    [Header("Recipe Configuration")]
    public List<RecipeData> availableRecipes = new List<RecipeData>();

    [Header("Inventory")]
    public Inventory inventory = new();

    [Header("Processing")]
    public float progress;
    private RecipeData activeRecipe; // The recipe currently being processed
    
    [SerializeField]
    private ConveyorItemView conveyorItemPrefab;

    [System.Serializable]
    private class CraftingSaveState
    {
        public float progress;
        public Inventory inventory;
    }

    public override string GetSaveState()
    {
        CraftingSaveState state = new CraftingSaveState
        {
            progress = this.progress,
            inventory = this.inventory
        };
        return JsonUtility.ToJson(state);
    }

    public override void LoadSaveState(string stateJson)
    {
        if (!string.IsNullOrEmpty(stateJson))
        {
            CraftingSaveState state = new CraftingSaveState();
            state.inventory = new Inventory(); // ensure it has a valid reference
            JsonUtility.FromJsonOverwrite(stateJson, state);
            this.progress = state.progress;
            this.inventory = state.inventory;
        }
    }

    public override void Tick()
    {
        ProcessRecipe();
        TryOutput();
    }

    public override bool TryReceiveItem(ConveyorItem item, Port receivingPort)
    {
        if (availableRecipes == null || availableRecipes.Count == 0) return false;

        // Accept item if it is an input for ANY available recipe
        bool isInputItem = availableRecipes.Any(r => r.Inputs.Any(input => input.type == item.Type));
        if (!isInputItem) return false;
        
        if (inventory.AddItem(item.Type))
        {
            if (item.View != null) Destroy(item.View.gameObject);
            return true;
        }
        return false;
    }

    private void ProcessRecipe()
    {
        if (availableRecipes == null || availableRecipes.Count == 0) return;

        // If we don't have an active recipe, or the active recipe is no longer valid, find a new one
        if (activeRecipe == null || !inventory.ContainsItems(activeRecipe.Inputs) || !inventory.CanAddItems(activeRecipe.Outputs))
        {
            activeRecipe = null;
            progress = 0f;
            foreach (var recipe in availableRecipes)
            {
                if (inventory.ContainsItems(recipe.Inputs) && inventory.CanAddItems(recipe.Outputs))
                {
                    activeRecipe = recipe;
                    break;
                }
            }
        }

        // Process the active recipe
        if (activeRecipe != null)
        {
            float tickDelta = FactoryTickManager.Instance.TickRate;
            if (progress < activeRecipe.ProcessTime)
            {
                progress += tickDelta;
            }
            else
            {
                inventory.RemoveItems(activeRecipe.Inputs);
                inventory.AddItems(activeRecipe.Outputs);
                progress = 0f;
                activeRecipe = null; // Reset to re-evaluate next tick (allows switching recipes)
            }
        }
    }

    private void TryOutput()
    {
        if (availableRecipes == null || availableRecipes.Count == 0) return;

        Port outPort = Ports.Find(p => p.IsOutput && p.ConnectedBlock != null);
        if (outPort == null) return;

        // Gather all possible output types from all recipes
        var allOutputTypes = availableRecipes.SelectMany(r => r.Outputs).Select(o => o.type).Distinct();

        foreach (var outputType in allOutputTypes)
        {
            if (inventory.GetAmount(outputType) > 0)
            {
                ConveyorItem item = new ConveyorItem();
                item.Type = outputType;
                
                ConveyorItemView itemView = null;
                if (conveyorItemPrefab != null)
                {
                    itemView = Instantiate(conveyorItemPrefab, transform.position, Quaternion.identity);
                    itemView.GetComponent<SpriteRenderer>().sprite = ResourcesManager.instance.getResourceSprite(outputType);
                    item.View = itemView;
                }

                if (outPort.ConnectedBlock.TryReceiveItem(item, outPort.ConnectedPort))
                {
                    inventory.RemoveItem(outputType);
                    return; // Output one item per tick max
                }
                else
                {
                    if (itemView != null) Destroy(itemView.gameObject);
                }
            }
        }
    }
}
