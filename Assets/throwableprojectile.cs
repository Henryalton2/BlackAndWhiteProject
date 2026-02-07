using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ThrowableProjectile : MonoBehaviour
{
    [Header("Impact Settings")]
    [SerializeField] private float impactForce = 10f;
    [SerializeField] private float lifetime = 5f;

    [Header("Effects")]
    [SerializeField] private GameObject impactEffect;
    [SerializeField] private AudioClip impactSound;
    [SerializeField] private bool destroyOnImpact = true;

    private Rigidbody rb;
    private bool hasImpacted = false;
    private ThrowableItemData itemData;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Destroy after lifetime
        Destroy(gameObject, lifetime);
    }

    public void Initialize(ThrowableItemData data)
    {
        itemData = data;

        // Override impact sound if provided in data
        if (data.throwSound != null)
        {
            impactSound = data.throwSound;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasImpacted) return;
        hasImpacted = true;

        // Apply impact force to rigidbodies
        Rigidbody hitRb = collision.gameObject.GetComponent<Rigidbody>();
        if (hitRb != null)
        {
            Vector3 impactDirection = collision.contacts[0].normal;
            hitRb.AddForce(-impactDirection * impactForce, ForceMode.Impulse);
        }

        // Spawn impact effect
        if (impactEffect != null)
        {
            Instantiate(
                impactEffect,
                collision.contacts[0].point,
                Quaternion.LookRotation(collision.contacts[0].normal)
            );
        }

        // Play impact sound
        if (impactSound != null)
        {
            AudioSource.PlayClipAtPoint(impactSound, transform.position);
        }

        // Destroy or stop the projectile
        if (destroyOnImpact)
        {
            Destroy(gameObject);
        }
        else
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }
}
