using UnityEngine;

public class ParticleAutoDestroy : MonoBehaviour
{
    public ParticleSystem target;

    void Awake()
    {
        if (target == null) target = GetComponent<ParticleSystem>();
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }
        if (!target.IsAlive(true)) Destroy(gameObject);
    }
}
