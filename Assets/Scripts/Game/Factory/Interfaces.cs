public interface IItemReceiver
{
    bool TryReceiveItem(ConveyorItem item);
}
public interface IItemGiver
{
    void RebuildConnections();
}
public interface IItemProvider
{
    ConveyorItem TryExtractItem();
}
public interface IInteractable
{
    void Interact(PlayerInteractor player);
}