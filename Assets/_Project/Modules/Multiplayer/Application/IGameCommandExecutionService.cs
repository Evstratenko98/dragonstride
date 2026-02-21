using System.Threading;
using System.Threading.Tasks;

public interface IGameCommandExecutionService
{
    Task<CommandSubmitResult> ExecuteAsync(GameCommandEnvelope command, CancellationToken cancellationToken = default);
}
