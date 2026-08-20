using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireBallPool : MonoBehaviour
{
    private sealed class WarningGlow
    {
        public GameObject Root;
        public SpriteRenderer Outer;
        public SpriteRenderer Core;
    }

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

    void Start()
    {
        for (int i = 0; i < poolSize; i++)
        {
            Fireball fb = Instantiate(fireballPrefab, transform);
            fb.gameObject.SetActive(false);
            pool.Add(fb);
        }

        StartCoroutine(SpawnDirectorRoutine());
    }

    IEnumerator SpawnDirectorRoutine()
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

    IEnumerator FirePatternRoutine(ShotPattern pattern, ShotLane lane)
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

    float GetPatternImpactTailDelay(ShotPattern pattern, float projectileSpeed)
    {
        if (pattern != ShotPattern.Double || player == null)
            return 0f;

        float relativeSpeed = Mathf.Max(0.1f, player.speed + projectileSpeed);
        return doubleShotGap * projectileSpeed / relativeSpeed;
    }

    float SelectProjectileSpeed(ShotPattern pattern, ShotLane lane)
    {
        if (pattern == ShotPattern.Fast)
            return Random.Range(fastMinProjectileSpeed, fastMaxProjectileSpeed);

        if (lane == ShotLane.High)
            return Random.Range(highMinProjectileSpeed, highMaxProjectileSpeed);

        return Random.Range(normalMinProjectileSpeed, normalMaxProjectileSpeed);
    }

    ShotPattern PickPattern(float difficulty)
    {
        if (difficulty < 1.25f)
            return ShotPattern.Single;

        int patternCount = difficulty < 1.75f ? 2 : 4;
        return (ShotPattern)Random.Range(0, patternCount);
    }

    Edge GetPrimaryThreatEdge()
    {
        if (player == null)
            return (Edge)Random.Range(0, 4);

        return player.CurrentEdge;
    }

    IEnumerator ShowTrackedWarnings(
        float duration,
        bool includeOpposite,
        float laneOffset,
        float projectileSpeed,
        float impactTailDelay,
        System.Action<Edge> onCompleted)
    {
        Camera cam = Camera.main;
        if (cam == null || player == null)
            yield break;

        ScreenEdgeBounds b = ScreenEdgeBounds.FromCamera(cam, player.edgeOffset);

        while (player != null)
        {
            Edge warningEdge = GetPrimaryThreatEdge();

            if (IsNearUpcomingCorner(b, warningEdge))
            {
                yield return null;
                continue;
            }

            float scheduledImpactTime = PredictImpactTime(
                b,
                warningEdge,
                duration,
                projectileSpeed) + impactTailDelay;
            if (scheduledImpactTime < lastScheduledImpactTime + minimumLaneImpactGap)
            {
                yield return null;
                continue;
            }

            activeWarnings.Add(CreateWarningGlow(b, warningEdge, laneOffset));
            if (includeOpposite)
                activeWarnings.Add(CreateWarningGlow(
                    b,
                    EdgeMath.GetOppositeEdge(warningEdge),
                    laneOffset));
            lastWarningStartedTime = Time.time;

            float elapsed = 0f;
            bool shouldRestart = false;

            while (elapsed < duration)
            {
                if (player == null)
                {
                    ClearWarnings();
                    yield break;
                }

                if (player.CurrentEdge != warningEdge || IsNearUpcomingCorner(b, warningEdge))
                {
                    shouldRestart = true;
                    break;
                }

                UpdateWarningGlows(elapsed, duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            ClearWarnings();

            if (shouldRestart)
            {
                yield return null;
                continue;
            }

            lastScheduledImpactTime = scheduledImpactTime;
            onCompleted?.Invoke(warningEdge);
            yield break;
        }
    }

    float PredictImpactTime(
        ScreenEdgeBounds b,
        Edge edge,
        float warningTime,
        float projectileSpeed)
    {
        float playerSpeed = Mathf.Max(0f, player.speed);
        float remainingDistance = GetRemainingDistanceToSpawn(b, edge);
        float distanceAfterWarning = Mathf.Max(0f, remainingDistance - playerSpeed * warningTime);
        float relativeSpeed = Mathf.Max(0.1f, playerSpeed + projectileSpeed);
        return Time.time + warningTime + distanceAfterWarning / relativeSpeed;
    }

    bool CanContinuePatternOnEdge(Edge edge)
    {
        if (player == null || player.CurrentEdge != edge)
            return false;

        Camera cam = Camera.main;
        if (cam == null)
            return false;

        ScreenEdgeBounds b = ScreenEdgeBounds.FromCamera(cam, player.edgeOffset);
        return !IsNearUpcomingCorner(b, edge);
    }

    bool IsNearUpcomingCorner(ScreenEdgeBounds b, Edge edge)
    {
        return GetRemainingDistanceToSpawn(b, edge) <= cornerWarningBuffer;
    }

    float GetRemainingDistanceToSpawn(ScreenEdgeBounds b, Edge edge)
    {
        Vector2 pos = player.transform.position;
        float remainingDistance;

        switch (edge)
        {
            case Edge.Bottom:
                remainingDistance = b.MaxX - pos.x;
                break;
            case Edge.Right:
                remainingDistance = b.MaxY - pos.y;
                break;
            case Edge.Top:
                remainingDistance = pos.x - b.MinX;
                break;
            default:
                remainingDistance = pos.y - b.MinY;
                break;
        }

        return Mathf.Max(0f, remainingDistance);
    }

    void ClearWarnings()
    {
        for (int i = 0; i < activeWarnings.Count; i++)
        {
            if (activeWarnings[i]?.Root != null)
                Destroy(activeWarnings[i].Root);
        }

        activeWarnings.Clear();
    }

    WarningGlow CreateWarningGlow(ScreenEdgeBounds b, Edge edge, float laneOffset)
    {
        EnsureWarningSprites();

        GameObject warning = new GameObject($"WarningGlow_{edge}");
        warning.transform.SetParent(transform, false);

        Vector2 center = EdgeMath.GetStartCornerForCounterClockwiseMotion(b, edge) -
                         EdgeMath.GetGravityDir(edge) * laneOffset;
        float rotation;
        float projectileThickness = GetProjectileThickness();
        float warningLampLength = projectileThickness * Mathf.Max(0.1f, warningLampSizeMultiplier);
        float warningThickness = warningLampLength * Mathf.Max(0.01f, warningCoreThicknessRatio);
        float warningGlowThickness = warningLampLength * Mathf.Max(0.05f, warningGlowThicknessRatio);
        float lampLength = Mathf.Min(
            warningLampLength,
            Mathf.Min(b.MaxX - b.MinX, b.MaxY - b.MinY) * 0.25f);

        switch (edge)
        {
            case Edge.Bottom:
                rotation = 90f;
                break;
            case Edge.Right:
                rotation = 0f;
                break;
            case Edge.Top:
                rotation = 90f;
                break;
            default:
                rotation = 0f;
                break;
        }

        warning.transform.SetPositionAndRotation(center, Quaternion.Euler(0f, 0f, rotation));

        WarningGlow glow = new WarningGlow
        {
            Root = warning,
            Outer = CreateWarningLayer(warning.transform, "OuterGlow", warningGlowSprite,
                new Vector2(lampLength, warningGlowThickness), warningGlowColor, 14),
            Core = CreateWarningLayer(warning.transform, "CoreGlow", warningSprite,
                new Vector2(lampLength, warningThickness), warningColor, 15)
        };

        return glow;
    }

    float GetProjectileThickness()
    {
        if (fireballPrefab == null)
            return 0.45f;

        SpriteRenderer projectileRenderer = fireballPrefab.GetComponent<SpriteRenderer>();
        if (projectileRenderer == null || projectileRenderer.sprite == null)
            return 0.45f;

        Vector2 spriteSize = projectileRenderer.sprite.bounds.size;
        Vector3 scale = projectileRenderer.transform.lossyScale;
        float width = Mathf.Abs(spriteSize.x * scale.x);
        float height = Mathf.Abs(spriteSize.y * scale.y);
        return Mathf.Max(0.1f, Mathf.Min(width, height));
    }

    SpriteRenderer CreateWarningLayer(
        Transform parent,
        string layerName,
        Sprite sprite,
        Vector2 size,
        Color color,
        int sortingOrder)
    {
        GameObject layer = new GameObject(layerName);
        layer.transform.SetParent(parent, false);
        layer.transform.localScale = size;

        SpriteRenderer renderer = layer.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    void UpdateWarningGlows(float elapsed, float duration)
    {
        float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
        float blink = Mathf.Pow(Mathf.Sin(progress * Mathf.Max(1, warningBlinkCount) * Mathf.PI), 2f);
        float intensity = Mathf.Lerp(0.08f, 1f, blink);

        for (int i = 0; i < activeWarnings.Count; i++)
        {
            WarningGlow glow = activeWarnings[i];
            if (glow == null)
                continue;

            SetRendererAlpha(glow.Outer, warningGlowColor, intensity);
            SetRendererAlpha(glow.Core, warningColor, intensity);
        }
    }

    void SetRendererAlpha(SpriteRenderer renderer, Color baseColor, float intensity)
    {
        if (renderer == null)
            return;

        baseColor.a *= Mathf.Clamp01(intensity);
        renderer.color = baseColor;
    }

    void EnsureWarningSprites()
    {
        if (warningSprite == null)
            warningSprite = CreateWarningStripSprite("WarningCoreTexture", 0.35f, 0.78f);

        if (warningGlowSprite != null)
            return;

        warningGlowSprite = CreateWarningStripSprite("WarningGlowTexture", 2.4f, 0.48f);
    }

    Sprite CreateWarningStripSprite(string textureName, float crossFadePower, float endFadeStart)
    {
        const int textureSize = 32;
        Texture2D texture = new Texture2D(textureSize, textureSize);
        texture.name = textureName;
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < textureSize; y++)
        {
            float normalizedY = (y + 0.5f) / textureSize;
            float distanceFromCenter = Mathf.Abs(normalizedY * 2f - 1f);
            float crossFade = Mathf.Pow(1f - distanceFromCenter, crossFadePower);

            for (int x = 0; x < textureSize; x++)
            {
                float normalizedX = (x + 0.5f) / textureSize;
                float distanceFromEnd = Mathf.Abs(normalizedX * 2f - 1f);
                float endFade = 1f - Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.InverseLerp(endFadeStart, 1f, distanceFromEnd));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, crossFade * endFade));
            }
        }

        texture.Apply();
        return Sprite.Create(
            texture,
            new Rect(0, 0, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            textureSize);
    }

    void SpawnFireball(Edge spawnEdge, float moveSpeed, float laneOffset)
    {
        Fireball fb = GetAvailableFireball();
        if (fb == null) return;

        if (player == null)
        {
            Debug.LogError("FireBallPool: player is null");
            return;
        }
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("FireBallPool: Camera.main is null");
            return;
        }

        ScreenEdgeBounds b = ScreenEdgeBounds.FromCamera(cam, player.edgeOffset);
        Vector2 spawnPos = EdgeMath.GetStartCornerForCounterClockwiseMotion(b, spawnEdge);

        // 이전 풀 오브젝트 포즈가 1프레임 보이지 않도록, 비활성 상태에서 먼저 Init 후 활성화
        fb.gameObject.SetActive(false);
        fb.Init(spawnPos, spawnEdge, player.edgeOffset, moveSpeed, laneOffset);
        fb.gameObject.SetActive(true);
    }

    Fireball GetAvailableFireball()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if(!pool[i].gameObject.activeSelf)
                return pool[i];
        }
        return null;
    }
}
