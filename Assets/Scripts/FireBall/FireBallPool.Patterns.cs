using UnityEngine;

public partial class FireBallPool
{
    private enum ShotPattern
    {
        Single,
        Double,
        Opposite,
        Fast
    }

    private enum ShotLane
    {
        Low,
        High
    }

    private float GetPatternImpactTailDelay(ShotPattern pattern, float projectileSpeed)
    {
        if (pattern != ShotPattern.Double || player == null)
            return 0f;

        float relativeSpeed = Mathf.Max(0.1f, player.speed + projectileSpeed);
        return doubleShotGap * projectileSpeed / relativeSpeed;
    }

    private float SelectProjectileSpeed(ShotPattern pattern, ShotLane lane)
    {
        if (pattern == ShotPattern.Fast)
            return Random.Range(fastMinProjectileSpeed, fastMaxProjectileSpeed);

        if (lane == ShotLane.High)
            return Random.Range(highMinProjectileSpeed, highMaxProjectileSpeed);

        return Random.Range(normalMinProjectileSpeed, normalMaxProjectileSpeed);
    }

    private ShotPattern PickPattern(float difficulty)
    {
        if (difficulty < doublePatternUnlockDifficulty)
            return ShotPattern.Single;

        float singleWeight = Mathf.Max(0f, singlePatternWeight);
        float doubleWeight = Mathf.Max(0f, doublePatternWeight);
        float oppositeWeight = difficulty >= advancedPatternUnlockDifficulty
            ? Mathf.Max(0f, oppositePatternWeight)
            : 0f;
        float fastWeight = difficulty >= advancedPatternUnlockDifficulty
            ? Mathf.Max(0f, fastPatternWeight)
            : 0f;

        float totalWeight = singleWeight + doubleWeight + oppositeWeight + fastWeight;
        if (totalWeight <= 0f)
            return ShotPattern.Single;

        float roll = Random.Range(0f, totalWeight);
        if (roll < singleWeight)
            return ShotPattern.Single;

        roll -= singleWeight;
        if (roll < doubleWeight)
            return ShotPattern.Double;

        roll -= doubleWeight;
        if (roll < oppositeWeight)
            return ShotPattern.Opposite;

        return ShotPattern.Fast;
    }

    private bool CanContinuePatternOnEdge(Edge edge)
    {
        if (player == null || player.CurrentEdge != edge)
            return false;

        Camera cam = Camera.main;
        if (cam == null)
            return false;

        ScreenEdgeBounds b = ScreenEdgeBounds.FromCamera(cam, player.edgeOffset);
        return !IsNearUpcomingCorner(b, edge);
    }
}
