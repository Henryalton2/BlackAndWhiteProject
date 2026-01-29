using UnityEngine;
using FMODUnity;

public class BirdFly : MonoBehaviour
{
    [Header("Movement")]
    public float baseSpeed = 5f;
    public float lifeTime = 15f;

    private Vector3 direction;
    private float speed;

    [Header("Animation Settings")]
    public float animationStartOffset = 1f;
    public float minAnimSpeed = 0.9f;
    public float maxAnimSpeed = 1.1f;

    [Header("Sound Settings")]
    public StudioEventEmitter emitter;
    public float minChirpInterval = 30f;
    public float maxChirpInterval = 40f;
    public float cancelRadius = 20f;

    private float nextChirpTime;
    private bool soundActive = false;

    /// <summary>
    /// Called by the spawner when this bird is created
    /// </summary>
    public void Init(Vector3 flyDirection, float depthMultiplier = 1f, int sortingOrder = 0)
    {
        direction = flyDirection.normalized;

        // Preserve the prefab's original scale; do not flip
        Vector3 originalScale = transform.localScale;
        transform.localScale = originalScale;

        speed = baseSpeed * depthMultiplier;

        // Set sprite layer
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.sortingOrder = sortingOrder;

        // Randomize animation
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.speed = Random.Range(minAnimSpeed, maxAnimSpeed);
            AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
            float offset = Random.Range(0f, animationStartOffset);
            anim.Play(state.fullPathHash, 0, offset);
        }

        // Auto destroy after lifetime
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // Movement
        transform.position += direction * speed * Time.deltaTime;

        // FMOD chirping
        if (soundActive && Time.time >= nextChirpTime)
        {
            TryPlayChirp();
        }
    }

    /// <summary>
    /// Starts FMOD sound after initial delay
    /// </summary>
    public void StartSoundAfterDelay(float delay)
    {
        if (emitter == null)
        {
            Debug.LogWarning("BirdFly has no FMOD emitter assigned.");
            return;
        }

        soundActive = true;
        Invoke(nameof(FirstChirp), delay);
    }

    private void FirstChirp()
    {
        TryPlayChirp();
    }

    private void TryPlayChirp()
    {
        // Cancel if another nearby bird is already chirping
        BirdFly[] birds = FindObjectsOfType<BirdFly>();
        foreach (BirdFly other in birds)
        {
            if (other == this) continue;
            if (!other.soundActive) continue;

            float dist = Vector3.Distance(transform.position, other.transform.position);
            if (dist < cancelRadius && other.emitter != null && other.emitter.IsPlaying())
            {
                ScheduleNextChirp();
                return;
            }
        }

        emitter.Play();
        ScheduleNextChirp();
    }

    private void ScheduleNextChirp()
    {
        nextChirpTime = Time.time + Random.Range(minChirpInterval, maxChirpInterval);
    }
}
