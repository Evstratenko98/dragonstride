public readonly struct CommandValidationResult
{
    public bool IsValid { get; }
    public string ErrorCode { get; }
    public string ErrorMessage { get; }

    private CommandValidationResult(bool isValid, string errorCode, string errorMessage)
    {
        IsValid = isValid;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public static CommandValidationResult Success()
    {
        return new CommandValidationResult(true, string.Empty, string.Empty);
    }

    public static CommandValidationResult Failure(string errorCode, string errorMessage)
    {
        return new CommandValidationResult(false, errorCode, errorMessage);
    }
}
