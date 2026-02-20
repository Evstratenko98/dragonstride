public readonly struct MultiplayerBootstrapFailed
{
    public string ErrorCode { get; }
    public string ErrorMessage { get; }

    public MultiplayerBootstrapFailed(string errorCode, string errorMessage)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }
}
