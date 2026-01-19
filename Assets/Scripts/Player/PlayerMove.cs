using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] public float edgeOffset = 0.3f;
    [SerializeField] float cornerBlendDistance = 0.5f;
    [SerializeField] public float jumpPower = 8f;
    
    public float speed = 3f;
    Vector3 velocity; // 현재 이동 속도 (누적됨)
    Vector3 gravityDir; // 현재 중력 방향 (edge 기준) 즉, 케릭터가 끌려가야하는 방향
    private float gravityPower = 20f;
    bool isGrounded;
    bool isJumping;
    
    private SpriteRenderer _spriteRend;
    Camera _cam;
    Animator _anim;
    float minX, minY, maxX, maxY;
    

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
        
        gravityDir = Vector3.down;
        velocity = Vector3.zero;
    }

    private enum Edge
    {
        Bottom,
        Right,
        Top,
        Left
    }
    private Edge currentEdge = Edge.Bottom;
    
    void Update()
    {
        ResolveGrounded(); // 1. 바닥 판단
        HandleJumpInput();
        ApplyGravity(); // 2. 공중일 때만 중력
        ApplyMovement(); // 3. 이동
        CheckCornerAndChangeGravity();
    }

    void ApplyGravity()
    {
        Vector3 newGravityDir = GetBlendedGravityDir();

        if (newGravityDir != gravityDir)
        {
            Quaternion rot = Quaternion.FromToRotation(gravityDir, newGravityDir);
            velocity = rot * velocity;
        }
        
        gravityDir = newGravityDir;
        
        if (isGrounded)
            return;

        velocity += gravityDir * gravityPower * Time.deltaTime;
    }

    void ApplyMovement()
    {
        Vector3 edgeMove = GetEdgeMoveDirection() * speed * Time.deltaTime;
        transform.position += edgeMove;
        
        transform.position += velocity * Time.deltaTime;
    }

    void HandleJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }
    }

    bool IsGrounded()
    {
        Vector3 pos = transform.position;

        switch (currentEdge)
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
        bool wasGrounded = isGrounded;
        isGrounded = IsGrounded();
        if (!isGrounded) return;
        
        Vector3 gravityVelocity = Vector3.Project(velocity, gravityDir);
        velocity -= gravityVelocity;
        
        Vector3 pos = transform.position;
        switch (currentEdge)
        {
            case Edge.Bottom: pos.y = minY; break;
            case Edge.Right: pos.x = maxX;break;
            case Edge.Top: pos.y = maxY; break;
            case Edge.Left: pos.x = minX; break;
        }
        transform.position = pos;

        if (!wasGrounded)
        {
            isJumping = false;
            _anim.SetBool("IsJumping", false);
        }
    }

    void ChangeEdge(Edge nextEdge)
    {
        currentEdge = nextEdge;

        // 위치 강제 고정
        Vector3 pos = transform.position;

        switch (currentEdge)
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

        UpdateVisualByEdge();
    }
    Vector3 GetEdgeMoveDirection()
    {
        switch (currentEdge)
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

        if (currentEdge == Edge.Bottom && pos.x >= maxX)
        {
            ChangeEdge(Edge.Right);
        }
        else if (currentEdge == Edge.Right && pos.y >= maxY)
        {
            ChangeEdge(Edge.Top);
        }
        else if (currentEdge == Edge.Top && pos.x <= minX)
        {
            ChangeEdge(Edge.Left);
        }
        else if (currentEdge == Edge.Left && pos.y <= minY)
        {
            ChangeEdge(Edge.Bottom);
        }
    }

    Vector3 GetBlendedGravityDir()
    {
        Vector3 pos = transform.position;

        switch (currentEdge)
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


    public void UpdateVisualByEdge()
    {
        float zRotation = 0f;
        
        switch (currentEdge)
        { 
            case Edge.Bottom:
                zRotation = 0f;    
                break;
            
            case Edge.Right:
                zRotation = 90f;
                break;  
            
            case Edge.Top:
                zRotation = 180f;
                break;
            
            case Edge.Left:
                zRotation = 270f;
                break;
        }
        transform.localRotation = Quaternion.Euler(0f, 0f, zRotation);
    }

    void Jump()
    {
        isGrounded = false;
        isJumping = true;
        
        velocity += -gravityDir * jumpPower;
        
        _anim.SetBool("IsJumping", true);
    }
    
}
