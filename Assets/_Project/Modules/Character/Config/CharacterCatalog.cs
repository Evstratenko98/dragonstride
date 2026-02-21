using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterCatalog", menuName = "Configs/Character Catalog")]
public sealed class CharacterCatalog : ScriptableObject
{
    [SerializeField] private List<CharacterDefinition> characters = new();

    private readonly Dictionary<string, CharacterDefinition> _cache = new(StringComparer.OrdinalIgnoreCase);
    private bool _isCacheBuilt;

    public IReadOnlyList<CharacterDefinition> Characters => characters;

    public bool HasAny => characters != null && characters.Count > 0;

    public bool TryGetById(string id, out CharacterDefinition definition)
    {
        definition = null;
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        EnsureCache();
        return _cache.TryGetValue(id.Trim(), out definition);
    }

    public CharacterDefinition GetByIndex(int index)
    {
        if (characters == null || index < 0 || index >= characters.Count)
        {
            return null;
        }

        return characters[index];
    }

    public CharacterDefinition GetFirstOrDefault()
    {
        if (characters == null)
        {
            return null;
        }

        for (int i = 0; i < characters.Count; i++)
        {
            CharacterDefinition character = characters[i];
            if (character != null && character.HasValidId)
            {
                return character;
            }
        }

        return null;
    }

    private void OnValidate()
    {
        _isCacheBuilt = false;
    }

    private void EnsureCache()
    {
        if (_isCacheBuilt)
        {
            return;
        }

        _cache.Clear();

        if (characters != null)
        {
            for (int i = 0; i < characters.Count; i++)
            {
                CharacterDefinition character = characters[i];
                if (character == null || !character.HasValidId)
                {
                    continue;
                }

                _cache[character.Id] = character;
            }
        }

        _isCacheBuilt = true;
    }
}
