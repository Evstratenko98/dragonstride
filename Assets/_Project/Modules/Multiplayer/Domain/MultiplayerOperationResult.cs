public readonly struct MultiplayerOperationResult<T>
{
    public bool IsSuccess { get; }
    public string ErrorCode { get; }
    public string ErrorMessage { get; }
    public T Value { get; }

    private MultiplayerOperationResult(bool isSuccess, T value, string errorCode, string errorMessage)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public static MultiplayerOperationResult<T> Success(T value)
    {
        return new MultiplayerOperationResult<T>(true, value, string.Empty, string.Empty);
    }

    public static MultiplayerOperationResult<T> Failure(string errorCode, string errorMessage)
    {
        return new MultiplayerOperationResult<T>(
            false,
            default,
            string.IsNullOrWhiteSpace(errorCode) ? "unknown_error" : errorCode,
            string.IsNullOrWhiteSpace(errorMessage) ? "Unknown error." : errorMessage);
    }
}
