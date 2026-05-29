public interface IItemReceiver
{
    bool TryReceiveItem(ConveyorItem item);
}
public interface IItemProvider
{
    ConveyorItem TryExtractItem();
}