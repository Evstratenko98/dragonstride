public readonly struct MultiplayerBootstrapResult
{
    public bool IsSuccess { get; }
    public string PlayerId { get; }
    public string ErrorCode { get; }
    public string ErrorMessage { get; }

    private MultiplayerBootstrapResult(bool isSuccess, string playerId, string errorCode, string errorMessage)
    {
        IsSuccess = isSuccess;
        PlayerId = playerId;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public static MultiplayerBootstrapResult Success(string playerId)
    {
        return new MultiplayerBootstrapResult(true, playerId, string.Empty, string.Empty);
    }

    public static MultiplayerBootstrapResult Failure(string errorCode, string errorMessage)
    {
        return new MultiplayerBootstrapResult(false, string.Empty, errorCode, errorMessage);
    }
}
