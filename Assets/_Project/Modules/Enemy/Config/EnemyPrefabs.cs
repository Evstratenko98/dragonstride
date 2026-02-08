using UnityEngine;

public sealed class EnemyPrefabs
{
    public GameObject SlimePrefab { get; }
    public GameObject WolfPrefab { get; }
    public int SlimeSpawnChancePercent { get; }
    public int WolfSpawnChancePercent { get; }

    public EnemyPrefabs(
        GameObject slimePrefab,
        GameObject wolfPrefab,
        int slimeSpawnChancePercent,
        int wolfSpawnChancePercent)
    {
        SlimePrefab = slimePrefab;
        WolfPrefab = wolfPrefab;
        SlimeSpawnChancePercent = Mathf.Max(0, slimeSpawnChancePercent);
        WolfSpawnChancePercent = Mathf.Max(0, wolfSpawnChancePercent);
    }
}
