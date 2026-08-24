using UnityEngine;

public partial class FireBallPool
{
    private void SpawnFireball(Edge spawnEdge, float moveSpeed, float laneOffset)
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

        ScreenEdgeBounds b = ScreenEdgeBounds.FromCamera(cam, player.edgeOffset);
        Vector2 spawnPos = EdgeMath.GetFireballStartCorner(b, spawnEdge);

        // 이전 풀 오브젝트 포즈가 1프레임 보이지 않도록, 비활성 상태에서 먼저 Init 후 활성화
        fb.gameObject.SetActive(false);
        fb.Init(spawnPos, spawnEdge, player.edgeOffset, moveSpeed, laneOffset);
        fb.gameObject.SetActive(true);
    }

    private Fireball GetAvailableFireball()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].gameObject.activeSelf)
                return pool[i];
        }
        return null;
    }

    private float GetProjectileThickness()
    {
        if (fireballPrefab == null)
            return 0.45f;

        SpriteRenderer projectileRenderer = fireballPrefab.GetComponent<SpriteRenderer>();
        if (projectileRenderer == null || projectileRenderer.sprite == null)
            return 0.45f;

        Vector2 spriteSize = projectileRenderer.sprite.bounds.size;
        Vector3 scale = projectileRenderer.transform.lossyScale;
        float width = Mathf.Abs(spriteSize.x * scale.x);
        float height = Mathf.Abs(spriteSize.y * scale.y);
        return Mathf.Max(0.1f, Mathf.Min(width, height));
    }
}
