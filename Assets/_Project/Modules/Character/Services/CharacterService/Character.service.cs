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
    private readonly CharacterCellLayoutService _layoutService;

    public CharacterService(CharacterFactory factory, CharacterCellLayoutService layoutService)
    {
        _factory = factory;
        _layoutService = layoutService;
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
        UpdateCellLayout(startCell);
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
        UpdateCellLayout(currentCell);
        UpdateCellLayout(neighborCell);
    }

    public void RemoveAllCharacters()
    {
        foreach (var character in _characters)
        {
            character.Destroy();
        }

        _characters.Clear();
    }

    private void UpdateCellLayout(CellModel cell)
    {
        if (cell == null) return;

        List<CharacterInstance> occupants = _characters
            .Where(character => character?.Model?.CurrentCell == cell)
            .ToList();

        _layoutService.ApplyLayout(cell, occupants);
    }
}
