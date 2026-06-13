using UnityEngine;

public class AnimationSpeedController : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Speed multiplier for this animator (1 = normal speed)")]
    [Range(0.1f, 5f)]
    public float animationSpeed = 1f;

    [Tooltip("Apply speed on start")]
    public bool applyOnStart = true;

    [Header("Random Start Delay")]
    [Tooltip("Delay the animation start by a random amount of seconds")]
    public bool randomStartDelay = true;

    [Tooltip("Minimum delay in seconds before animation starts")]
    public float minDelay = 0f;

    [Tooltip("Maximum delay in seconds before animation starts")]
    public float maxDelay = 5f;

    private Animator animator;

    void Start()
    {
        // Get the Animator component
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogError("No Animator component found on " + gameObject.name);
            return;
        }

        // Apply random start delay if enabled
        if (randomStartDelay)
        {
            float delay = Random.Range(minDelay, maxDelay);
            animator.enabled = false; // Disable animator initially
            Invoke(nameof(StartAnimation), delay);
        }
        else if (applyOnStart)
        {
            SetAnimationSpeed(animationSpeed);
        }
    }

    /// <summary>
    /// Starts the animation after delay
    /// </summary>
    private void StartAnimation()
    {
        animator.enabled = true;

        if (applyOnStart)
        {
            SetAnimationSpeed(animationSpeed);
        }
    }

    /// <summary>
    /// Sets the animation speed for this object's animator
    /// </summary>
    /// <param name="speed">Speed multiplier (1 = normal, 2 = double speed, 0.5 = half speed)</param>
    public void SetAnimationSpeed(float speed)
    {
        if (animator != null)
        {
            animator.speed = speed;
            animationSpeed = speed;
        }
    }

    /// <summary>
    /// Gets the current animation speed
    /// </summary>
    public float GetAnimationSpeed()
    {
        return animator != null ? animator.speed : animationSpeed;
    }

    // Update is called once per frame (optional - for runtime adjustments in Inspector)
    void Update()
    {
#if UNITY_EDITOR
        // Allow real-time speed changes in the editor
        if (animator != null && !Mathf.Approximately(animator.speed, animationSpeed))
        {
            animator.speed = animationSpeed;
        }
#endif
    }
}