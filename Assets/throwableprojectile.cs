using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ThrowableProjectile : MonoBehaviour
{
    [HideInInspector] public float damage = 10f;
    [HideInInspector] public string itemName = "";
    [HideInInspector] public float destroyAfter = 8f;

    [Header("Optional FX")]
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private GameObject impactFXPrefab;

    private bool _hasHit = false;

    private void Start()
    {
        Destroy(gameObject, destroyAfter);
    }

    private void OnCollisionEnter(Collision col)
    {
        if (_hasHit) return;
        _hasHit = true;

        if (trail != null)
        {
            trail.transform.SetParent(null);
            Destroy(trail.gameObject, trail.time + 0.5f);
        }

        if (impactFXPrefab != null)
            Instantiate(impactFXPrefab, col.contacts[0].point,
                        Quaternion.LookRotation(col.contacts[0].normal));

        IDamageable target = col.gameObject.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            Debug.Log($"[Throw] {itemName} hit {col.gameObject.name} for {damage} damage.");
            target.TakeDamage(damage);
        }

        Destroy(gameObject, 0.05f);
    }
}

public interface IDamageable
{
    void TakeDamage(float amount);
}