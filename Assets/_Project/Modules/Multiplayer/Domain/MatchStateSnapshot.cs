using System.Collections.Generic;

public readonly struct MatchStateSnapshot
{
    public long Sequence { get; }
    public GameState GameState { get; }
    public TurnState TurnState { get; }
    public int CurrentActorId { get; }
    public int StepsTotal { get; }
    public int StepsRemaining { get; }
    public bool IsPaused { get; }
    public string PauseReason { get; }
    public string Phase { get; }
    public IReadOnlyList<ActorStateSnapshot> Actors { get; }
    public IReadOnlyList<OpenedCellSnapshot> OpenedCells { get; }

    public MatchStateSnapshot(
        long sequence,
        GameState gameState,
        TurnState turnState,
        int currentActorId,
        int stepsTotal,
        int stepsRemaining,
        bool isPaused,
        string pauseReason,
        string phase,
        IReadOnlyList<ActorStateSnapshot> actors,
        IReadOnlyList<OpenedCellSnapshot> openedCells)
    {
        Sequence = sequence;
        GameState = gameState;
        TurnState = turnState;
        CurrentActorId = currentActorId;
        StepsTotal = stepsTotal;
        StepsRemaining = stepsRemaining;
        IsPaused = isPaused;
        PauseReason = pauseReason;
        Phase = phase;
        Actors = actors;
        OpenedCells = openedCells;
    }
}
