using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireBallPool : MonoBehaviour
{
    private enum ShotPattern
    {
        Single,
        Double,
        Opposite,
        Fast
    }

    [SerializeField] private Fireball fireballPrefab;
    [SerializeField] private int poolSize = 20;
    [SerializeField] private float spawnInterval = 2.8f;
    [SerializeField] private float minSpawnInterval = 0.85f;
    [SerializeField] private float warningDuration = 0.65f;
    [SerializeField] private float fastWarningDuration = 0.28f;
    [SerializeField] private float doubleShotGap = 0.32f;
    [SerializeField] private float warningThickness = 0.08f;
    [SerializeField] private Color warningColor = new Color(1f, 0.28f, 0.05f, 0.78f);
    [SerializeField] private PlayerMove player;
    
    
    private readonly List<Fireball> pool = new List<Fireball>();
    private readonly List<SpriteRenderer> activeWarnings = new List<SpriteRenderer>();
    private Sprite warningSprite;

    void Start()
    {
        for (int i = 0; i < poolSize; i++)
        {
            Fireball fb = Instantiate(fireballPrefab, transform);
            fb.gameObject.SetActive(false);
            pool.Add(fb);
        }

        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            float difficulty = GameSession.Difficulty;
            float waitTime = Mathf.Max(minSpawnInterval, spawnInterval / difficulty);
            yield return new WaitForSeconds(waitTime);
            ShotPattern pattern = PickPattern(difficulty);
            yield return StartCoroutine(FirePatternRoutine(pattern, difficulty));
        }
    }

    IEnumerator FirePatternRoutine(ShotPattern pattern, float difficulty)
    {
        Edge primaryEdge = GetPrimaryThreatEdge();

        switch (pattern)
        {
            case ShotPattern.Double:
                yield return StartCoroutine(ShowWarnings(warningDuration, primaryEdge));
                SpawnFireball(primaryEdge, difficulty);
                yield return new WaitForSeconds(doubleShotGap);
                SpawnFireball(primaryEdge, difficulty);
                break;

            case ShotPattern.Opposite:
                Edge oppositeEdge = EdgeMath.GetOppositeEdge(primaryEdge);
                yield return StartCoroutine(ShowWarnings(warningDuration, primaryEdge, oppositeEdge));
                SpawnFireball(primaryEdge, difficulty);
                SpawnFireball(oppositeEdge, difficulty);
                break;

            case ShotPattern.Fast:
                yield return StartCoroutine(ShowWarnings(fastWarningDuration, primaryEdge));
                SpawnFireball(primaryEdge, difficulty * 1.45f);
                break;

            default:
                yield return StartCoroutine(ShowWarnings(warningDuration, primaryEdge));
                SpawnFireball(primaryEdge, difficulty);
                break;
        }
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

    IEnumerator ShowWarnings(params Edge[] edges)
    {
        yield return ShowWarnings(warningDuration, edges);
    }

    IEnumerator ShowWarnings(float duration, params Edge[] edges)
    {
        Camera cam = Camera.main;
        if (cam == null || player == null)
            yield break;

        ScreenEdgeBounds b = ScreenEdgeBounds.FromCamera(cam, player.edgeOffset);

        for (int i = 0; i < edges.Length; i++)
            activeWarnings.Add(CreateWarningLine(b, edges[i]));

        yield return new WaitForSeconds(duration);

        for (int i = 0; i < activeWarnings.Count; i++)
        {
            if (activeWarnings[i] != null)
                Destroy(activeWarnings[i].gameObject);
        }
        activeWarnings.Clear();
    }

    SpriteRenderer CreateWarningLine(ScreenEdgeBounds b, Edge edge)
    {
        EnsureWarningSprite();

        GameObject warning = new GameObject($"WarningLine_{edge}");
        warning.transform.SetParent(transform, false);

        SpriteRenderer renderer = warning.AddComponent<SpriteRenderer>();
        renderer.sprite = warningSprite;
        renderer.color = warningColor;
        renderer.sortingOrder = 15;

        Vector2 center;
        Vector2 size;

        switch (edge)
        {
            case Edge.Bottom:
                center = new Vector2((b.MinX + b.MaxX) * 0.5f, b.MinY);
                size = new Vector2(b.MaxX - b.MinX, warningThickness);
                break;
            case Edge.Right:
                center = new Vector2(b.MaxX, (b.MinY + b.MaxY) * 0.5f);
                size = new Vector2(warningThickness, b.MaxY - b.MinY);
                break;
            case Edge.Top:
                center = new Vector2((b.MinX + b.MaxX) * 0.5f, b.MaxY);
                size = new Vector2(b.MaxX - b.MinX, warningThickness);
                break;
            default:
                center = new Vector2(b.MinX, (b.MinY + b.MaxY) * 0.5f);
                size = new Vector2(warningThickness, b.MaxY - b.MinY);
                break;
        }

        warning.transform.position = center;
        warning.transform.localScale = size;
        return renderer;
    }

    void EnsureWarningSprite()
    {
        if (warningSprite != null)
            return;

        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        warningSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    void SpawnFireball(Edge spawnEdge, float speedMultiplier)
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
        fb.Init(spawnPos, spawnEdge, player.edgeOffset, speedMultiplier);
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
