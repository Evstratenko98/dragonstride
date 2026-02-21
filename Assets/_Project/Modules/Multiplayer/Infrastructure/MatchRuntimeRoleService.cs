public sealed class MatchRuntimeRoleService : IMatchRuntimeRoleService
{
    private readonly IMatchSetupContextService _matchSetupContextService;
    private readonly IMatchNetworkService _matchNetworkService;

    public bool IsOnlineMatch
    {
        get
        {
            bool contextOnline = _matchSetupContextService != null && _matchSetupContextService.IsOnlineMatch;
            if (contextOnline)
            {
                return true;
            }

            return _matchNetworkService != null && (_matchNetworkService.IsHost || _matchNetworkService.IsClient);
        }
    }

    public bool IsHostAuthority
    {
        get
        {
            if (!IsOnlineMatch)
            {
                return true;
            }

            return _matchNetworkService != null && _matchNetworkService.IsHost;
        }
    }

    public bool IsClientReplica => IsOnlineMatch && !IsHostAuthority;

    public bool IsOfflineMatch => !IsOnlineMatch;

    public bool CanMutateWorld => IsHostAuthority;

    public MatchRuntimeRoleService(
        IMatchSetupContextService matchSetupContextService,
        IMatchNetworkService matchNetworkService)
    {
        _matchSetupContextService = matchSetupContextService;
        _matchNetworkService = matchNetworkService;
    }
}
