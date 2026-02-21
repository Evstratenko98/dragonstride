using System.Collections.Generic;

public interface IMatchSetupContextService
{
    bool HasPreparedRoster { get; }
    bool IsOnlineMatch { get; }
    int MatchSeed { get; }

    IReadOnlyList<CharacterSpawnRequest> GetSpawnRequests();
    void SetRoster(IReadOnlyList<CharacterSpawnRequest> requests, bool isOnlineMatch, int matchSeed = 0);
    void ClearRoster();
    void Clear();
}
