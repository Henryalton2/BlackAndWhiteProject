using UnityEngine;
using FMODUnity;

public class BirdFly : MonoBehaviour
{
    [Header("Movement Settings")]
    public float baseSpeed = 5f;      // Bird speed
    public float lifeTime = 15f;      // Destroy after this many seconds
    private Vector3 direction;
    private float speed;

    [Header("FMOD Sound Settings")]
    public float soundDelay = 10f;    // Seconds after spawn to play sound
    private StudioEventEmitter fmodEmitter;

    /// <summary>
    /// Initialize the bird movement
    /// </summary>
    /// <param name="flyDirection">Direction to fly</param>
    /// <param name="depthMultiplier">Optional speed multiplier</param>
    /// <param name="sortingOrder">Sprite sorting order</param>
    public void Init(Vector3 flyDirection, float depthMultiplier = 1f, int sortingOrder = 100)
    {
        direction = flyDirection.normalized;

        // Flip sprite if moving left
        if (direction.x < 0)
            transform.localScale = new Vector3(-1, 1, 1);

        // Apply speed multiplier
        speed = baseSpeed * depthMultiplier;

        // SpriteRenderer setup
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = sortingOrder;
            Color c = sr.color;
            c.a = Mathf.Lerp(0.6f, 1f, depthMultiplier);
            sr.color = c;
        }

        // Destroy after lifeTime
        Destroy(gameObject, lifeTime);

        // Play FMOD event after delay, if emitter exists
        if (fmodEmitter != null)
            Invoke(nameof(PlaySound), soundDelay);
    }

    private void Awake()
    {
        // Automatically find the Studio Event Emitter on the same GameObject
        fmodEmitter = GetComponent<StudioEventEmitter>();
        if (fmodEmitter == null)
        {
            Debug.LogWarning($"BirdFly: No Studio Event Emitter found on {gameObject.name}. Sound will not play.");
        }
    }

    private void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    private void PlaySound()
    {
        if (fmodEmitter != null)
            fmodEmitter.Play();
    }
}
