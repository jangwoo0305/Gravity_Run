using UnityEngine;
using System;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] public float edgeOffset = 0.3f;
    [SerializeField] float cornerBlendDistance = 0.2f;
    [SerializeField] public float jumpPower = 6f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField, Min(0.01f)] private float gravityPower = 20f;
    [SerializeField, Min(1)] private int maxJumpCount = 2;
    
    public float speed = 3f;
    Vector2 velocity; // 점프와 낙하에 사용하는 현재 속도
    Vector2 gravityDir; // 현재 벽이 만드는 중력 방향
    bool isGrounded;
    bool isJumping;
    bool isFalling;
    private int jumpCount;
    private float lastGroundedTime;
    private float lastJumpPressedTime = -999f;
    
    private Camera _cam;
    private ScreenEdgeBounds bounds;
    
    public Edge CurrentEdge { get; private set; } = Edge.Bottom;
    public Vector2 GravityDir => gravityDir;
    public float GravityPower => gravityPower;
    public bool IsGrounded => isGrounded;
    public bool IsJumping => isJumping;
    public bool IsFalling => isFalling;
    public Vector2 EdgeMoveDirection => EdgeMath.GetPlayerMoveDir(CurrentEdge);

    public int CurrentLap { get; private set; }
    public event Action<int> LapChanged;

    void Awake()
    {
        _cam = Camera.main;
    }

    void Start()
    {
        if (_cam == null)
        {
            Debug.LogError("PlayerMove: Camera.main is null");
            enabled = false;
            return;
        }

        bounds = ScreenEdgeBounds.FromCamera(_cam, edgeOffset);

        CurrentEdge = Edge.Bottom;
        CurrentLap = 0;
        gravityDir = Vector2.down;
        
        // 캐릭터를 화면 하단 중앙에 고정
        Vector3 startpos = transform.position;
        startpos.x = (bounds.MinX + bounds.MaxX) * 0.5f;
        startpos.y = bounds.MinY;
        transform.position = startpos;
        
        velocity = Vector2.zero;
        isGrounded = true;
        isJumping = false;
        isFalling = false;
        jumpCount = 0;
    }

    
    void Update()
    {
        HandleJumpInput(); // 점프 입력
        ApplyGravity(); // 현재 벽 기준 중력 처리
        ApplyMovement(); // 벽 방향 자동 이동 + 점프/낙하 속도 적용
        CheckCornerAndChangeGravity(); // 코너에서 다음 벽으로 전환
        ResolveGrounded(); // 착지 판단
    }

    void ApplyGravity()
    {
        Vector2 newGravityDir = EdgeMath.GetBlendedGravityDir(bounds, CurrentEdge, cornerBlendDistance, (Vector2)transform.position);

        if (newGravityDir != gravityDir)
        {
            float angle = Vector2.SignedAngle(gravityDir, newGravityDir);
            velocity = (Vector2)(Quaternion.Euler(0f, 0f, angle) * (Vector3)velocity);
            gravityDir = newGravityDir;
            transform.up = (Vector3)(-gravityDir);
        }

        // 벽에 붙어 달리는 동안에는 중력 가속을 누적하지 않음
        if (isGrounded)
        {
            isFalling = false;
            return;
        }

        // 공중일 때만 현재 벽 방향의 중력을 적용
        velocity += gravityDir * gravityPower * Time.deltaTime;
        
        float fallSpeed = Vector2.Dot(velocity, gravityDir);
        isFalling = fallSpeed > 0.1f;
    }

    void ApplyMovement()
    {
        Vector3 edgeMove = (Vector3)EdgeMoveDirection * speed * Time.deltaTime;
        transform.position += edgeMove;
        
        transform.position += (Vector3)(velocity * Time.deltaTime);
    }

    void HandleJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            lastJumpPressedTime = Time.time;

        if (Time.time - lastJumpPressedTime <= jumpBufferTime && CanJump())
        {
            Jump();
            lastJumpPressedTime = -999f;
        }
    }

    bool CanJump()
    {
        if (jumpCount == 0 && (isGrounded || Time.time - lastGroundedTime <= coyoteTime))
            return true;

        if (jumpCount > 0 && jumpCount < maxJumpCount)
            return true;

        return false;
    }

    bool CheckGrounded()
    {
        return bounds.IsBeyondEdge(CurrentEdge, (Vector2)transform.position, 0f);
    }

    void ResolveGrounded()
    {
        if (isJumping && Vector2.Dot(velocity, gravityDir) < 0f)
            return;
        
        isGrounded = CheckGrounded();
        if (!isGrounded)
            return;

        // 착지하면 중력 방향 속도를 제거
        Vector2 gravityVelocity = Vector2.Dot(velocity, gravityDir) * gravityDir;
        velocity -= gravityVelocity;

        // 현재 벽 위로 위치를 고정
        transform.position = bounds.SnapToEdge(CurrentEdge, (Vector2)transform.position);

        // 착지 상태면 점프 관련 상태를 정리
        jumpCount = 0;
        lastGroundedTime = Time.time;
        isJumping = false;
        isFalling = false;
    }

    void ChangeEdge(Edge nextEdge)
    {
        Edge prevEdge = CurrentEdge;
        CurrentEdge = nextEdge;

        // 한 바퀴( Bottom -> Right -> Top -> Left -> Bottom )가 끝나는 지점
        // 즉 Left에서 Bottom으로 돌아오면 랩 +1
        if (prevEdge == Edge.Left && nextEdge == Edge.Bottom)
        {
            CurrentLap++;
            LapChanged?.Invoke(CurrentLap);
        }

        // 위치 강제 고정
        transform.position = bounds.SnapToEdge(CurrentEdge, (Vector2)transform.position);
        ForceGroundAfterEdgeChange();
        ResolveGrounded();
        

        // if (isJumping && !isGrounded)
        // {
        //     _anim.Play("Jump", 0 ,0f);
        // }
    }
    void CheckCornerAndChangeGravity()
    {
        Vector3 pos = transform.position;

        if (CurrentEdge == Edge.Bottom && pos.x >= bounds.MaxX)
        {
            ChangeEdge(Edge.Right);
        }
        else if (CurrentEdge == Edge.Right && pos.y >= bounds.MaxY)
        {
            ChangeEdge(Edge.Top);
        }
        else if (CurrentEdge == Edge.Top && pos.x <= bounds.MinX)
        {
            ChangeEdge(Edge.Left);
        }
        else if (CurrentEdge == Edge.Left && pos.y <= bounds.MinY)
        {
            ChangeEdge(Edge.Bottom);
        }
    }


    void Jump()
    {
        isGrounded = false;
        isJumping = true;
        isFalling = false;
        
        Vector2 gravityVelocity = Vector2.Dot(velocity, gravityDir) * gravityDir;
        velocity -= gravityVelocity;
        
        velocity += -gravityDir * jumpPower;
        
        jumpCount = Mathf.Max(jumpCount + 1, 1);
    }
    
    void ForceGroundAfterEdgeChange()
    {

        if (!isGrounded)
            return;
        
        Vector2 gravityVelocity = Vector2.Dot(velocity, gravityDir) * gravityDir;
        velocity -= gravityVelocity;
    }

    public float GetMaxJumpHeight()
    {
        return (jumpPower * jumpPower) / (2f * GravityPower);
    }
}
