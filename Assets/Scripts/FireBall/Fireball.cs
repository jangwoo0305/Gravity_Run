using UnityEngine;
using Random = UnityEngine.Random;

public class Fireball : MonoBehaviour
{
    private Rigidbody2D rb;

    enum Edge
    {
        Bottom,
        Right,
        Top,
        Left
    }

    [SerializeField] private float minSpeed = 2f;
    [SerializeField] private float maxSpeed = 3f;

    private Edge currentEdge;
    private float speed;
    private float heightLevel;

    private float minX, maxX, minY, maxY;

    private int edgeChangeCount;
    private const int EDGES_PER_LAP = 4;
    private const int MAX_LAP = 1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // 🔹 Pool에서 호출 (생성 시 1회)
    public void Init(float height)
    {
        Camera cam = Camera.main;
        float z = Mathf.Abs(cam.transform.position.z);

        minX = cam.ViewportToWorldPoint(new Vector3(0, 0, z)).x;
        maxX = cam.ViewportToWorldPoint(new Vector3(1, 0, z)).x;
        minY = cam.ViewportToWorldPoint(new Vector3(0, 0, z)).y;
        maxY = cam.ViewportToWorldPoint(new Vector3(0, 1, z)).y;

        heightLevel = height;
        speed = Random.Range(minSpeed, maxSpeed);

        edgeChangeCount = 0;
        currentEdge = Edge.Left;

        // 🔥 생성 시에는 즉시 반영
        ApplyPosition(true);
        ApplyRotation(true);
    }

    private void FixedUpdate()
    {
        MoveClockwise();
        CheckEdgeChange();
    }
    
    private void OnEnable()
    {
        // 풀에서 꺼내질 때 항상 현재 Edge 기준으로 다시 적용
        ApplyPosition(true);
        ApplyRotation(true);
    }

    // =====================
    // 이동
    // =====================
    void MoveClockwise()
    {
        Vector2 pos = rb.position;
        float delta = speed * Time.fixedDeltaTime;

        switch (currentEdge)
        {
            case Edge.Bottom:
                pos.x -= delta;
                pos.y = minY + heightLevel;
                break;

            case Edge.Right:
                pos.y -= delta;
                pos.x = maxX - heightLevel;
                break;

            case Edge.Top:
                pos.x += delta;
                pos.y = maxY - heightLevel;
                break;

            case Edge.Left:
                pos.y += delta;
                pos.x = minX + heightLevel;
                break;
        }

        rb.MovePosition(pos);
    }

    // =====================
    // Edge 변경 체크
    // =====================
    void CheckEdgeChange()
    {
        Vector2 pos = rb.position;

        switch (currentEdge)
        {
            case Edge.Bottom:
                if (pos.x <= minX) ChangeEdge(Edge.Left);
                break;

            case Edge.Right:
                if (pos.y <= minY) ChangeEdge(Edge.Bottom);
                break;

            case Edge.Top:
                if (pos.x >= maxX) ChangeEdge(Edge.Right);
                break;

            case Edge.Left:
                if (pos.y >= maxY) ChangeEdge(Edge.Top);
                break;
        }
    }

    void ChangeEdge(Edge next)
    {
        edgeChangeCount++;

        if (edgeChangeCount >= EDGES_PER_LAP * MAX_LAP)
        {
            gameObject.SetActive(false);
            return;
        }

        currentEdge = next;

        // 🔹 런타임에서는 물리 기준 이동
        ApplyPosition(false);
        ApplyRotation(false);
    }

    // =====================
    // Edge 기준 위치 보정
    // immediate = true  : Init / OnEnable (즉시 반영)
    // immediate = false : Runtime (물리 프레임 반영)
    // =====================
    void ApplyPosition(bool immediate)
    {
        Vector2 pos = rb.position;

        switch (currentEdge)
        {
            case Edge.Bottom: pos.y = minY + heightLevel; break;
            case Edge.Right:  pos.x = maxX - heightLevel; break;
            case Edge.Top:    pos.y = maxY - heightLevel; break;
            case Edge.Left:   pos.x = minX + heightLevel; break;
        }

        if (immediate)
            rb.position = pos;       // 즉시 반영 (첫 프레임 보정)
        else
            rb.MovePosition(pos);    // 물리 프레임 기준 이동
    }

    // =====================
    // Edge 기준 스프라이트 회전
    // immediate = true  : Init / OnEnable
    // immediate = false : Edge 변경 시
    // =====================
    void ApplyRotation(bool immediate)
    {
        float angle = GetEdgeAngle();

        if (immediate)
            rb.rotation = angle;     // 즉시 회전
        else
            rb.MoveRotation(angle);  // 물리 프레임 회전
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