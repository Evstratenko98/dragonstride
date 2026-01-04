public interface ITurnController
{
    TurnState State { get; }

    int StepsAvailable { get; }
    int StepsRemaining { get; }

    void StartTurn(ICharacterInstance character);
    void RollDice();
    void StartMovement();
    void RegisterStep();     // уменьшает StepsRemaining
    void StartInteractions();
    void EndTurn();
}
