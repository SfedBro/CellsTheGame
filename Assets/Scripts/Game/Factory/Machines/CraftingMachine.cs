using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CraftingMachine : FactoryBlock, IInteractable, ICraftingProvider
{
    [Header("Recipe Configuration")]
    public List<RecipeData> availableRecipes = new List<RecipeData>();
    public RecipeData selectedRecipe;

    [Header("Inventory")]
    public Inventory inputInventory = new Inventory { slotCount = 4 };
    public Inventory outputInventory = new Inventory { slotCount = 1 };

    public Inventory InputInventory => inputInventory;
    public Inventory OutputInventory => outputInventory;
    public float ProgressPercentage => (activeRecipe != null && activeRecipe.ProcessTime > 0) ? (progress / activeRecipe.ProcessTime) : 0f;
    public string RecipeName => activeRecipe != null ? activeRecipe.RecipeName : "Нет рецепта";

    public List<RecipeData> AvailableRecipes => availableRecipes;
    public RecipeData SelectedRecipe => selectedRecipe;

    public void SelectRecipe(RecipeData recipe)
    {
        if (selectedRecipe != recipe)
        {
            selectedRecipe = recipe;
            activeRecipe = null;
            progress = 0f;
        }
    }

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
        public string selectedRecipeName;
    }

    public override string GetSaveState()
    {
        CraftingSaveState state = new CraftingSaveState
        {
            progress = this.progress,
            inputInventory = this.inputInventory,
            outputInventory = this.outputInventory,
            selectedRecipeName = this.selectedRecipe != null ? this.selectedRecipe.name : ""
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

            if (!string.IsNullOrEmpty(state.selectedRecipeName))
            {
                this.selectedRecipe = availableRecipes.Find(r => r != null && (r.name == state.selectedRecipeName || r.RecipeName == state.selectedRecipeName));
            }
            else
            {
                this.selectedRecipe = null;
            }
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

        if (selectedRecipe == null) 
        {
            GameLogger.Log(LogChannel.Crafting, $"[{gameObject.name}] no recipe selected to accept items.", gameObject);
            return false;
        }

        // Accept item only if it is an input for the selected recipe
        bool isInputItem = selectedRecipe.Inputs.Any(input => input.type == item.Type);
        if (!isInputItem) 
        {
            GameLogger.Log(LogChannel.Crafting, $"[{gameObject.name}] item {item.Type} is not an input for selected recipe.", gameObject);
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
        if (selectedRecipe == null)
        {
            activeRecipe = null;
            progress = 0f;
            return;
        }

        // Keep activeRecipe locked to selectedRecipe
        activeRecipe = selectedRecipe;

        // If inputs are missing or outputs are full, hold progress
        if (!inputInventory.ContainsItems(activeRecipe.Inputs) || !outputInventory.CanAddItems(activeRecipe.Outputs))
        {
            progress = 0f;
            return;
        }

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
