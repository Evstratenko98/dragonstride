public readonly struct OnlineLootTaken
{
    public int ActorId { get; }

    public OnlineLootTaken(int actorId)
    {
        ActorId = actorId;
    }
}
