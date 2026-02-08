public abstract class Behavior
{
    public abstract void ExecuteTurn(
        EnemyInstance enemy,
        IRandomSource randomSource,
        TurnFlow turnFlow,
        CharacterRoster characterRoster,
        CrownOwnershipService crownOwnershipService
    );
}
