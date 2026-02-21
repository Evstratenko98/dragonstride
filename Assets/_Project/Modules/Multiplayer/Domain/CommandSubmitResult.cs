public readonly struct CommandSubmitResult
{
    public bool IsAccepted { get; }
    public string ErrorCode { get; }
    public string ErrorMessage { get; }
    public long ServerSequence { get; }

    private CommandSubmitResult(bool isAccepted, string errorCode, string errorMessage, long serverSequence)
    {
        IsAccepted = isAccepted;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ServerSequence = serverSequence;
    }

    public static CommandSubmitResult Accepted(long serverSequence = 0)
    {
        return new CommandSubmitResult(true, string.Empty, string.Empty, serverSequence);
    }

    public static CommandSubmitResult Rejected(string errorCode, string errorMessage)
    {
        return new CommandSubmitResult(false, errorCode, errorMessage, 0);
    }
}
