using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Weapon))]
public class EnemyCombatController : MonoBehaviour
{
    public Transform player;
    public string playerTag = "Player";

    [Header("Ranged")]
    public bool useRanged = true;
    public bool manualFireOnly = false;

    [Header("Detection")]
    public bool useRectDetection = true;
    public float detectLeft = 4f;
    public float detectRight = 4f;
    public float detectUp = 2f;
    public float detectDown = 1f;
    public float detectionRange = 6f;

    [Header("Attack")]
    public float attackRange = 5f;
    public float fireInterval = 1.5f;

    [Header("Aim")]
    public bool fireForwardOnly = true;

    [Header("Projectile Overrides")]
    public bool overrideBulletSpeed = false;
    public float bulletSpeed = 10f;
    public bool overrideBulletDamage = false;
    public int bulletDamage = 1;

    [Header("Touch Damage")]
    public bool useTouchDamage = false;
    public int touchDamage = 1;
    public float touchCooldown = 0.5f;
    public LayerMask damageMask;

    private Weapon weapon;
    private float fireTimer;
    private Dictionary<object, float> lastTouchTime = new Dictionary<object, float>();
    private Health health;

    void Awake()
    {
        weapon = GetComponent<Weapon>();
        fireTimer = 0f;
        health = GetComponent<Health>();
    }

    void Start()
    {
        if (player == null && !string.IsNullOrEmpty(playerTag))
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go != null) player = go.transform;
        }
    }

    void Update()
    {
        if (health != null && health.IsFrozen)
        {
            fireTimer = fireInterval;
            return;
        }

        fireTimer -= Time.deltaTime;

        if (useRanged && !manualFireOnly && player != null)
        {
            bool detected = useRectDetection ? IsPlayerInRectZone() : IsPlayerInCircleZone();
            bool inAttack = IsPlayerInAttackRange();

            if (detected && inAttack && fireTimer <= 0f)
            {
                Fire();
                fireTimer = fireInterval;
            }
        }
    }

    public void FireOnceAtPlayer()
    {
        if (!useRanged || player == null) return;
        Fire();
    }

    private void Fire()
    {
        if (weapon == null) return;
        if (overrideBulletSpeed) weapon.bulletSpeed = bulletSpeed;
        if (overrideBulletDamage) weapon.damage = bulletDamage;

        Vector2 dir;
        if (fireForwardOnly)
        {
            float sign = transform.localScale.x >= 0f ? 1f : -1f;
            dir = new Vector2(sign, 0f);
        }
        else
        {
            dir = ((Vector2)(player.position - (weapon != null && weapon.firePoint != null ? weapon.firePoint.position : transform.position))).normalized;
        }

        weapon.Fire(dir);
    }

    private bool IsPlayerInRectZone()
    {
        if (player == null) return false;
        Vector2 localPos = transform.InverseTransformPoint(player.position);
        return localPos.x >= -detectLeft &&
               localPos.x <= detectRight &&
               localPos.y >= -detectDown &&
               localPos.y <= detectUp;
    }

    private bool IsPlayerInCircleZone()
    {
        if (player == null) return false;
        float dist = Vector2.Distance(transform.position, player.position);
        return dist <= detectionRange;
    }

    private bool IsPlayerInAttackRange()
    {
        if (player == null) return false;
        float dist = Vector2.Distance(transform.position, player.position);
        return dist <= attackRange;
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (!useTouchDamage) return;
        TryTouchDamage(collision.collider);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!useTouchDamage) return;
        TryTouchDamage(other);
    }

    private void TryTouchDamage(Collider2D col)
    {
        if (col == null) return;
        if (damageMask.value != 0 && ((1 << col.gameObject.layer) & damageMask.value) == 0) return;

        if (col.TryGetComponent<IDamageable>(out var dmg))
        {
            var rootA = transform.root;
            var rootB = (col.attachedRigidbody ? col.attachedRigidbody.transform.root : col.transform.root);
            if (rootA == rootB) return;

            object key = dmg;
            float tNow = Time.time;
            if (!lastTouchTime.TryGetValue(key, out var tLast) || tNow - tLast >= touchCooldown)
            {
                dmg.ApplyDamage(touchDamage, gameObject);
                lastTouchTime[key] = tNow;
            }
        }
    }

    void OnDisable()
    {
        lastTouchTime.Clear();
    }

    void OnDrawGizmosSelected()
    {
        if (!useRectDetection) return;

        Gizmos.color = Color.red;
        Vector3 localCenter = new Vector3((detectRight - detectLeft) * 0.5f, (detectUp - detectDown) * 0.5f, 0f);
        Vector3 localSize = new Vector3(detectLeft + detectRight, detectUp + detectDown, 0.1f);

        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(localCenter, localSize);
        Gizmos.matrix = old;
    }
}
