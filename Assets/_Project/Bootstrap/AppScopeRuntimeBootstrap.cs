using UnityEngine;

public static class AppScopeRuntimeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureAppScopeExists()
    {
#if UNITY_2022_1_OR_NEWER
        var existingScope = Object.FindAnyObjectByType<AppScope>(FindObjectsInactive.Include);
#else
        var existingScope = Object.FindObjectOfType<AppScope>();
#endif

        if (existingScope == null && AppScope.Instance != null)
        {
            existingScope = AppScope.Instance;
        }

        if (existingScope != null)
        {
            return;
        }

        var appScopeObject = new GameObject("AppScope");
        appScopeObject.AddComponent<AppScope>();
    }
}
