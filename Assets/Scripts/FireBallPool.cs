using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireBallPool : MonoBehaviour
{
    [SerializeField] private Fireball fireballPrefab;
    [SerializeField] private int poolSize = 20;
    [SerializeField] private float minspawnInterval = 1f;
    [SerializeField] private float maxspawnInterval = 5f;
    
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
            float waitTime = Random.Range(minspawnInterval, maxspawnInterval);
            yield return new WaitForSeconds(waitTime);
            SpawnFireball();
        }
    }

    void SpawnFireball()
    {
        Fireball fb = GetAvailableFireball();
        if (fb == null) return;

        SetSpawnPosition(fb);
        fb.Init();
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

    void SetSpawnPosition(Fireball fb)
    {
        Camera cam = Camera.main;
        float z = Mathf.Abs(cam.transform.position.z);
        
        float minX = cam.ViewportToWorldPoint(new Vector3(0, 0, z)).x;
        float centerY = cam.ViewportToWorldPoint(new Vector3(0, 0.5f, z)).y;
        
        fb.transform.position = new Vector3(minX, centerY, 0f);
    }
}
