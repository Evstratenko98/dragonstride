using System.Threading;
using System.Threading.Tasks;

public interface IMultiplayerBootstrapService
{
    Task<MultiplayerBootstrapResult> InitializeAsync(CancellationToken cancellationToken = default);
}
