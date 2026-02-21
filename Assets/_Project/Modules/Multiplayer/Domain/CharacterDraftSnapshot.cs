using System.Collections.Generic;

public readonly struct CharacterDraftPlayerSnapshot
{
    public string PlayerId { get; }
    public bool IsHost { get; }
    public string CharacterId { get; }
    public string CharacterName { get; }
    public bool IsConfirmed { get; }
    public long UpdatedAtUnixMs { get; }

    public CharacterDraftPlayerSnapshot(
        string playerId,
        bool isHost,
        string characterId,
        string characterName,
        bool isConfirmed,
        long updatedAtUnixMs)
    {
        PlayerId = playerId;
        IsHost = isHost;
        CharacterId = characterId;
        CharacterName = characterName;
        IsConfirmed = isConfirmed;
        UpdatedAtUnixMs = updatedAtUnixMs;
    }
}

public readonly struct CharacterDraftSnapshot
{
    public string SessionId { get; }
    public string Phase { get; }
    public bool IsHost { get; }
    public string LocalPlayerId { get; }
    public int MatchSeed { get; }
    public string SelectedCharacterId { get; }
    public string SelectedCharacterName { get; }
    public bool IsLocalConfirmed { get; }
    public bool AreAllConfirmed { get; }
    public bool HasUniqueCharacterPicks { get; }
    public bool HasUniqueNames { get; }
    public bool HasConflicts { get; }
    public IReadOnlyList<CharacterDraftPlayerSnapshot> Players { get; }

    public CharacterDraftSnapshot(
        string sessionId,
        string phase,
        bool isHost,
        string localPlayerId,
        int matchSeed,
        string selectedCharacterId,
        string selectedCharacterName,
        bool isLocalConfirmed,
        bool areAllConfirmed,
        bool hasUniqueCharacterPicks,
        bool hasUniqueNames,
        IReadOnlyList<CharacterDraftPlayerSnapshot> players)
    {
        SessionId = sessionId;
        Phase = phase;
        IsHost = isHost;
        LocalPlayerId = localPlayerId;
        MatchSeed = matchSeed;
        SelectedCharacterId = selectedCharacterId;
        SelectedCharacterName = selectedCharacterName;
        IsLocalConfirmed = isLocalConfirmed;
        AreAllConfirmed = areAllConfirmed;
        HasUniqueCharacterPicks = hasUniqueCharacterPicks;
        HasUniqueNames = hasUniqueNames;
        HasConflicts = !hasUniqueCharacterPicks || !hasUniqueNames;
        Players = players;
    }
}
