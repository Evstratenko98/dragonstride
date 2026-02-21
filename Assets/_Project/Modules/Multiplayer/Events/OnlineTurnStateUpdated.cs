public readonly struct OnlineTurnStateUpdated
{
    public int CurrentActorId { get; }
    public string OwnerPlayerId { get; }
    public TurnState TurnState { get; }
    public int StepsTotal { get; }
    public int StepsRemaining { get; }
    public bool IsLocalTurn { get; }

    public OnlineTurnStateUpdated(
        int currentActorId,
        string ownerPlayerId,
        TurnState turnState,
        int stepsTotal,
        int stepsRemaining,
        bool isLocalTurn)
    {
        CurrentActorId = currentActorId;
        OwnerPlayerId = ownerPlayerId;
        TurnState = turnState;
        StepsTotal = stepsTotal;
        StepsRemaining = stepsRemaining;
        IsLocalTurn = isLocalTurn;
    }
}
