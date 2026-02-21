public readonly struct CharacterSpawnRequest
{
    public string PlayerId { get; }
    public string CharacterId { get; }
    public string CharacterName { get; }

    public CharacterSpawnRequest(string playerId, string characterId, string characterName)
    {
        PlayerId = playerId;
        CharacterId = characterId;
        CharacterName = characterName;
    }
}
