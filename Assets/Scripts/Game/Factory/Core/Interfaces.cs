using System.Collections.Generic;

public interface IInteractable
{
    void Interact(PlayerInteractor player);
}
public interface IInventoryProvider
{
    Inventory Inventory { get; }
}
