using UnityEngine;

public class CharacterLifecycleService
{
    private readonly ConfigScriptableObject _config;
    private readonly ItemFactory _itemFactory;
    private readonly CharacterFactory _characterFactory;
    private readonly FieldState _fieldState;

    public CharacterLifecycleService(
        ConfigScriptableObject config,
        ItemFactory itemFactory,
        CharacterFactory characterFactory,
        FieldState fieldState)
    {
        _config = config;
        _itemFactory = itemFactory;
        _characterFactory = characterFactory;
        _fieldState = fieldState;
    }

    public CharacterInstance CreateCharacter(Cell startCell, string name, int prefabIndex, CharacterClass characterClass)
    {
        if (startCell == null)
        {
            Debug.LogError("[CharacterLifecycleService] Start cell is not available. Character will not be created.");
            return null;
        }

        Character model = CreateModel(name, characterClass, withStarterItems: true);
        CharacterInstance character = _characterFactory.Create(model, name, prefabIndex);
        if (character == null)
        {
            return null;
        }

        character.Spawn(startCell);
        return character;
    }

    public bool TryRebirth(CharacterInstance character)
    {
        if (character == null || character.Model == null)
        {
            return false;
        }

        Cell startCell = _fieldState.StartCell;
        if (startCell == null)
        {
            Debug.LogError("[CharacterLifecycleService] Start cell is not available. Rebirth is impossible.");
            return false;
        }

        CharacterClass characterClass = character.Model.Class;
        if (characterClass == null)
        {
            Debug.LogError($"[CharacterLifecycleService] Character class is not configured for '{character.Name}'.");
            return false;
        }

        Character rebornModel = CreateModel(character.Name, characterClass, withStarterItems: false);
        character.Respawn(rebornModel, startCell);
        return true;
    }

    private Character CreateModel(string name, CharacterClass characterClass, bool withStarterItems)
    {
        Character model = new Character(characterClass);
        model.SetName(name);
        model.InitializeInventory(_config.INVENTORY_CAPACITY);
        model.InitializeEquipment();

        characterClass?.Apply(model);

        if (withStarterItems)
        {
            Item sword = _itemFactory.CreateItem("sword_common");
            Item healthFlaskSmall = _itemFactory.CreateItem("health_flask_small");

            if (sword?.Definition != null)
            {
                model.Inventory.AddItem(sword.Definition);
            }

            if (healthFlaskSmall?.Definition != null)
            {
                model.Inventory.AddItem(healthFlaskSmall.Definition);
            }
        }

        return model;
    }
}
