using UnityEngine;
using System.Collections.Generic;

public class FireSmokeSpawner : MonoBehaviour
{
    [Header("Smoke Prefab")]
    public GameObject smokePrefab;

    [Header("Spawn Settings")]
    public float spawnInterval = 0.8f;
    public int poolSize = 20;
    public Vector2 spawnOffset = new Vector2(0.15f, 0.05f);

    float timer;

    Queue<GameObject> pool = new();

    void Start()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject smoke = Instantiate(smokePrefab, transform);
            smoke.SetActive(false);
            pool.Enqueue(smoke);
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnSmoke();
        }
    }

    void SpawnSmoke()
    {
        if (pool.Count == 0) return;

        GameObject smoke = pool.Dequeue();

        smoke.transform.localPosition = new Vector3(
            Random.Range(-spawnOffset.x, spawnOffset.x),
            Random.Range(0f, spawnOffset.y),
            0f
        );

        smoke.transform.localScale = Vector3.one;
        smoke.SetActive(true);

        pool.Enqueue(smoke);
    }
}
