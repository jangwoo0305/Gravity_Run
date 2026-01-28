using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireBallPool : MonoBehaviour
{
    [SerializeField] private Fireball fireballPrefab;
    [SerializeField] private int poolSize = 20;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] PlayerMove player;
    
    
    private List<Fireball> pool = new List<Fireball>();

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

        float heightLevel = SetSpawnPosition(fb);
        fb.Init(heightLevel);
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

    float SetSpawnPosition(Fireball fb)
    {
        if (player == null)
        {
            Debug.LogError($"FireBallPool.SetSpawnPosition: player is null");
            return 0f;
        }
        
        Camera cam = Camera.main;
        float z = Mathf.Abs(cam.transform.position.z);
        
        float minX = cam.ViewportToWorldPoint(new Vector3(0, 0, z)).x;
        float groundY = cam.ViewportToWorldPoint(new Vector3(0, 0, z)).y
        + player.edgeOffset - 0.2f;
        
        fb.transform.position = new Vector3(minX, groundY, 0f);
        float minY = cam.ViewportToWorldPoint(new Vector3(0, 0, z)).y;
        return groundY - minY;
    }
}
