using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public sealed class GameCommandGatewayFacade : IGameCommandGateway
{
    private readonly MpsGameCommandGateway _gateway;

    public GameCommandGatewayFacade(MpsGameCommandGateway gateway)
    {
        _gateway = gateway;
    }

    public Task<CommandSubmitResult> SubmitMoveAsync(Vector2Int direction, CancellationToken ct = default)
    {
        return _gateway.SubmitMoveAsync(direction, ct);
    }

    public Task<CommandSubmitResult> SubmitAttackAsync(int targetActorId, CancellationToken ct = default)
    {
        return _gateway.SubmitAttackAsync(targetActorId, ct);
    }

    public Task<CommandSubmitResult> SubmitOpenCellAsync(CancellationToken ct = default)
    {
        return _gateway.SubmitOpenCellAsync(ct);
    }

    public Task<CommandSubmitResult> SubmitEndTurnAsync(CancellationToken ct = default)
    {
        return _gateway.SubmitEndTurnAsync(ct);
    }

    public Task<CommandSubmitResult> SubmitTakeLootAsync(CancellationToken ct = default)
    {
        return _gateway.SubmitTakeLootAsync(ct);
    }
}
