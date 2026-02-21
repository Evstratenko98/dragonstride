public interface IMatchStatePublisher
{
    MatchStateSnapshot Capture(long sequence, string phase = "in_game");
}
