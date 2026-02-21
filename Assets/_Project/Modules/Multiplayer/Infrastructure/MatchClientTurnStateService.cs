using System;

public sealed class MatchClientTurnStateService : IMatchClientTurnStateService
{
    public bool HasInitialState { get; private set; }
    public bool IsLocalTurn { get; private set; }
    public TurnState CurrentTurnState { get; private set; } = TurnState.None;
    public string CurrentOwnerPlayerId { get; private set; } = string.Empty;

    public void UpdateFromSnapshot(MatchStateSnapshot snapshot, string localPlayerId)
    {
        CurrentTurnState = snapshot.TurnState;
        CurrentOwnerPlayerId = ResolveOwnerPlayerId(snapshot);
        IsLocalTurn = IsTurnBoundActionState(snapshot.TurnState) &&
                      !string.IsNullOrWhiteSpace(localPlayerId) &&
                      string.Equals(CurrentOwnerPlayerId, localPlayerId, StringComparison.Ordinal);
        HasInitialState = true;
    }

    public void Reset()
    {
        HasInitialState = false;
        IsLocalTurn = false;
        CurrentTurnState = TurnState.None;
        CurrentOwnerPlayerId = string.Empty;
    }

    private static string ResolveOwnerPlayerId(MatchStateSnapshot snapshot)
    {
        if (snapshot.Actors == null || snapshot.CurrentActorId <= 0)
        {
            return string.Empty;
        }

        for (int i = 0; i < snapshot.Actors.Count; i++)
        {
            ActorStateSnapshot actor = snapshot.Actors[i];
            if (actor.ActorId != snapshot.CurrentActorId)
            {
                continue;
            }

            return actor.OwnerPlayerId ?? string.Empty;
        }

        return string.Empty;
    }

    private static bool IsTurnBoundActionState(TurnState state)
    {
        return state == TurnState.ActionSelection ||
               state == TurnState.Movement ||
               state == TurnState.Attack ||
               state == TurnState.OpenCell ||
               state == TurnState.Trade;
    }
}
