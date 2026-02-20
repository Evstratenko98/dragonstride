using UnityEngine;

[CreateAssetMenu(fileName = "MultiplayerConfig", menuName = "Configs/Multiplayer Config")]
public sealed class MultiplayerConfig : ScriptableObject
{
    [Header("General")]
    public bool EnableMultiplayer = true;
    [Min(2)] public int MaxPlayers = 4;

    [Header("Match Timers")]
    [Min(1)] public int TurnTimerSeconds = 60;
    [Min(1)] public int DisconnectGraceSeconds = 45;

    [Header("Lobby")]
    [Min(0.2f)] public float LobbyRefreshIntervalSeconds = 2f;
    public bool DefaultUniquePicks = true;
    public string DefaultRegion = "auto";

    private void OnValidate()
    {
        MaxPlayers = Mathf.Max(2, MaxPlayers);
        TurnTimerSeconds = Mathf.Max(1, TurnTimerSeconds);
        DisconnectGraceSeconds = Mathf.Max(1, DisconnectGraceSeconds);
        LobbyRefreshIntervalSeconds = Mathf.Max(0.2f, LobbyRefreshIntervalSeconds);
        DefaultRegion = string.IsNullOrWhiteSpace(DefaultRegion) ? "auto" : DefaultRegion.Trim();
    }
}
