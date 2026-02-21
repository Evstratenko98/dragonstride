using System.Collections.Generic;

public interface IMatchSetupContextService
{
    bool HasPreparedRoster { get; }
    bool IsOnlineMatch { get; }

    IReadOnlyList<CharacterSpawnRequest> GetSpawnRequests();
    void SetRoster(IReadOnlyList<CharacterSpawnRequest> requests, bool isOnlineMatch);
    void Clear();
}
