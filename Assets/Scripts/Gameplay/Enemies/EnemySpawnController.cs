using System.Collections;
using UnityEngine;

public class EnemySpawnController : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawnEntry
    {
        public GameObject prefab;
        public Transform spawnPoint;
        public bool allowRespawn = false;

        [HideInInspector] public GameObject instance;
    }

    [Header("Entries")]
    public EnemySpawnEntry[] entries;

    [Header("Options")]
    public bool spawnOnStart = true;
    public float respawnDelay = 2f;

    void Start()
    {
        if (spawnOnStart) SpawnAll();
    }

    public void SpawnAll()
    {
        if (entries == null) return;
        for (int i = 0; i < entries.Length; i++)
        {
            SpawnAtIndex(i);
        }
    }

    public void DespawnAtIndex(int index)
    {
        if (!IsValidIndex(index)) return;
        var e = entries[index];
        if (e.instance != null)
        {
            Destroy(e.instance);
            e.instance = null;
        }
    }

    public void SpawnAtIndex(int index)
    {
        if (!IsValidIndex(index)) return;

        var e = entries[index];
        if (e == null || e.prefab == null || e.spawnPoint == null) return;
        if (e.instance != null) return;

        GameObject inst = Instantiate(e.prefab, e.spawnPoint.position, e.spawnPoint.rotation);
        e.instance = inst;

        var tracker = inst.AddComponent<SpawnedEnemyTracker>();
        tracker.owner = this;
        tracker.entryIndex = index;
    }

    internal void NotifyEnemyDied(int entryIndex)
    {
        if (!IsValidIndex(entryIndex)) return;

        var e = entries[entryIndex];
        e.instance = null;

        if (e.allowRespawn)
        {
            StartCoroutine(RespawnRoutine(entryIndex));
        }
    }

    private IEnumerator RespawnRoutine(int entryIndex)
    {
        if (respawnDelay > 0f)
            yield return new WaitForSeconds(respawnDelay);

        SpawnAtIndex(entryIndex);
    }

    private bool IsValidIndex(int i)
    {
        return entries != null && i >= 0 && i < entries.Length;
    }
}

class SpawnedEnemyTracker : MonoBehaviour
{
    public EnemySpawnController owner;
    public int entryIndex;

    private Health health;
    private bool hasDied;

    void Awake()
    {
        health = GetComponent<Health>();
        if (health != null)
        {
            health.OnDied += OnDied;
        }
    }

    void OnDestroy()
    {
        if (health != null)
        {
            health.OnDied -= OnDied;
        }
    }

    private void OnDied(GameObject killer)
    {
        if (hasDied) return;
        hasDied = true;

        if (owner != null)
        {
            owner.NotifyEnemyDied(entryIndex);
        }

        Destroy(gameObject);
    }
}
