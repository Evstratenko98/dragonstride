public class CameraService
{
    public CharacterInstance CurrentTarget { get; private set; }
    public bool FollowEnabled { get; private set; } = true;

    public void SetTarget(CharacterInstance character)
    {
        CurrentTarget = character;
    }

    public void SetFollowEnabled(bool isEnabled)
    {
        FollowEnabled = isEnabled;
    }
}
