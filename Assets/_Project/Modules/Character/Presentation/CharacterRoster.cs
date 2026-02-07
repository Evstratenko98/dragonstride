using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CharacterRoster
{
    private List<CharacterInstance> _characters = new List<CharacterInstance>();
    private readonly List<ICellLayoutOccupant> _extraLayoutOccupants = new List<ICellLayoutOccupant>();
    public IReadOnlyList<CharacterInstance> AllCharacters => _characters;

    public bool IsMoving => _characters.Any(c => c.IsMoving);
    private readonly CharacterLifecycleService _lifecycleService;
    private readonly CharacterLayout _layoutService;

    public CharacterRoster(CharacterLifecycleService lifecycleService, CharacterLayout layoutService)
    {
        _lifecycleService = lifecycleService;
        _layoutService = layoutService;
    }

    public CharacterInstance CreateCharacter(Cell startCell, string name, int prefabIndex, CharacterClass characterClass)
    {
        CharacterInstance character = _lifecycleService.CreateCharacter(startCell, name, prefabIndex, characterClass);
        if (character == null)
        {
            return null;
        }

        _characters.Add(character);
        UpdateCellLayout(startCell);
        return character;
    }

    public bool TryRebirthCharacter(CharacterInstance character)
    {
        if (character?.Model == null)
        {
            return false;
        }

        Cell previousCell = character.Model.CurrentCell;
        bool rebirthSuccess = _lifecycleService.TryRebirth(character);
        if (!rebirthSuccess)
        {
            return false;
        }

        UpdateCellLayout(previousCell);
        UpdateCellLayout(character.Model.CurrentCell);
        return true;
    }

    public async Task TryMove(CharacterInstance characterInstance, Vector2Int dir)
    {  
        if(characterInstance == null || characterInstance.Model == null) return;

        Cell currentCell = characterInstance.Model.CurrentCell;
        if (currentCell == null) return;

        Cell neighborCell = currentCell.GetNeighbor(dir.x, dir.y);
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

    public void RegisterLayoutOccupant(ICellLayoutOccupant occupant)
    {
        if (occupant == null || _extraLayoutOccupants.Contains(occupant))
        {
            return;
        }

        _extraLayoutOccupants.Add(occupant);
        UpdateCellLayout(occupant.Entity?.CurrentCell);
    }

    public void UnregisterLayoutOccupant(ICellLayoutOccupant occupant)
    {
        if (occupant == null)
        {
            return;
        }

        Cell currentCell = occupant.Entity?.CurrentCell;
        if (_extraLayoutOccupants.Remove(occupant))
        {
            UpdateCellLayout(currentCell);
        }
    }

    public void UpdateEntityLayout(Entity entity, Cell previousCell = null)
    {
        UpdateCellLayout(previousCell);
        UpdateCellLayout(entity?.CurrentCell);
    }

    private void UpdateCellLayout(Cell cell)
    {
        if (cell == null) return;

        List<ICellLayoutOccupant> occupants = _characters
            .Cast<ICellLayoutOccupant>()
            .Concat(_extraLayoutOccupants)
            .Where(occupant => occupant?.Entity?.CurrentCell == cell)
            .ToList();

        _layoutService.ApplyLayout(cell, occupants);
    }
}
