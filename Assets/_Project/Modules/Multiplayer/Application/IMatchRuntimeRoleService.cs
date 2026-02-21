public interface IMatchRuntimeRoleService
{
    bool IsOnlineMatch { get; }
    bool IsHostAuthority { get; }
    bool IsClientReplica { get; }
    bool IsOfflineMatch { get; }
    bool CanMutateWorld { get; }
}
