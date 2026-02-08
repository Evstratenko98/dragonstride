public class ConsumableItemUseService
{
    public bool TryUse(CharacterInstance character, int inventorySlotIndex)
    {
        if (character?.Model == null)
        {
            return false;
        }

        return character.Model.TryUseConsumable(inventorySlotIndex);
    }

    public bool TryUse(Character character, int inventorySlotIndex)
    {
        if (character == null)
        {
            return false;
        }

        return character.TryUseConsumable(inventorySlotIndex);
    }
}
