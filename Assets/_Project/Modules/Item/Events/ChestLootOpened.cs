using System.Collections.Generic;

public readonly struct ChestLootOpened
{
    public ChestLootOpened(CharacterInstance character, IReadOnlyList<ItemDefinition> loot)
    {
        Character = character;
        Loot = loot;
    }

    public CharacterInstance Character { get; }
    public IReadOnlyList<ItemDefinition> Loot { get; }
}
