using UnityEngine;

public class CharacterLifecycleService
{
    private readonly ConfigScriptableObject _config;
    private readonly CharacterCatalog _characterCatalog;
    private readonly ItemFactory _itemFactory;
    private readonly CharacterFactory _characterFactory;
    private readonly FieldState _fieldState;

    public CharacterLifecycleService(
        ConfigScriptableObject config,
        CharacterCatalog characterCatalog,
        ItemFactory itemFactory,
        CharacterFactory characterFactory,
        FieldState fieldState)
    {
        _config = config;
        _characterCatalog = characterCatalog;
        _itemFactory = itemFactory;
        _characterFactory = characterFactory;
        _fieldState = fieldState;
    }

    public CharacterInstance CreateCharacter(Cell startCell, CharacterSpawnRequest request)
    {
        if (startCell == null)
        {
            Debug.LogError("[CharacterLifecycleService] Start cell is not available. Character will not be created.");
            return null;
        }

        CharacterDefinition definition = ResolveCharacterDefinition(request.CharacterId);
        if (definition == null)
        {
            Debug.LogError("[CharacterLifecycleService] Character definition was not resolved. Character will not be created.");
            return null;
        }

        string name = ResolveCharacterName(request.CharacterName, definition);
        Character model = CreateModel(name, definition, withStarterItems: true);
        CharacterInstance character = _characterFactory.Create(model, request.PlayerId, name, definition.PrefabIndex);
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

        CharacterDefinition definition = character.Model.Definition;
        if (definition == null)
        {
            Debug.LogError($"[CharacterLifecycleService] Character definition is not configured for '{character.Name}'.");
            return false;
        }

        Character rebornModel = CreateModel(character.Name, definition, withStarterItems: false);
        character.Respawn(rebornModel, startCell);
        return true;
    }

    private Character CreateModel(string name, CharacterDefinition definition, bool withStarterItems)
    {
        Character model = new Character(definition);
        model.SetName(name);
        model.InitializeInventory(_config.INVENTORY_CAPACITY);
        model.InitializeEquipment();

        definition?.ApplyTo(model);

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

    private CharacterDefinition ResolveCharacterDefinition(string characterId)
    {
        if (_characterCatalog != null &&
            !string.IsNullOrWhiteSpace(characterId) &&
            _characterCatalog.TryGetById(characterId, out CharacterDefinition exact))
        {
            return exact;
        }

        CharacterDefinition fallback = _characterCatalog?.GetFirstOrDefault();
        if (fallback != null)
        {
            if (!string.IsNullOrWhiteSpace(characterId))
            {
                Debug.LogWarning($"[CharacterLifecycleService] Character id '{characterId}' not found in catalog. Fallback '{fallback.Id}' is used.");
            }

            return fallback;
        }

        return null;
    }

    private static string ResolveCharacterName(string requestedName, CharacterDefinition definition)
    {
        string trimmed = string.IsNullOrWhiteSpace(requestedName) ? string.Empty : requestedName.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            return trimmed;
        }

        return definition == null || string.IsNullOrWhiteSpace(definition.DisplayName)
            ? "Hero"
            : definition.DisplayName;
    }
}
