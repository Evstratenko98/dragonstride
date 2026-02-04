using System.Collections.Generic;

public readonly struct CharacterRosterUpdated
{
    public IReadOnlyList<CharacterInstance> Characters { get; }

    public CharacterRosterUpdated(IReadOnlyList<CharacterInstance> characters)
    {
        Characters = characters;
    }
}
