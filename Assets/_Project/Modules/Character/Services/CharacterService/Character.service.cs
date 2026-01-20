using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CharacterService
{
    private List<CharacterInstance> _characters = new List<CharacterInstance>();
    public IReadOnlyList<CharacterInstance> AllCharacters => _characters;

    //TODO: удалить, когда станет понятно, что работает отлично без этого метода
    public bool IsMoving => _characters.Any(c => c.IsMoving);
    private readonly CharacterFactory _factory;

    public CharacterService(CharacterFactory factory)
    {
        _factory = factory;
    }

    public CharacterInstance CreateCharacter(CellModel startCell, string name, int prefabIndex, CharacterClass characterClass)
    {
        if (startCell == null)
        {
            Debug.LogError("[CharacterService] Start cell is not available. Character will not be created.");
            return null;
        }

        CharacterInstance character = _factory.Create(name, prefabIndex, characterClass);
        if (character == null)
        {
            return null;
        }

        character.Spawn(startCell);

        _characters.Add(character);
        return character;
    }

    public async Task TryMove(CharacterInstance characterInstance, Vector2Int dir)
    {  
        if(characterInstance == null || characterInstance.Model == null) return;

        CellModel currentCell = characterInstance.Model.CurrentCell;
        if (currentCell == null) return;

        CellModel neighborCell = currentCell.GetNeighbor(dir);
        if (neighborCell == null) return;

        await characterInstance.MoveTo(neighborCell);
    }

    public void RemoveAllCharacters()
    {
        foreach (var character in _characters)
        {
            character.Destroy();
        }

        _characters.Clear();
    }
}
