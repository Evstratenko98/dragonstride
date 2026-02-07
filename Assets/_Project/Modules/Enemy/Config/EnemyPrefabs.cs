using UnityEngine;

public sealed class EnemyPrefabs
{
    public GameObject SlimePrefab { get; }
    public GameObject WolfPrefab { get; }

    public EnemyPrefabs(GameObject slimePrefab, GameObject wolfPrefab)
    {
        SlimePrefab = slimePrefab;
        WolfPrefab = wolfPrefab;
    }
}
