using UnityEngine;

public class CharacterFactory
{
    private readonly ConfigScriptableObject _config;
    private readonly IEventBus _eventBus;
    private readonly CharacterView[] _prefabs;
    private readonly FieldRoot _fieldRootService;

    public CharacterFactory(
        ConfigScriptableObject config,
        IEventBus eventBus,
        CharacterView[] prefabs,
        FieldRoot fieldRootService)
    {
        _config = config;
        _eventBus = eventBus;
        _prefabs = prefabs;
        _fieldRootService = fieldRootService;
    }

    public CharacterInstance Create(Character model, string playerId, string name, int prefabIndex)
    {
        if (_prefabs == null || _prefabs.Length == 0)
        {
            Debug.LogError("[CharacterFactory] Character prefabs are not configured.");
            return null;
        }

        if (prefabIndex < 0 || prefabIndex >= _prefabs.Length)
        {
            Debug.LogError($"[CharacterFactory] Character prefab index {prefabIndex} is out of range (0-{_prefabs.Length - 1}).");
            return null;
        }

        CharacterView prefab = _prefabs[prefabIndex];
        if (prefab == null)
        {
            Debug.LogError($"[CharacterFactory] Character prefab at index {prefabIndex} is not assigned.");
            return null;
        }

        CharacterInstance instance = new CharacterInstance(
            _config,
            model,
            prefab,
            playerId,
            name,
            _eventBus,
            _fieldRootService);

        return instance;
    }
}
