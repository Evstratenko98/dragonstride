using System;
using VContainer.Unity;

public sealed class MpsGameCommandGatewayRunner : IStartable, IDisposable
{
    private readonly MpsGameCommandGateway _gateway;

    public MpsGameCommandGatewayRunner(MpsGameCommandGateway gateway)
    {
        _gateway = gateway;
    }

    public void Start()
    {
        _gateway?.Start();
    }

    public void Dispose()
    {
        _gateway?.Dispose();
    }
}
