public readonly struct ActionEventEnvelope
{
    public ActionEventType Type { get; }
    public int ActorId { get; }
    public int TargetActorId { get; }
    public int FromX { get; }
    public int FromY { get; }
    public int ToX { get; }
    public int ToY { get; }
    public int IntValue1 { get; }
    public int IntValue2 { get; }
    public bool BoolValue1 { get; }
    public bool BoolValue2 { get; }
    public string StrValue1 { get; }
    public string StrValue2 { get; }
    public int DurationMs { get; }

    public ActionEventEnvelope(
        ActionEventType type,
        int actorId,
        int targetActorId,
        int fromX,
        int fromY,
        int toX,
        int toY,
        int intValue1,
        int intValue2,
        bool boolValue1,
        bool boolValue2,
        string strValue1,
        string strValue2,
        int durationMs)
    {
        Type = type;
        ActorId = actorId;
        TargetActorId = targetActorId;
        FromX = fromX;
        FromY = fromY;
        ToX = toX;
        ToY = toY;
        IntValue1 = intValue1;
        IntValue2 = intValue2;
        BoolValue1 = boolValue1;
        BoolValue2 = boolValue2;
        StrValue1 = strValue1 ?? string.Empty;
        StrValue2 = strValue2 ?? string.Empty;
        DurationMs = durationMs < 0 ? 0 : durationMs;
    }
}
