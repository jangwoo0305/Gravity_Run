using System.Collections;
using UnityEngine;

public partial class FireBallPool
{
    private sealed class WarningGlow
    {
        public GameObject Root;
        public SpriteRenderer Outer;
        public SpriteRenderer Core;
    }

    private IEnumerator ShowTrackedWarnings(
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

    private Edge GetPrimaryThreatEdge()
    {
        if (player == null)
            return (Edge)Random.Range(0, 4);

        return player.CurrentEdge;
    }

    private float PredictImpactTime(
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

    private bool IsNearUpcomingCorner(ScreenEdgeBounds b, Edge edge)
    {
        return GetRemainingDistanceToSpawn(b, edge) <= cornerWarningBuffer;
    }

    private float GetRemainingDistanceToSpawn(ScreenEdgeBounds b, Edge edge)
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

    private void ClearWarnings()
    {
        for (int i = 0; i < activeWarnings.Count; i++)
        {
            if (activeWarnings[i]?.Root != null)
                Destroy(activeWarnings[i].Root);
        }

        activeWarnings.Clear();
    }

    private WarningGlow CreateWarningGlow(ScreenEdgeBounds b, Edge edge, float laneOffset)
    {
        EnsureWarningSprites();

        GameObject warning = new GameObject($"WarningGlow_{edge}");
        warning.transform.SetParent(transform, false);

        Vector2 center = EdgeMath.GetFireballStartCorner(b, edge) -
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

    private SpriteRenderer CreateWarningLayer(
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

    private void UpdateWarningGlows(float elapsed, float duration)
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

    private void SetRendererAlpha(SpriteRenderer renderer, Color baseColor, float intensity)
    {
        if (renderer == null)
            return;

        baseColor.a *= Mathf.Clamp01(intensity);
        renderer.color = baseColor;
    }

    private void EnsureWarningSprites()
    {
        if (warningSprite == null)
            warningSprite = CreateWarningStripSprite("WarningCoreTexture", 0.35f, 0.78f);

        if (warningGlowSprite != null)
            return;

        warningGlowSprite = CreateWarningStripSprite("WarningGlowTexture", 2.4f, 0.48f);
    }

    private Sprite CreateWarningStripSprite(string textureName, float crossFadePower, float endFadeStart)
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
}
