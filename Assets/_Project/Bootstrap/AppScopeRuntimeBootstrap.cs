using UnityEngine;

public static class AppScopeRuntimeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureAppScopeExists()
    {
#if UNITY_2022_1_OR_NEWER
        var existingScope = Object.FindAnyObjectByType<AppScope>();
#else
        var existingScope = Object.FindObjectOfType<AppScope>();
#endif
        if (existingScope != null)
        {
            return;
        }

        var appScopeObject = new GameObject("AppScope");
        appScopeObject.AddComponent<AppScope>();
    }
}
