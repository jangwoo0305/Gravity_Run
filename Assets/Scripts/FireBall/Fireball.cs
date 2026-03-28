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
    
    private Edge currentEdge;
    private float speed;

    private float waveTime;
    private bool useWave;
    
    private float minX, maxX, minY, maxY;

    private int edgeChangeCount;
    private const int EDGES_PER_LAP = 4;
    private const int MAX_LAP = 1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // 🔹 Pool에서 호출 (생성 시 1회)
    public void Init(Vector2 spawnWorldPos, Edge startEdge, float edgeOffsetWorld)
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        float z = cam.nearClipPlane;

        edgeOffset = edgeOffsetWorld;

        Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, z));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1, 1, z));

        minX = bottomLeft.x + edgeOffset;
        maxX = topRight.x - edgeOffset;
        minY = bottomLeft.y + edgeOffset;
        maxY = topRight.y - edgeOffset;

        speed = Random.Range(minSpeed, maxSpeed);

        edgeChangeCount = 0;
        currentEdge = startEdge;

        float angle = GetEdgeAngle();
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
                pos.y = minY + waveOffset; // 안쪽 방향은 +Y
                break;

            // Right: 아래로 이동 (플레이어 진행 방향과 반대)
            case Edge.Right:
                pos.y -= delta;
                pos.x = maxX - waveOffset; // 안쪽 방향은 -X
                break;

            // Top: 오른쪽으로 이동 (플레이어 진행 방향과 반대)
            case Edge.Top:
                pos.x += delta;
                pos.y = maxY - waveOffset; // 안쪽 방향은 -Y
                break;

            // Left: 위로 이동 (플레이어 진행 방향과 반대)
            case Edge.Left:
                pos.y += delta;
                pos.x = minX + waveOffset; // 안쪽 방향은 +X
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
                if (pos.x < minX + epsilon)
                {
                    ChangeEdge(Edge.Left, new Vector2(minX, minY));
                    return;
                }
                break;

            // Left → Top (maxY에 도달하면)
            case Edge.Left:
                if (pos.y > maxY - epsilon)
                {
                    ChangeEdge(Edge.Top, new Vector2(minX, maxY));
                    return;
                }
                break;

            // Top → Right (maxX에 도달하면)
            case Edge.Top:
                if (pos.x > maxX - epsilon)
                {
                    ChangeEdge(Edge.Right, new Vector2(maxX, maxY));
                    return;
                }
                break;

            // Right → Bottom (minY에 도달하면)
            case Edge.Right:
                if (pos.y < minY + epsilon)
                {
                    ChangeEdge(Edge.Bottom, new Vector2(maxX, minY));
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
        rb.rotation = GetEdgeAngle();
    }

    Vector2 ClampToEdge(Vector2 worldPos, Edge edge)
    {
        float clampedX = Mathf.Clamp(worldPos.x, minX, maxX);
        float clampedY = Mathf.Clamp(worldPos.y, minY, maxY);

        return edge switch
        {
            Edge.Bottom => new Vector2(clampedX, minY),
            Edge.Right => new Vector2(maxX, clampedY),
            Edge.Top => new Vector2(clampedX, maxY),
            Edge.Left => new Vector2(minX, clampedY),
            _ => new Vector2(clampedX, clampedY)
        };
    }

    float GetEdgeAngle()
    {
        return currentEdge switch
        {
            Edge.Bottom => 180f,
            Edge.Right  => -90f,
            Edge.Top    => 0f,
            Edge.Left   => 90f,
            _ => 0f
        };
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("FireBall hit Player");
        }
    }
}
