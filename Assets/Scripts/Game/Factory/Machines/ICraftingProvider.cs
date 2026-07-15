using System.Collections.Generic;

public interface ICraftingProvider
{
    Inventory InputInventory { get; }
    Inventory OutputInventory { get; }
    float ProgressPercentage { get; }
    string RecipeName { get; }
    List<RecipeData> AvailableRecipes { get; }
    RecipeData SelectedRecipe { get; }
    void SelectRecipe(RecipeData recipe);
}
