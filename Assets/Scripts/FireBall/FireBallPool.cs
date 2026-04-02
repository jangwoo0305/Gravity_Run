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
        Edge spawnEdge = EdgeMath.GetPreviousEdgeClockwise(player.CurrentEdge);
        ScreenEdgeBounds b = ScreenEdgeBounds.FromCamera(cam, player.edgeOffset);
        Vector2 spawnPos = EdgeMath.GetStartCornerForCounterClockwiseMotion(b, spawnEdge);

        // 이전 풀 오브젝트 포즈가 1프레임 보이지 않도록, 비활성 상태에서 먼저 Init 후 활성화
        fb.gameObject.SetActive(false);
        fb.Init(spawnPos, spawnEdge, player.edgeOffset);
        fb.gameObject.SetActive(true);
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
