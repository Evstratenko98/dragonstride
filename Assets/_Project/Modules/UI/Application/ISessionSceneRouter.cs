using System.Threading.Tasks;

public interface ISessionSceneRouter
{
    bool IsTransitionInProgress { get; }

    Task<bool> LoadMainMenuAsync();
    Task<bool> LoadLobbyAsync();
    Task<bool> LoadGameSceneAsync();
    Task<bool> LoadGameOverAsync();
}
