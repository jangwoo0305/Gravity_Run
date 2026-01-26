using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fireball : MonoBehaviour
{
    [SerializeField] private float minspeed = 1f;
    [SerializeField] private float maxspeed = 6f;

    enum Edge
    {
        Bottom,
        Right,
        Top,
        Left
    }
    Edge currentEdge = Edge.Left;
    
    float minX, maxX, minY, maxY;
    private int edgeChangeCount; // 모서리 몇번 넘었나
    private const int EDGES_PER_LAP = 4; // 화면 한바퀴에는 4개의 모서리
    private int maxLapCount = 2; // 최대 도는 횟수 
    private float runSpeed;
    

    public void Init()
    {
        edgeChangeCount = 0; // 이전에 돌던 기록이 있으면 안됨, 재사용시에는 항상 0부터 시작.
        currentEdge = Edge.Left;
        
        runSpeed = Random.Range(minspeed, maxspeed);
        
        Camera cam = Camera.main;
        float z = Mathf.Abs(cam.transform.position.z);
        
        minX = cam.ViewportToWorldPoint(new Vector3(0,0,z)).x;
        maxX = cam.ViewportToWorldPoint(new Vector3(1,0,z)).x;
        minY = cam.ViewportToWorldPoint(new Vector3(0,0,z)).y;
        maxY = cam.ViewportToWorldPoint(new Vector3(0,1,z)).y;

        DetectInitialEdge();
    }
    
    void Update()
    {
        MoveAlongEdge();
        CheckCorner();
    }

    void MoveAlongEdge()
    {
        Vector3 dir = GetMoveDir();
        transform.position += dir * runSpeed * Time.deltaTime;
    }

    Vector3 GetMoveDir()
    {
        switch (currentEdge)
        {
            case Edge.Bottom: return Vector3.left;
            case Edge.Right: return Vector3.down;
            case Edge.Top: return Vector3.right;
            case Edge.Left: return Vector3.up;
        }
        return Vector3.zero;
    }

    void CheckCorner()
    {
        Vector3 pos =  transform.position;

        switch (currentEdge)
        {
            case Edge.Bottom:
                if (pos.x <= minX) ChangeEdge(Edge.Left);
                break;
            case Edge.Right:
                if (pos.y <= minY) ChangeEdge(Edge.Bottom);
                break;
            case Edge.Top:
                if(pos.x >= maxX) ChangeEdge(Edge.Right);
                break;
            case Edge.Left:
                if(pos.y >= maxY) ChangeEdge(Edge.Top);
                break;
        }
    }
    
    void ChangeEdge(Edge next)
    {
        edgeChangeCount++;
        // 두바퀴 체크
        if (edgeChangeCount >= EDGES_PER_LAP * maxLapCount)
        {
            gameObject.SetActive(false);
            return;
        }
        currentEdge = next;
        
        Vector3 pos =  transform.position;
        switch (currentEdge)
        {
            case Edge.Bottom: pos.y = minY; break;
            case Edge.Right: pos.x = maxX; break;
            case Edge.Top: pos.y = maxY; break;
            case Edge.Left: pos.x = minX; break;
        }
        transform.position = pos;
    }

    void DetectInitialEdge()
    {
        Vector3 pos =  transform.position;
        
        if(Mathf.Abs(pos.y - minY) < 0.05f) currentEdge = Edge.Bottom;
        else if(Mathf.Abs(pos.x - maxX) < 0.05f) currentEdge = Edge.Right;
        else if(Mathf.Abs(pos.y - maxY) < 0.05f) currentEdge = Edge.Top;
        else if (Mathf.Abs(pos.x - minX) < 0.05f) currentEdge = Edge.Left;
        else currentEdge = Edge.Left;
    }
}
