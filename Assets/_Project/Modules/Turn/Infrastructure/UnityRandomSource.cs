using System;

public sealed class UnityRandomSource : IRandomSource
{
    private Random _random = new(Environment.TickCount);

    public int Range(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            return minInclusive;
        }

        return _random.Next(minInclusive, maxExclusive);
    }

    public float Value01()
    {
        return (float)_random.NextDouble();
    }

    public void ResetSeed(int seed)
    {
        _random = new Random(seed);
    }
}
