public interface ICraftingProvider
{
    Inventory InputInventory { get; }
    Inventory OutputInventory { get; }
    float ProgressPercentage { get; }
    string RecipeName { get; }
}
