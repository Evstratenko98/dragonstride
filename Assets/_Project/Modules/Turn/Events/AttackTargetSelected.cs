public readonly struct AttackTargetSelected
{
    public int TargetActorId { get; }

    public AttackTargetSelected(int targetActorId)
    {
        TargetActorId = targetActorId;
    }
}
