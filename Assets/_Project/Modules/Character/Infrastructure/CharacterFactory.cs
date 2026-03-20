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

    public CharacterInstance Create(Character model, string name, int prefabIndex)
    {
        if (_prefabs == null || _prefabs.Length == 0)
        {
            Debug.LogError("[CharacterFactory] Character prefabs are not configured.");
            return null;
        }

        int normalizedPrefabIndex = Mathf.Abs(prefabIndex) % _prefabs.Length;
        CharacterView prefab = _prefabs[normalizedPrefabIndex];
        if (prefab == null)
        {
            Debug.LogError($"[CharacterFactory] Character prefab at index {normalizedPrefabIndex} is not assigned.");
            return null;
        }

        CharacterInstance instance = new CharacterInstance(_config, model, prefab, name, _eventBus, _fieldRootService);

        return instance;
    }
}
