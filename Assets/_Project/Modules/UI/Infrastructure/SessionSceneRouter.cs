using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SessionSceneRouter : ISessionSceneRouter
{
    public bool IsTransitionInProgress => _isTransitionInProgress;

    private bool _isTransitionInProgress;

    public Task<bool> LoadMainMenuAsync()
    {
        return LoadSceneAsync(SessionSceneNames.MainMenu);
    }

    public Task<bool> LoadLobbyAsync()
    {
        return LoadSceneAsync(SessionSceneNames.Lobby);
    }

    public Task<bool> LoadGameSceneAsync()
    {
        return LoadSceneAsync(SessionSceneNames.GameScene);
    }

    public Task<bool> LoadGameOverAsync()
    {
        return LoadSceneAsync(SessionSceneNames.GameOver);
    }

    private async Task<bool> LoadSceneAsync(string sceneName)
    {
        if (_isTransitionInProgress)
        {
            Debug.LogWarning($"[SessionSceneRouter] Ignored transition to '{sceneName}' because another transition is in progress.");
            return false;
        }

        if (!IsSceneInBuildSettings(sceneName))
        {
            Debug.LogError($"[SessionSceneRouter] Scene '{sceneName}' is missing in Build Settings.");
            return false;
        }

        if (SceneManager.GetActiveScene().name == sceneName)
        {
            return true;
        }

        _isTransitionInProgress = true;
        try
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                Debug.LogError($"[SessionSceneRouter] Failed to start scene loading for '{sceneName}'.");
                return false;
            }

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[SessionSceneRouter] Scene transition to '{sceneName}' failed: {exception}");
            return false;
        }
        finally
        {
            _isTransitionInProgress = false;
        }
    }

    private static bool IsSceneInBuildSettings(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            if (string.Equals(Path.GetFileNameWithoutExtension(scenePath), sceneName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
