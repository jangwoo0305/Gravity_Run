using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] public float edgeOffset = 0.3f;
    [SerializeField] float cornerBlendDistance = 0.2f;
    [SerializeField] public float jumpPower = 6f;
    
    public float speed = 3f;
    Vector3 velocity; // 현재 이동 속도 (누적됨)
    Vector3 gravityDir; // 현재 중력 방향 (edge 기준) 즉, 케릭터가 끌려가야하는 방향
    private float gravityPower = 20f;
    bool isGrounded;
    bool isJumping;
    private int jumpCount;
    private int maxJumpCount = 2;
    
    private SpriteRenderer _spriteRend;
    private Camera _cam;
    private Animator _anim;
    float minX, minY, maxX, maxY;
    
    public Edge CurrentEdge { get; private set; } = Edge.Bottom;

    void Awake()
    {
        _spriteRend = GetComponent<SpriteRenderer>();
        _cam = Camera.main;
        _anim = GetComponent<Animator>();
    }

    void Start()
    {
        Vector3 bottomLeft = _cam.ViewportToWorldPoint(new Vector3(0, 0, _cam.nearClipPlane));
        Vector3 topRight = _cam.ViewportToWorldPoint(new Vector3(1, 1, _cam.nearClipPlane));
        
        minX = bottomLeft.x + edgeOffset;
        minY = bottomLeft.y + edgeOffset;
        maxX = topRight.x - edgeOffset;
        maxY = topRight.y - edgeOffset;

        CurrentEdge = Edge.Bottom;
        gravityDir = Vector3.down;
        
        Vector3 startpos = transform.position;
        startpos.x = (minX + maxX) * 0.5f;
        startpos.y = minY;
        transform.position = startpos;
        
        velocity = Vector3.zero;
        isGrounded = true;
        isJumping = false;
        jumpCount = 0;
        
        _anim.SetBool("IsJumping", false);
        _anim.SetBool("IsFalling", false);
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
        Vector3 newGravityDir = GetBlendedGravityDir();

        if (newGravityDir != gravityDir)
        {
            Quaternion rot = Quaternion.FromToRotation(gravityDir, newGravityDir);
            velocity = rot * velocity;
            gravityDir = newGravityDir;
            transform.up = -gravityDir;
        }

        // 지상에서는 중력 가속만 적용하지 않음
        if (isGrounded)
            return;

        // 공중일 때만 중력 적용
        velocity += gravityDir * gravityPower * Time.deltaTime;
        
        float fallSpeed = Vector3.Dot(velocity, gravityDir);
        if (fallSpeed > 0.1f)
        {
            _anim.SetBool("IsJumping", false);
            _anim.SetBool("IsFalling", true);
        }
    }

    void ApplyMovement()
    {
        Vector3 edgeMove = GetEdgeMoveDirection() * speed * Time.deltaTime;
        transform.position += edgeMove;
        
        transform.position += velocity * Time.deltaTime;

        UpdateVisualFlip();
    }

    void HandleJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && jumpCount < maxJumpCount)
        {
            Jump();
        }
    }

    bool IsGrounded()
    {
        Vector3 pos = transform.position;

        switch (CurrentEdge)
        {
            case Edge.Bottom:
                return pos.y <= minY;
            case Edge.Right:
                return pos.x >= maxX;
            case Edge.Top:
                return pos.y >= maxY;
            case Edge.Left: 
                return pos.x <= minX;
        }
        return false;
    }

    void ResolveGrounded()
    {
        if (isJumping && Vector3.Dot(velocity, gravityDir) < 0f)
            return;
        
        isGrounded = IsGrounded();
        if (!isGrounded)
            return;

        // 중력 방향 속도 제거
        Vector3 gravityVelocity = Vector3.Project(velocity, gravityDir);
        velocity -= gravityVelocity;

        // 위치를 edge에 고정
        Vector3 pos = transform.position;
        switch (CurrentEdge)
        {
            case Edge.Bottom: pos.y = minY; break;
            case Edge.Right:  pos.x = maxX; break;
            case Edge.Top:    pos.y = maxY; break;
            case Edge.Left:   pos.x = minX; break;
        }
        transform.position = pos;

        // 🔥 핵심: 착지 상태면 무조건 상태 정리
        jumpCount = 0;
        isJumping = false;

        _anim.SetBool("IsJumping", false);
        _anim.SetBool("IsFalling", false);
    }

    void ChangeEdge(Edge nextEdge)
    {
        CurrentEdge = nextEdge;

        // 위치 강제 고정
        Vector3 pos = transform.position;

        switch (CurrentEdge)
        {
            case Edge.Bottom:
                pos.y = minY;
                break;
            case Edge.Right:
                pos.x = maxX;
                break;
            case Edge.Top:
                pos.y = maxY;
                break;
            case Edge.Left:
                pos.x = minX;
                break;
        }

        transform.position = pos;
        
        UpdateVisualFlip();
        ForceGroundAfterEdgeChange();
        ResolveGrounded();
        

        // if (isJumping && !isGrounded)
        // {
        //     _anim.Play("Jump", 0 ,0f);
        // }
    }
    Vector3 GetEdgeMoveDirection()
    {
        switch (CurrentEdge)
        {
            case Edge.Bottom: 
                return Vector3.right;
            case Edge.Right: 
                return Vector3.up;
            case Edge.Top: 
                return Vector3.left;
            case Edge.Left: 
                return Vector3.down;
        }
        return Vector3.zero;
    }

    void CheckCornerAndChangeGravity()
    {
        Vector3 pos = transform.position;

        if (CurrentEdge == Edge.Bottom && pos.x >= maxX)
        {
            ChangeEdge(Edge.Right);
        }
        else if (CurrentEdge == Edge.Right && pos.y >= maxY)
        {
            ChangeEdge(Edge.Top);
        }
        else if (CurrentEdge == Edge.Top && pos.x <= minX)
        {
            ChangeEdge(Edge.Left);
        }
        else if (CurrentEdge == Edge.Left && pos.y <= minY)
        {
            ChangeEdge(Edge.Bottom);
        }
    }

    Vector3 GetBlendedGravityDir()
    {
        Vector3 pos = transform.position;

        switch (CurrentEdge)
        {
            case Edge.Bottom:
                if (pos.x > maxX - cornerBlendDistance)
                {
                    float t = Mathf.InverseLerp(maxX - cornerBlendDistance,maxX, pos.x);
                    return Vector3.Lerp(Vector3.down, Vector3.right, t).normalized;
                }
                return  Vector3.down;
            
            case Edge.Right:
                if (pos.y > maxY - cornerBlendDistance)
                {
                    float t = Mathf.InverseLerp(maxY - cornerBlendDistance, maxY, pos.y);
                    return Vector3.Lerp(Vector3.right, Vector3.up, t).normalized;
                }
                return  Vector3.right;
            
            case Edge.Top:
                if (pos.x < minX + cornerBlendDistance)
                {
                    float t = Mathf.InverseLerp(minX + cornerBlendDistance, minX, pos.x);
                    return Vector3.Lerp(Vector3.up, Vector3.left, t).normalized;
                }
                return  Vector3.up;
            
            case Edge.Left:
                if (pos.y < minY + cornerBlendDistance)
                {
                    float t = Mathf.InverseLerp(minY + cornerBlendDistance, minY, pos.y);
                    return Vector3.Lerp(Vector3.left, Vector3.down, t).normalized;
                }
                return  Vector3.left;
        }
        return gravityDir;
    }


    void Jump()
    {
        isGrounded = false;
        isJumping = true;
        
        Vector3  gravityVelocity = Vector3.Project(velocity, gravityDir);
        velocity -= gravityVelocity;
        
        velocity += -gravityDir * jumpPower;
        
        jumpCount++;
        
        _anim.SetBool("IsJumping", true);
    }

    void UpdateVisualFlip()
    {
        Vector3 moveDir = GetEdgeMoveDirection();

        // 기준: 캐릭터 로컬 right가 진행 방향을 바라보도록
        float dot = Vector3.Dot(transform.right, moveDir);

        _spriteRend.flipX = dot < 0f;
    }
    
    void ForceGroundAfterEdgeChange()
    {

        if (!isGrounded)
            return;
        
        Vector3 gravityVelocity = Vector3.Project(velocity, gravityDir);
        velocity -= gravityVelocity;
    }
    
    public Vector3 GravityDir => gravityDir;
    public float GravityPower => gravityPower;

    public float GetMaxJumpHeight()
    {
        return (jumpPower * jumpPower) / (2f * GravityPower);
    }
}
