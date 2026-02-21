using System.Collections.Generic;

public sealed class MatchSetupContextService : IMatchSetupContextService
{
    private readonly List<CharacterSpawnRequest> _requests = new();

    public bool HasPreparedRoster => _requests.Count > 0;
    public bool IsOnlineMatch { get; private set; }

    public IReadOnlyList<CharacterSpawnRequest> GetSpawnRequests()
    {
        return _requests;
    }

    public void SetRoster(IReadOnlyList<CharacterSpawnRequest> requests, bool isOnlineMatch)
    {
        _requests.Clear();

        if (requests != null)
        {
            for (int i = 0; i < requests.Count; i++)
            {
                _requests.Add(requests[i]);
            }
        }

        IsOnlineMatch = isOnlineMatch;
    }

    public void Clear()
    {
        _requests.Clear();
        IsOnlineMatch = false;
    }
}
