public interface ICameraService
{
    void SetTarget(ICharacterInstance character);
    ICharacterInstance CurrentTarget { get; }
}