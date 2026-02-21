using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public interface IGameCommandGateway
{
    Task<CommandSubmitResult> SubmitMoveAsync(Vector2Int direction, CancellationToken ct = default);
    Task<CommandSubmitResult> SubmitAttackAsync(int targetActorId, CancellationToken ct = default);
    Task<CommandSubmitResult> SubmitOpenCellAsync(CancellationToken ct = default);
    Task<CommandSubmitResult> SubmitEndTurnAsync(CancellationToken ct = default);
    Task<CommandSubmitResult> SubmitTakeLootAsync(CancellationToken ct = default);
}
