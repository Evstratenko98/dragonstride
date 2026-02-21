public interface IRandomSource
{
    int Range(int minInclusive, int maxExclusive);
    float Value01();
    void ResetSeed(int seed);
}
