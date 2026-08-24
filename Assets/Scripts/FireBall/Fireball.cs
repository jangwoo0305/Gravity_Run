using UnityEngine;

public class Fireball : MonoBehaviour
{
    private Rigidbody2D rb;

    [SerializeField] private float edgeOffset = 0.3f; // 기본값 (풀에서 플레이어 값으로 덮어쓸 수 있음)
    [SerializeField] private float spriteRotationOffset = 180f;
    
    private Edge currentEdge;
    private float speed;
    private float laneOffset;

    private ScreenEdgeBounds bounds;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // fb.Init(spawnPos, spawnEdge, player.edgeOffset);
    // 🔹 Pool에서 호출 (생성 시 1회)
    public void Init(
        Vector2 spawnWorldPos,
        Edge startEdge,
        float edgeOffsetWorld,
        float moveSpeed,
        float laneOffsetWorld = 0f)
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        edgeOffset = edgeOffsetWorld;
        bounds = ScreenEdgeBounds.FromCamera(cam, edgeOffset);

        speed = Mathf.Max(0.1f, moveSpeed);
        laneOffset = Mathf.Max(0f, laneOffsetWorld);

        currentEdge = startEdge;

        float angle = GetVisualAngle(currentEdge);
        Vector2 clampedSpawn = ClampToLane(spawnWorldPos, startEdge);

        // 풀링으로 Enable/Disable 될 때 이전 포즈가 1프레임 보이지 않도록 Transform/Rigidbody2D 둘 다 세팅
        transform.SetPositionAndRotation(clampedSpawn, Quaternion.Euler(0f, 0f, angle));
        rb.position = clampedSpawn;
        rb.rotation = angle;

    }

    private void FixedUpdate()
    {
        MoveAlongEdge();
        DeactivateAtEdgeEnd();
    }
    

    // =====================
    // 이동
    // =====================
    void MoveAlongEdge()
    {
        Vector2 pos = rb.position;
        float delta = speed * Time.fixedDeltaTime;

        switch (currentEdge)
        {
            // Bottom: 왼쪽으로 이동 (플레이어 진행 방향과 반대)
            case Edge.Bottom:
                pos.x -= delta;
                pos.y = bounds.MinY + laneOffset;
                break;

            // Right: 아래로 이동 (플레이어 진행 방향과 반대)
            case Edge.Right:
                pos.y -= delta;
                pos.x = bounds.MaxX - laneOffset;
                break;

            // Top: 오른쪽으로 이동 (플레이어 진행 방향과 반대)
            case Edge.Top:
                pos.x += delta;
                pos.y = bounds.MaxY - laneOffset;
                break;

            // Left: 위로 이동 (플레이어 진행 방향과 반대)
            case Edge.Left:
                pos.y += delta;
                pos.x = bounds.MinX + laneOffset;
                break;
        }

        rb.MovePosition(pos);
    }

    void DeactivateAtEdgeEnd()
    {
        Vector2 pos = rb.position;
        float epsilon = 0.01f;

        switch (currentEdge)
        {
            case Edge.Bottom:
                if (pos.x < bounds.MinX - epsilon)
                    gameObject.SetActive(false);
                break;

            case Edge.Left:
                if (pos.y > bounds.MaxY + epsilon)
                    gameObject.SetActive(false);
                break;

            case Edge.Top:
                if (pos.x > bounds.MaxX + epsilon)
                    gameObject.SetActive(false);
                break;

            case Edge.Right:
                if (pos.y < bounds.MinY - epsilon)
                    gameObject.SetActive(false);
                break;
        }
    }

    private float GetVisualAngle(Edge edge)
    {
        return EdgeMath.GetAngleForFireballMotion(edge) + spriteRotationOffset;
    }

    Vector2 ClampToLane(Vector2 worldPos, Edge edge)
    {
        float clampedX = Mathf.Clamp(worldPos.x, bounds.MinX, bounds.MaxX);
        float clampedY = Mathf.Clamp(worldPos.y, bounds.MinY, bounds.MaxY);

        return edge switch
        {
            Edge.Bottom => new Vector2(clampedX, bounds.MinY + laneOffset),
            Edge.Right => new Vector2(bounds.MaxX - laneOffset, clampedY),
            Edge.Top => new Vector2(clampedX, bounds.MaxY - laneOffset),
            Edge.Left => new Vector2(bounds.MinX + laneOffset, clampedY),
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
        GameSession.ReportGameOver();
    }
}
