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

        if (player == null)
        {
            Debug.LogError("FireBallPool: player is null");
            return;
        }
        // Spawn from the exact corner of the player's current edge
        float height = 0f;
        Edge spawnEdge = player.CurrentEdge;

        // Activate first (in case OnEnable resets state)
        fb.gameObject.SetActive(true);
        fb.Init(height, spawnEdge);
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
