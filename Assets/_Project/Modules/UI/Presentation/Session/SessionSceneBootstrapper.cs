using UnityEngine;

public static class SessionSceneBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // Phase 1: session screens are now authored as static scene UI.
    }
}
