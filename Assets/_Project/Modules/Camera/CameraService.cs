public class CameraService : ICameraService
{
    public ICharacterInstance CurrentTarget { get; private set; }

    public void SetTarget(ICharacterInstance character)
    {
        CurrentTarget = character;
    }
}
