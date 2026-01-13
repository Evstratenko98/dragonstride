public class CameraService
{
    public CharacterInstance CurrentTarget { get; private set; }

    public void SetTarget(CharacterInstance character)
    {
        CurrentTarget = character;
    }
}
