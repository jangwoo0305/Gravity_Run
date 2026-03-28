using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireBallPool : MonoBehaviour
{
    [SerializeField] private Fireball fireballPrefab;
    [SerializeField] private int poolSize = 20;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private PlayerMove player;
    
    
    private readonly List<Fireball> pool = new List<Fireball>();

    void Start()
    {
        for (int i = 0; i < poolSize; i++)
        {
            Fireball fb = Instantiate(fireballPrefab, transform);
            fb.gameObject.SetActive(false);
            pool.Add(fb);
        }

        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnFireball();
        }
    }

    void SpawnFireball()
    {
        Fireball fb = GetAvailableFireball();
        if (fb == null) return;

        if (player == null)
        {
            Debug.LogError("FireBallPool: player is null");
            return;
        }
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("FireBallPool: Camera.main is null");
            return;
        }

        // 플레이어가 있는 Edge의 "이전 Edge"에서 스폰
        // Top -> Right, Right -> Bottom, Bottom -> Left, Left -> Top.
        Edge spawnEdge = GetPreviousEdge(player.CurrentEdge);
        Vector2 spawnPos = GetFireballStartCornerCounterClockwise(cam, spawnEdge, player.edgeOffset);

        // 이전 풀 오브젝트 포즈가 1프레임 보이지 않도록, 비활성 상태에서 먼저 Init 후 활성화
        fb.gameObject.SetActive(false);
        fb.Init(spawnPos, spawnEdge, player.edgeOffset);
        fb.gameObject.SetActive(true);
    }

    static Edge GetPreviousEdge(Edge current)
    {
        return current switch
        {
            Edge.Top => Edge.Right,
            Edge.Right => Edge.Bottom,
            Edge.Bottom => Edge.Left,
            Edge.Left => Edge.Top,
            _ => Edge.Bottom
        };
    }

    // Fireball은 반시계 방향으로 이동:
    // Bottom: 오른쪽 -> 왼쪽, Left: 아래 -> 위, Top: 왼쪽 -> 오른쪽, Right: 위 -> 아래.
    static Vector2 GetFireballStartCornerCounterClockwise(Camera cam, Edge edge, float edgeOffset)
    {
        float z = cam.nearClipPlane;
        Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, z));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1, 1, z));

        float minX = bottomLeft.x + edgeOffset;
        float minY = bottomLeft.y + edgeOffset;
        float maxX = topRight.x - edgeOffset;
        float maxY = topRight.y - edgeOffset;

        return edge switch
        {
            Edge.Bottom => new Vector2(maxX, minY),
            Edge.Left => new Vector2(minX, minY),
            Edge.Top => new Vector2(minX, maxY),
            Edge.Right => new Vector2(maxX, maxY),
            _ => new Vector2(maxX, minY)
        };
    }

    Fireball GetAvailableFireball()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if(!pool[i].gameObject.activeSelf)
                return pool[i];
        }
        return null;
    }
}
