public interface ITurnAuthorityService
{
    CommandValidationResult Validate(GameCommandEnvelope command);
}
