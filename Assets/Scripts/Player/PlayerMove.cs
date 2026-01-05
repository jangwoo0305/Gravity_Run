using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] public float edgeOffset = 0.3f;
    public float speed = 3f;
    private SpriteRenderer _spriteRend;
    Camera _cam;
    float minX, minY, maxX, maxY;
    


    void Awake()
    {
        _spriteRend = GetComponent<SpriteRenderer>();
        _cam = Camera.main;
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
        transform.position = pos;
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
