public interface IItemReceiver
{
    bool TryReceiveItem(ConveyorItem item);
}
public interface IItemProvider
{
    ConveyorItem TryExtractItem();
}
public interface IInteractable
{
    void Interact(PlayerInteractor player);
}
public interface IBuildable
{
    void RebuildConnections();
}