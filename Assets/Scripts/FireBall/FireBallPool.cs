using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class FireBallPool : MonoBehaviour
{
    [SerializeField] private Fireball fireballPrefab;
    [SerializeField] private int poolSize = 20;
    [SerializeField] private float spawnInterval = 1.7f;
    [SerializeField] private float minSpawnInterval = 0.7f;
    [SerializeField] private float highSpawnInterval = 3f;
    [SerializeField] private float minHighSpawnInterval = 1.8f;
    [SerializeField] private float highInitialDelay = 5f;
    [SerializeField] private float minimumLaneImpactGap = 0.7f;
    [SerializeField] private float highLaneHeightMultiplier = 2f;
    [SerializeField] private float warningDuration = 0.65f;
    [SerializeField] private float fastWarningDuration = 0.28f;
    [SerializeField] private float doubleShotGap = 0.32f;
    [SerializeField] private float normalMinProjectileSpeed = 3.4f;
    [SerializeField] private float normalMaxProjectileSpeed = 4.8f;
    [SerializeField] private float highMinProjectileSpeed = 3f;
    [SerializeField] private float highMaxProjectileSpeed = 4.2f;
    [SerializeField] private float fastMinProjectileSpeed = 5.5f;
    [SerializeField] private float fastMaxProjectileSpeed = 6.5f;
    [SerializeField, Min(0f)] private float doublePatternUnlockDifficulty = 1.25f;
    [SerializeField, Min(0f)] private float advancedPatternUnlockDifficulty = 1.75f;
    [SerializeField, Min(0f)] private float singlePatternWeight = 1f;
    [SerializeField, Min(0f)] private float doublePatternWeight = 1f;
    [SerializeField, Min(0f)] private float oppositePatternWeight = 0.75f;
    [SerializeField, Min(0f)] private float fastPatternWeight = 0.55f;
    [SerializeField] private float cornerWarningBuffer = 1.15f;
    [SerializeField] private float warningLampSizeMultiplier = 1f;
    [SerializeField] private float warningCoreThicknessRatio = 0.12f;
    [SerializeField] private float warningGlowThicknessRatio = 0.55f;
    [SerializeField] private int warningBlinkCount = 3;
    [SerializeField] private Color warningColor = new Color(1f, 0.02f, 0.08f, 0.95f);
    [SerializeField] private Color warningGlowColor = new Color(1f, 0f, 0.06f, 0.42f);
    [SerializeField] private PlayerMove player;

    private readonly List<Fireball> pool = new List<Fireball>();
    private readonly List<WarningGlow> activeWarnings = new List<WarningGlow>();
    private Sprite warningSprite;
    private Sprite warningGlowSprite;
    private float lastWarningStartedTime;
    private float lastScheduledImpactTime = -999f;

    private void Start()
    {
        for (int i = 0; i < poolSize; i++)
        {
            Fireball fb = Instantiate(fireballPrefab, transform);
            fb.gameObject.SetActive(false);
            pool.Add(fb);
        }

        StartCoroutine(SpawnDirectorRoutine());
    }

    private IEnumerator SpawnDirectorRoutine()
    {
        float nextLowAttackTime = Time.time + spawnInterval;
        float nextHighAttackTime = Time.time + highInitialDelay;

        while (true)
        {
            float difficulty = GameSession.Difficulty;
            bool lowIsDue = Time.time >= nextLowAttackTime;
            bool highIsDue = Time.time >= nextHighAttackTime;

            if (!lowIsDue && !highIsDue)
            {
                yield return null;
                continue;
            }

            ShotLane lane = highIsDue && (!lowIsDue || nextHighAttackTime <= nextLowAttackTime)
                ? ShotLane.High
                : ShotLane.Low;
            ShotPattern pattern = lane == ShotLane.High
                ? ShotPattern.Single
                : PickPattern(difficulty);

            yield return StartCoroutine(FirePatternRoutine(pattern, lane));

            float scheduleFrom = lastWarningStartedTime > 0f
                ? lastWarningStartedTime
                : Time.time;
            if (lane == ShotLane.Low)
            {
                float lowInterval = Mathf.Max(minSpawnInterval, spawnInterval / difficulty);
                nextLowAttackTime = scheduleFrom + lowInterval;
            }
            else
            {
                float highInterval = Mathf.Max(minHighSpawnInterval, highSpawnInterval / difficulty);
                nextHighAttackTime = scheduleFrom + highInterval;
            }
        }
    }

    private IEnumerator FirePatternRoutine(ShotPattern pattern, ShotLane lane)
    {
        Edge primaryEdge = Edge.Bottom;
        bool warningCompleted = false;
        float duration = pattern == ShotPattern.Fast ? fastWarningDuration : warningDuration;
        bool includeOpposite = pattern == ShotPattern.Opposite;
        float projectileSpeed = SelectProjectileSpeed(pattern, lane);
        float laneOffset = lane == ShotLane.High
            ? GetProjectileThickness() * highLaneHeightMultiplier
            : 0f;
        float impactTailDelay = GetPatternImpactTailDelay(pattern, projectileSpeed);

        yield return StartCoroutine(ShowTrackedWarnings(
            duration,
            includeOpposite,
            laneOffset,
            projectileSpeed,
            impactTailDelay,
            edge =>
            {
                primaryEdge = edge;
                warningCompleted = true;
            }));

        if (!warningCompleted)
            yield break;

        switch (pattern)
        {
            case ShotPattern.Double:
                SpawnFireball(primaryEdge, projectileSpeed, laneOffset);
                yield return new WaitForSeconds(doubleShotGap);
                if (CanContinuePatternOnEdge(primaryEdge))
                    SpawnFireball(primaryEdge, projectileSpeed, laneOffset);
                break;

            case ShotPattern.Opposite:
                Edge oppositeEdge = EdgeMath.GetOppositeEdge(primaryEdge);
                SpawnFireball(primaryEdge, projectileSpeed, laneOffset);
                SpawnFireball(oppositeEdge, projectileSpeed, laneOffset);
                break;

            case ShotPattern.Fast:
                SpawnFireball(primaryEdge, projectileSpeed, laneOffset);
                break;

            default:
                SpawnFireball(primaryEdge, projectileSpeed, laneOffset);
                break;
        }
    }
}
