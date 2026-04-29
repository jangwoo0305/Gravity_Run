using UnityEngine;
using System;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] public float edgeOffset = 0.3f;
    [SerializeField] float cornerBlendDistance = 0.2f;
    [SerializeField] public float jumpPower = 6f;
    
    public float speed = 3f;
    Vector2 velocity; // 현재 이동 속도 (누적됨)
    Vector2 gravityDir; // 현재 중력 방향 (edge 기준) 즉, 케릭터가 끌려가야하는 방향
    private float gravityPower = 20f;
    bool isGrounded;
    bool isJumping;
    bool isFalling;
    private int jumpCount;
    private int maxJumpCount = 2;
    
    private Camera _cam;
    private ScreenEdgeBounds bounds;
    
    public Edge CurrentEdge { get; private set; } = Edge.Bottom;
    public Vector2 GravityDir => gravityDir;
    public float GravityPower => gravityPower;
    public bool IsGrounded => isGrounded;
    public bool IsJumping => isJumping;
    public bool IsFalling => isFalling;
    public Vector2 EdgeMoveDirection => EdgeMath.GetClockwiseMoveDir(CurrentEdge);

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
        
        // 케릭터를 화면 하단 중앙에 고정
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
        HandleJumpInput(); // 점프입력
        ApplyGravity(); // 중력처리
        ApplyMovement(); // 이동적용
        CheckCornerAndChangeGravity(); // Edge 전환
        ResolveGrounded(); // 착지판단
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

        // 지상에서는 중력 가속만 적용하지 않음
        if (isGrounded)
        {
            isFalling = false;
            return;
        }

        // 공중일 때만 중력 적용
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
        if (Input.GetKeyDown(KeyCode.Space) && jumpCount < maxJumpCount)
        {
            Jump();
        }
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

        // 중력 방향 속도 제거
        Vector2 gravityVelocity = Vector2.Dot(velocity, gravityDir) * gravityDir;
        velocity -= gravityVelocity;

        // 위치를 edge에 고정
        transform.position = bounds.SnapToEdge(CurrentEdge, (Vector2)transform.position);

        // 🔥 핵심: 착지 상태면 무조건 상태 정리
        jumpCount = 0;
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
        
        jumpCount++;
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
