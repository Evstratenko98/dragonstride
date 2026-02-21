public readonly struct MultiplayerQuerySessionsRequest
{
    public int Count { get; }
    public int Skip { get; }

    public MultiplayerQuerySessionsRequest(int count, int skip)
    {
        Count = count;
        Skip = skip;
    }
}
