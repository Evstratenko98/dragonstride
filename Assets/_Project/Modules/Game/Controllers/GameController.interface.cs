using System.Collections.Generic;

public interface IGameController
{
    GameState GameState { get; }
    GameTurnState GameTurnState { get; }

    IReadOnlyList<ICharacterInstance> Players { get; }
    ICharacterInstance CurrentPlayer { get; }

    void StartGame();
    void StartTurnCycle();
    void NextPlayer();
}
