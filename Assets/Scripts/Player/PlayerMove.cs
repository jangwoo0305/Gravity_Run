using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] public float edgeOffset = 0.3f;
    [SerializeField] public float jumpHeight = 1.2f;
    [SerializeField] public float jumpDuration = 0.5f;
    
    bool isJumping = false;
    private float jumpOffset = 0f;
    
    public float speed = 3f;
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
        MoveEdge();
        if (Input.GetKeyDown(KeyCode.Space))
        {
            InputJump();
        }
    }

    public void InputJump()
    {
        if (isJumping) return;
        StartCoroutine(JumpCoroutine());
    }

    IEnumerator JumpCoroutine()
    {
        isJumping = true;
        _anim.SetBool("IsJumping", true);
        
        float halfTime = jumpDuration * 0.5f;
        float t = 0f;

        while (t < halfTime)
        {
            t += Time.deltaTime;
            float ratio  = t / halfTime;
            jumpOffset = Mathf.Lerp(0f, jumpHeight, ratio);
            yield return null;
        }

        t = 0f;

        while (t < halfTime)
        {
            t += Time.deltaTime;
            float ratio = t / halfTime;
            jumpOffset = Mathf.Lerp(jumpHeight, 0f, ratio);
            yield return null;
        }
        jumpOffset = 0f;
        isJumping = false;
        _anim.SetBool("IsJumping", false);
    }

    void ChangeEdge(Edge nextEdge)
    {
        currentEdge = nextEdge;
        UpdateVisualByEdge();
    }
    
    public void MoveEdge()
    {
        Vector3 pos = transform.position;
        float move = speed * Time.deltaTime;

        switch (currentEdge)
        {
            case Edge.Bottom:
                pos.x += move;
                pos.y = minY;

                if (pos.x > maxX)
                {
                    pos.x = maxX;
                    ChangeEdge(Edge.Right);
                }
                break;
            
            case Edge.Right:
                pos.y += move;
                pos.x = maxX;

                if (pos.y > maxY)
                {
                    pos.y = maxY;
                    ChangeEdge(Edge.Top);
                }
                break;
            
            case Edge.Top:
                pos.x -= move;
                pos.y = maxY;

                if (pos.x < minX)
                {
                    pos.x = minX;
                    ChangeEdge(Edge.Left);
                }
                break;
            
            case Edge.Left:
                pos.y -= move;
                pos.x = minX;

                if (pos.y < minY)
                {
                    pos.y = minY;
                    ChangeEdge(Edge.Bottom);
                }
                break;
        }

        pos += GetJumpDirection() * jumpOffset;
        transform.position = pos;
    }

    Vector3 GetJumpDirection()
    {
        switch (currentEdge)
        {
            case Edge.Bottom: return Vector3.up;
            case Edge.Right: return Vector3.left;
            case Edge.Top: return Vector3.down;
            case Edge.Left: return Vector3.right;
        }
        return Vector3.zero;
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
    
}
