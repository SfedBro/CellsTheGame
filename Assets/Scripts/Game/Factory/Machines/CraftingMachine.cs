using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CraftingMachine : FactoryBlock, IInteractable, ICraftingProvider
{
    [Header("Recipe Configuration")]
    public List<RecipeData> availableRecipes = new List<RecipeData>();

    [Header("Inventory")]
    public Inventory inputInventory = new Inventory { slotCount = 4 };
    public Inventory outputInventory = new Inventory { slotCount = 1 };

    public Inventory InputInventory => inputInventory;
    public Inventory OutputInventory => outputInventory;
    public float ProgressPercentage => (activeRecipe != null && activeRecipe.ProcessTime > 0) ? (progress / activeRecipe.ProcessTime) : 0f;
    public string RecipeName => activeRecipe != null ? activeRecipe.RecipeName : "Нет рецепта";

    [Header("Processing")]
    public float progress;
    private RecipeData activeRecipe; // The recipe currently being processed

    public void Interact(PlayerInteractor player)
    {
        if (PlayerInventoryWindow.Instance != null)
        {
            PlayerInventoryWindow.Instance.SelectMachine(this);
            PlayerInventoryWindow.Instance.Open();
            if (PlayerInventoryWindow.Instance.rightPanel != null)
            {
                PlayerInventoryWindow.Instance.rightPanel.SetTab(PanelTabType.Machine);
            }
        }
        else
        {
            Debug.LogError("[CraftingMachine] PlayerInventoryWindow not found in scene!");
        }
    }
    
    [SerializeField]
    private ConveyorItemView conveyorItemPrefab;

    [System.Serializable]
    private class CraftingSaveState
    {
        public float progress;
        public Inventory inputInventory;
        public Inventory outputInventory;
    }

    public override string GetSaveState()
    {
        CraftingSaveState state = new CraftingSaveState
        {
            progress = this.progress,
            inputInventory = this.inputInventory,
            outputInventory = this.outputInventory
        };
        return JsonUtility.ToJson(state);
    }

    public override void LoadSaveState(string stateJson)
    {
        if (!string.IsNullOrEmpty(stateJson))
        {
            CraftingSaveState state = new CraftingSaveState();
            state.inputInventory = new Inventory { slotCount = 4 };
            state.outputInventory = new Inventory { slotCount = 1 };
            JsonUtility.FromJsonOverwrite(stateJson, state);
            this.progress = state.progress;
            
            // In case of old saves, they might not have these, so null check
            if (state.inputInventory != null && state.inputInventory.slots != null)
                this.inputInventory = state.inputInventory;
            if (state.outputInventory != null && state.outputInventory.slots != null)
                this.outputInventory = state.outputInventory;
        }
    }

    public override void Tick()
    {
        ProcessRecipe();
        TryOutput();
    }

    public override bool TryReceiveItem(ConveyorItem item, Port receivingPort)
    {
        if (receivingPort == null || !receivingPort.IsInput) return false;

        if (availableRecipes == null || availableRecipes.Count == 0) 
        {
            GameLogger.Log(LogChannel.Crafting, $"[{gameObject.name}] no recipes available.", gameObject);
            return false;
        }

        // Accept item if it is an input for ANY available recipe
        bool isInputItem = availableRecipes.Any(r => r.Inputs.Any(input => input.type == item.Type));
        if (!isInputItem) 
        {
            GameLogger.Log(LogChannel.Crafting, $"[{gameObject.name}] item {item.Type} is not an input for any recipe.", gameObject);
            return false;
        }
        
        if (inputInventory.AddItem(item.Type))
        {
            GameLogger.Log(LogChannel.Crafting, $"[{gameObject.name}] accepted item {item.Type}.", gameObject);
            if (item.View != null) Destroy(item.View.gameObject);
            return true;
        }
        
        GameLogger.Log(LogChannel.Crafting, $"[{gameObject.name}] inventory full for {item.Type}.", gameObject);
        return false;
    }

    private void ProcessRecipe()
    {
        if (availableRecipes == null || availableRecipes.Count == 0) return;

        // If we don't have an active recipe, or the active recipe is no longer valid, find a new one
        if (activeRecipe == null || !inputInventory.ContainsItems(activeRecipe.Inputs) || !outputInventory.CanAddItems(activeRecipe.Outputs))
        {
            activeRecipe = null;
            progress = 0f;
            foreach (var recipe in availableRecipes)
            {
                if (inputInventory.ContainsItems(recipe.Inputs) && outputInventory.CanAddItems(recipe.Outputs))
                {
                    activeRecipe = recipe;
                    break;
                }
            }
        }

        // If we still don't have an active recipe, return
        if (activeRecipe == null) return;

        // Process the recipe
        progress += FactoryTickManager.Instance.TickRate;

        if (progress >= activeRecipe.ProcessTime)
        {
            // Complete the recipe
            if (inputInventory.RemoveItems(activeRecipe.Inputs))
            {
                outputInventory.AddItems(activeRecipe.Outputs);
                GameLogger.Log(LogChannel.Crafting, $"[{gameObject.name}] Crafted {activeRecipe.RecipeName}", gameObject);
                
                // Reset progress but keep the recipe active if we can still craft it
                progress = 0f;
                if (!inputInventory.ContainsItems(activeRecipe.Inputs) || !outputInventory.CanAddItems(activeRecipe.Outputs))
                {
                    activeRecipe = null;
                }
            }
            else
            {
                // Should not happen as we checked CanRemoveItems earlier, but reset if it does
                progress = 0f;
                activeRecipe = null;
            }
        }
    }

    private void TryOutput()
    {
        if (outputInventory.CurrentTotalAmount <= 0) return;

        Port outPort = Ports.Find(p => p.IsOutput && p.ConnectedBlock != null);
        if (outPort == null) return;

        ItemType typeToOutput = ItemType.Default;
        foreach (var slot in outputInventory.slots)
        {
            if (!slot.IsEmpty)
            {
                typeToOutput = slot.type;
                break;
            }
        }

        if (typeToOutput == ItemType.Default) return;

        ConveyorItem item = new ConveyorItem();
        item.Type = typeToOutput;

        if (outPort.ConnectedBlock.TryReceiveItem(item, outPort.ConnectedPort))
        {
            ConveyorItemView itemView = null;
            if (conveyorItemPrefab != null)
            {
                itemView = Instantiate(conveyorItemPrefab, transform.position, Quaternion.identity);
            }
            else
            {
                GameObject go = new GameObject("ConveyorItem");
                go.transform.position = transform.position;
                float scale = ResourcesManager.instance.getResourceScale(typeToOutput);
                go.transform.localScale = new Vector3(scale, scale, 1f);
                itemView = go.AddComponent<ConveyorItemView>();
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = 32767;
            }

            var sr = itemView.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) 
            {
                sr.sprite = ResourcesManager.instance.getResourceSprite(typeToOutput);
                sr.sortingOrder = 32767;
            }
            item.View = itemView;

            outputInventory.RemoveItem(typeToOutput);
        }
    }
}
