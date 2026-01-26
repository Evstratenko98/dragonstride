public class CameraFollowToggledMessage
{
    public bool IsEnabled { get; }

    public CameraFollowToggledMessage(bool isEnabled)
    {
        IsEnabled = isEnabled;
    }
}
