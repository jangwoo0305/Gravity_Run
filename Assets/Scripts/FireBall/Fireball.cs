using UnityEngine;
using Random = UnityEngine.Random;

public class Fireball : MonoBehaviour
{
    private Rigidbody2D rb;

    [SerializeField] private float minSpeed = 2f;
    [SerializeField] private float maxSpeed = 3f;
    [SerializeField] private float waveAmplitude = 0.3f;
    [SerializeField] private float waveFrequency = 4f;
    [SerializeField] private bool useWaveByDefault = true;
    [Range(0f,1f)]
    [SerializeField] private float waveChance = 0.6f;
    [SerializeField] private float edgeOffset = 0.3f; // 기본값 (풀에서 플레이어 값으로 덮어쓸 수 있음)
    [Header("Corner Blend")]
    [Min(0f)]
    [SerializeField] private float cornerBlendDistance = 0.25f; // 코너 근처에서 다음 Edge 각도로 서서히 보간
    [Min(0.01f)]
    [SerializeField] private float rotationLerpSpeed = 14f; // 높을수록 빨리 따라감
    
    private Edge currentEdge;
    private float speed;

    private float waveTime;
    private bool useWave;
    
    private ScreenEdgeBounds bounds;
    private float targetAngle;

    private int edgeChangeCount;
    private const int EDGES_PER_LAP = 4;
    private const int MAX_LAP = 1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // fb.Init(spawnPos, spawnEdge, player.edgeOffset);
    // 🔹 Pool에서 호출 (생성 시 1회)
    public void Init(Vector2 spawnWorldPos, Edge startEdge, float edgeOffsetWorld)
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        edgeOffset = edgeOffsetWorld;
        bounds = ScreenEdgeBounds.FromCamera(cam, edgeOffset);

        speed = Random.Range(minSpeed, maxSpeed);

        edgeChangeCount = 0;
        currentEdge = startEdge;

        float angle = EdgeMath.GetAngleForCounterClockwiseMotion(currentEdge);
        targetAngle = angle;
        Vector2 clampedSpawn = ClampToEdge(spawnWorldPos, startEdge);

        // 풀링으로 Enable/Disable 될 때 이전 포즈가 1프레임 보이지 않도록 Transform/Rigidbody2D 둘 다 세팅
        transform.SetPositionAndRotation(clampedSpawn, Quaternion.Euler(0f, 0f, angle));
        rb.position = clampedSpawn;
        rb.rotation = angle;

        waveTime = 0f;
        
        if (useWaveByDefault)
            useWave = Random.value < waveChance;
        else
        {
            useWave = false;
        }
    }

    private void FixedUpdate()
    {
        MoveCounterClockwise();
        UpdateCornerBlendedRotation();
        CheckEdgeChange();
        waveTime += Time.fixedDeltaTime;
    }
    

    // =====================
    // 이동
    // =====================
    void MoveCounterClockwise()
    {
        Vector2 pos = rb.position;
        float delta = speed * Time.fixedDeltaTime;
        // 웨이브 오프셋은 항상 화면 안쪽 방향으로만 적용해서 화면 밖으로 나가지 않게 함
        // 범위: [0, waveAmplitude]
        float waveOffset = useWave ? Mathf.Abs(Mathf.Sin(waveTime * waveFrequency)) * waveAmplitude : 0f;

        switch (currentEdge)
        {
            // Bottom: 왼쪽으로 이동 (플레이어 진행 방향과 반대)
            case Edge.Bottom:
                pos.x -= delta;
                pos.y = bounds.MinY + waveOffset; // 안쪽 방향은 +Y
                break;

            // Right: 아래로 이동 (플레이어 진행 방향과 반대)
            case Edge.Right:
                pos.y -= delta;
                pos.x = bounds.MaxX - waveOffset; // 안쪽 방향은 -X
                break;

            // Top: 오른쪽으로 이동 (플레이어 진행 방향과 반대)
            case Edge.Top:
                pos.x += delta;
                pos.y = bounds.MaxY - waveOffset; // 안쪽 방향은 -Y
                break;

            // Left: 위로 이동 (플레이어 진행 방향과 반대)
            case Edge.Left:
                pos.y += delta;
                pos.x = bounds.MinX + waveOffset; // 안쪽 방향은 +X
                break;
        }

        rb.MovePosition(pos);
    }

    // =====================
    // Edge 변경 체크 (반시계 방향)
    // =====================
    void CheckEdgeChange()
    {
        Vector2 pos = rb.position;
        float epsilon = 0.01f;

        switch (currentEdge)
        {
            // Bottom → Left (minX에 도달하면)
            case Edge.Bottom:
                if (pos.x < bounds.MinX + epsilon)
                {
                    ChangeEdge(Edge.Left, new Vector2(bounds.MinX, bounds.MinY));
                    return;
                }
                break;

            // Left → Top (maxY에 도달하면)
            case Edge.Left:
                if (pos.y > bounds.MaxY - epsilon)
                {
                    ChangeEdge(Edge.Top, new Vector2(bounds.MinX, bounds.MaxY));
                    return;
                }
                break;

            // Top → Right (maxX에 도달하면)
            case Edge.Top:
                if (pos.x > bounds.MaxX - epsilon)
                {
                    ChangeEdge(Edge.Right, new Vector2(bounds.MaxX, bounds.MaxY));
                    return;
                }
                break;

            // Right → Bottom (minY에 도달하면)
            case Edge.Right:
                if (pos.y < bounds.MinY + epsilon)
                {
                    ChangeEdge(Edge.Bottom, new Vector2(bounds.MaxX, bounds.MinY));
                    return;
                }
                break;
        }
    }

    void ChangeEdge(Edge next, Vector2 cornerPos)
    {
        edgeChangeCount++;

        if (edgeChangeCount >= EDGES_PER_LAP * MAX_LAP)
        {
            gameObject.SetActive(false);
            return;
        }

        currentEdge = next;

        // 코너로 1회 스냅해서 전환을 깔끔하게 만든 뒤, 다음 Edge에서 계속 이동
        rb.position = cornerPos;
        targetAngle = EdgeMath.GetAngleForCounterClockwiseMotion(currentEdge);
    }

    private void UpdateCornerBlendedRotation()
    {
        if (cornerBlendDistance <= 0f)
        {
            targetAngle = EdgeMath.GetAngleForCounterClockwiseMotion(currentEdge);
        }
        else
        {
            Vector2 pos = rb.position;
            Edge next = EdgeMath.GetNextEdgeCounterClockwise(currentEdge);

            float a0 = EdgeMath.GetAngleForCounterClockwiseMotion(currentEdge);
            float a1 = EdgeMath.GetAngleForCounterClockwiseMotion(next);
            float t = 0f;

            // 코너 접근 방향에 따라 t를 계산해서 각도를 미리(코너에 닿기 전) 돌려준다.
            switch (currentEdge)
            {
                case Edge.Bottom: // x: MaxX -> MinX 로 이동, MinX 근처에서 Left로 전환
                    if (pos.x < bounds.MinX + cornerBlendDistance)
                        t = Mathf.InverseLerp(bounds.MinX + cornerBlendDistance, bounds.MinX, pos.x);
                    break;
                case Edge.Left: // y: MinY -> MaxY 로 이동, MaxY 근처에서 Top으로 전환
                    if (pos.y > bounds.MaxY - cornerBlendDistance)
                        t = Mathf.InverseLerp(bounds.MaxY - cornerBlendDistance, bounds.MaxY, pos.y);
                    break;
                case Edge.Top: // x: MinX -> MaxX 로 이동, MaxX 근처에서 Right로 전환
                    if (pos.x > bounds.MaxX - cornerBlendDistance)
                        t = Mathf.InverseLerp(bounds.MaxX - cornerBlendDistance, bounds.MaxX, pos.x);
                    break;
                case Edge.Right: // y: MaxY -> MinY 로 이동, MinY 근처에서 Bottom으로 전환
                    if (pos.y < bounds.MinY + cornerBlendDistance)
                        t = Mathf.InverseLerp(bounds.MinY + cornerBlendDistance, bounds.MinY, pos.y);
                    break;
            }

            targetAngle = Mathf.LerpAngle(a0, a1, Mathf.Clamp01(t));
        }

        // 프레임레이트에 덜 민감한 지수 보간.
        float alpha = 1f - Mathf.Exp(-rotationLerpSpeed * Time.fixedDeltaTime);
        float newAngle = Mathf.LerpAngle(rb.rotation, targetAngle, alpha);
        rb.MoveRotation(newAngle);
    }

    Vector2 ClampToEdge(Vector2 worldPos, Edge edge)
    {
        float clampedX = Mathf.Clamp(worldPos.x, bounds.MinX, bounds.MaxX);
        float clampedY = Mathf.Clamp(worldPos.y, bounds.MinY, bounds.MaxY);

        return edge switch
        {
            Edge.Bottom => new Vector2(clampedX, bounds.MinY),
            Edge.Right => new Vector2(bounds.MaxX, clampedY),
            Edge.Top => new Vector2(clampedX, bounds.MaxY),
            Edge.Left => new Vector2(bounds.MinX, clampedY),
            _ => new Vector2(clampedX, clampedY)
        };
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GameOver();
        }
    }

    public void GameOver()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
