public interface IMatchClientTurnStateService
{
    bool HasInitialState { get; }
    bool IsLocalTurn { get; }
    TurnState CurrentTurnState { get; }
    string CurrentOwnerPlayerId { get; }

    void UpdateFromSnapshot(MatchStateSnapshot snapshot, string localPlayerId);
    void Reset();
}
