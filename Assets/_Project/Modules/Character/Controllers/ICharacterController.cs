using System.Collections.Generic;
using VContainer.Unity;

public interface ICharacterController : IPostInitializable, ITickable, System.IDisposable
{
    IReadOnlyList<ICharacterInstance> SpawnCharacters(ICellModel startCell);
    void Reset();
}
