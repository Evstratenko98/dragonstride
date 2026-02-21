using System;
using System.Collections.Generic;

public sealed class MatchStatePublisher : IMatchStatePublisher
{
    private readonly GameFlow _gameFlow;
    private readonly TurnFlow _turnFlow;
    private readonly TurnActorRegistry _turnActorRegistry;
    private readonly FieldState _fieldState;
    private readonly IActorIdentityService _actorIdentityService;
    private readonly CrownOwnershipService _crownOwnershipService;
    private readonly IMatchPauseService _matchPauseService;

    public MatchStatePublisher(
        GameFlow gameFlow,
        TurnFlow turnFlow,
        TurnActorRegistry turnActorRegistry,
        FieldState fieldState,
        IActorIdentityService actorIdentityService,
        CrownOwnershipService crownOwnershipService,
        IMatchPauseService matchPauseService)
    {
        _gameFlow = gameFlow;
        _turnFlow = turnFlow;
        _turnActorRegistry = turnActorRegistry;
        _fieldState = fieldState;
        _actorIdentityService = actorIdentityService;
        _crownOwnershipService = crownOwnershipService;
        _matchPauseService = matchPauseService;
    }

    public MatchStateSnapshot Capture(long sequence, string phase = "in_game")
    {
        var actorSnapshots = BuildActorSnapshots();
        var openedCells = BuildOpenedCellSnapshots();

        int currentActorId = 0;
        if (_turnFlow?.CurrentActor != null)
        {
            currentActorId = _actorIdentityService?.GetId(_turnFlow.CurrentActor) ?? 0;
        }

        GameState gameState = _gameFlow != null ? _gameFlow.GameState : GameState.Init;
        TurnState turnState = _turnFlow != null ? _turnFlow.State : TurnState.None;
        int stepsTotal = _turnFlow != null ? _turnFlow.StepsAvailable : 0;
        int stepsRemaining = _turnFlow != null ? _turnFlow.StepsRemaining : 0;
        bool isPaused = _matchPauseService != null && _matchPauseService.IsPaused;
        string pauseReason = isPaused && _matchPauseService != null ? _matchPauseService.Reason : string.Empty;
        string normalizedPhase = string.IsNullOrWhiteSpace(phase) ? "in_game" : phase;

        return new MatchStateSnapshot(
            sequence,
            gameState,
            turnState,
            currentActorId,
            stepsTotal,
            stepsRemaining,
            isPaused,
            pauseReason,
            normalizedPhase,
            actorSnapshots,
            openedCells);
    }

    private IReadOnlyList<ActorStateSnapshot> BuildActorSnapshots()
    {
        var result = new List<ActorStateSnapshot>();
        if (_turnActorRegistry == null)
        {
            return result;
        }

        List<ICellLayoutOccupant> actors = _turnActorRegistry.GetActiveActorsSnapshot();
        for (int i = 0; i < actors.Count; i++)
        {
            ICellLayoutOccupant actor = actors[i];
            Entity entity = actor?.Entity;
            if (entity == null)
            {
                continue;
            }

            int actorId = _actorIdentityService?.GetOrAssign(actor) ?? 0;
            if (actorId <= 0)
            {
                continue;
            }

            int cellX = entity.CurrentCell?.X ?? -1;
            int cellY = entity.CurrentCell?.Y ?? -1;
            bool hasCrown = _crownOwnershipService != null && _crownOwnershipService.HasCrown(entity);

            if (actor is CharacterInstance character)
            {
                result.Add(new ActorStateSnapshot(
                    actorId,
                    "character",
                    character.PlayerId ?? string.Empty,
                    character.CharacterDefinition?.Id ?? string.Empty,
                    string.IsNullOrWhiteSpace(character.Name) ? entity.Name : character.Name,
                    cellX,
                    cellY,
                    entity.Health,
                    entity.Level,
                    hasCrown,
                    entity.CurrentCell != null));
                continue;
            }

            string enemyType = actor is EnemyInstance enemyInstance && enemyInstance.EntityModel != null
                ? enemyInstance.EntityModel.GetType().Name
                : entity.GetType().Name;

            result.Add(new ActorStateSnapshot(
                actorId,
                "enemy",
                string.Empty,
                enemyType,
                entity.Name ?? string.Empty,
                cellX,
                cellY,
                entity.Health,
                entity.Level,
                hasCrown,
                entity.CurrentCell != null));
        }

        return result;
    }

    private IReadOnlyList<OpenedCellSnapshot> BuildOpenedCellSnapshots()
    {
        var result = new List<OpenedCellSnapshot>();
        FieldGrid field = _fieldState?.CurrentField;
        if (field == null)
        {
            return result;
        }

        foreach (Cell cell in field.GetAllCells())
        {
            if (cell == null || !cell.IsOpened)
            {
                continue;
            }

            result.Add(new OpenedCellSnapshot(cell.X, cell.Y, cell.Type.ToString()));
        }

        return result;
    }
}
