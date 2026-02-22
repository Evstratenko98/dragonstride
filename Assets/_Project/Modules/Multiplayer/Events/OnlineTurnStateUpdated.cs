public readonly struct OnlineTurnStateUpdated
{
    public int CurrentActorId { get; }
    public string OwnerPlayerId { get; }
    public string CurrentActorDisplayName { get; }
    public TurnState TurnState { get; }
    public int StepsTotal { get; }
    public int StepsRemaining { get; }
    public bool IsLocalTurn { get; }

    public OnlineTurnStateUpdated(
        int currentActorId,
        string ownerPlayerId,
        string currentActorDisplayName,
        TurnState turnState,
        int stepsTotal,
        int stepsRemaining,
        bool isLocalTurn)
    {
        CurrentActorId = currentActorId;
        OwnerPlayerId = ownerPlayerId;
        CurrentActorDisplayName = currentActorDisplayName ?? string.Empty;
        TurnState = turnState;
        StepsTotal = stepsTotal;
        StepsRemaining = stepsRemaining;
        IsLocalTurn = isLocalTurn;
    }
}
