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
    
    private Edge currentEdge;
    private float speed;
    private float heightLevel;

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
    public void Init(float height, Edge startEdge)
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
        currentEdge = startEdge;

        // 🔥 생성 시에는 즉시 반영
        ApplyPosition(true);
        ApplyRotation(true);

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
        waveTime += Time.fixedDeltaTime;
        MoveClockwise();
        CheckEdgeChange();
    }
    

    // =====================
    // 이동
    // =====================
    void MoveClockwise()
    {
        Vector2 pos = rb.position;
        float delta = speed * Time.fixedDeltaTime;
        float waveOffset = useWave ? Mathf.Sin(waveTime * waveFrequency) * waveAmplitude : 0f;

        switch (currentEdge)
        {
            // Bottom: move RIGHT
            case Edge.Bottom:
                pos.x += delta;
                pos.y = minY + waveOffset;
                break;

            // Right: move UP
            case Edge.Right:
                pos.y += delta;
                pos.x = maxX + waveOffset;
                break;

            // Top: move LEFT
            case Edge.Top:
                pos.x -= delta;
                pos.y = maxY + waveOffset;
                break;

            // Left: move DOWN
            case Edge.Left:
                pos.y -= delta;
                pos.x = minX + waveOffset;
                break;
        }

        rb.MovePosition(pos);
    }

    // =====================
    // Edge 변경 체크 (Clockwise)
    // =====================
    void CheckEdgeChange()
    {
        Vector2 pos = rb.position;
        float epsilon = 0.01f;

        switch (currentEdge)
        {
            // Bottom → Right (when reaching maxX)
            case Edge.Bottom:
                if (pos.x > maxX + epsilon)
                    ChangeEdge(Edge.Right);
                break;

            // Right → Top (when reaching maxY)
            case Edge.Right:
                if (pos.y > maxY + epsilon)
                    ChangeEdge(Edge.Top);
                break;

            // Top → Left (when reaching minX)
            case Edge.Top:
                if (pos.x < minX - epsilon)
                    ChangeEdge(Edge.Left);
                break;

            // Left → Bottom (when reaching minY)
            case Edge.Left:
                if (pos.y < minY - epsilon)
                    ChangeEdge(Edge.Bottom);
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

        // 🔥 Spawn exactly at screen corners based on currentEdge
        switch (currentEdge)
        {
            case Edge.Bottom:
                pos = new Vector2(minX, minY);
                break;

            case Edge.Right:
                pos = new Vector2(maxX, minY);
                break;

            case Edge.Top:
                pos = new Vector2(maxX, maxY);
                break;

            case Edge.Left:
                pos = new Vector2(minX, maxY);
                break;
        }

        if (immediate)
            rb.position = pos;
        else
            rb.MovePosition(pos);
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