public interface IItemService
{
    public ItemModel CreateItem(string id);
    public ItemModel CreateRandomChestLoot();
    public void DeleteItem(ItemModel item);
    bool IsUniqueSpawned(string id);
}