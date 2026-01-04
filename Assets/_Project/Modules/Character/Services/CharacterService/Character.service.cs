using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CharacterService : ICharacterService
{
    private List<ICharacterInstance> _characters = new List<ICharacterInstance>();
    public IReadOnlyList<ICharacterInstance> AllCharacters => _characters;

    //TODO: удалить, когда станет понятно, что работает отлично без этого метода
    public bool IsMoving => _characters.Any(c => c.IsMoving);
    private readonly ICharacterFactory _factory;

    public CharacterService(ICharacterFactory factory)
    {
        _factory = factory;
    }

    public ICharacterInstance CreateCharacter(ICellModel startCell, string name, int prefabIndex, ICharacterClass characterClass)
    {
        ICharacterInstance character = _factory.Create(name, prefabIndex, characterClass);
        
        character.Spawn(startCell);

        _characters.Add(character);
        return character;
    }

    public async Task TryMove(ICharacterInstance characterInstance, Vector2Int dir)
    {  
        if(characterInstance == null || characterInstance.Model == null) return;

        ICellModel currentCell = characterInstance.Model.CurrentCell;
        if (currentCell == null) return;

        ICellModel neighborCell = currentCell.GetNeighbor(dir);
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
