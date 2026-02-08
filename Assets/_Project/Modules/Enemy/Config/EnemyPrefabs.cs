using UnityEngine;

public sealed class EnemyPrefabs
{
    public GameObject SlimePrefab { get; }
    public GameObject WolfPrefab { get; }
    public GameObject BossPrefab { get; }
    public int SlimeSpawnChancePercent { get; }
    public int WolfSpawnChancePercent { get; }

    public EnemyPrefabs(
        GameObject slimePrefab,
        GameObject wolfPrefab,
        GameObject bossPrefab,
        int slimeSpawnChancePercent,
        int wolfSpawnChancePercent)
    {
        SlimePrefab = slimePrefab;
        WolfPrefab = wolfPrefab;
        BossPrefab = bossPrefab;
        SlimeSpawnChancePercent = Mathf.Max(0, slimeSpawnChancePercent);
        WolfSpawnChancePercent = Mathf.Max(0, wolfSpawnChancePercent);
    }
}
