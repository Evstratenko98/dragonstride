using System.Threading;
using System.Threading.Tasks;

public interface ICharacterDraftService
{
    bool IsOperationInProgress { get; }

    Task<MultiplayerOperationResult<CharacterDraftSnapshot>> GetSnapshotAsync(
        CancellationToken cancellationToken = default);

    Task<MultiplayerOperationResult<CharacterDraftSnapshot>> SetPhaseAsync(
        string phase,
        CancellationToken cancellationToken = default);

    Task<MultiplayerOperationResult<CharacterDraftSnapshot>> SubmitSelectionAsync(
        string characterId,
        string characterName,
        bool confirmed,
        CancellationToken cancellationToken = default);

    Task<MultiplayerOperationResult<CharacterDraftSnapshot>> ClearSelectionAsync(
        CancellationToken cancellationToken = default);
}
