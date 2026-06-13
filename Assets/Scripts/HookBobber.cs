using System.Collections;
using UnityEngine;

/// <summary>
/// Attach to the hookPrefab. Handles:
///   - Water entry/exit detection via Trigger collider on water
///   - Bobbing animation on the water surface
///   - Sinking animation when a fish bites
///   - Fish tug movement with shore avoidance raycasts
///
/// Setup:
///   - Add a Rigidbody (non-kinematic initially for the cast arc)
///   - Add a small SphereCollider for physics
///   - The water volume must have a Trigger collider on the Water layer
///   - Your lake shore mesh must be on a dedicated Shore layer
///   - Optionally assign a bobberRenderer for color feedback
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class HookBobber : MonoBehaviour
{
    [Header("Bobbing")]
    public float bobAmplitude = 0.04f;
    public float bobFrequency = 1.4f;

    [Header("Sink Animation")]
    public float sinkDepth = 0.35f;
    public float sinkDuration = 0.4f;

    [Header("Visuals")]
    public Renderer bobberRenderer;
    public Color aboveWaterColor = new Color(0.9f, 0.2f, 0.2f);
    public Color belowWaterColor = new Color(0.2f, 0.6f, 0.9f);
    public GameObject splashParticlePrefab;

    [Header("Fish Tug")]
    public float tugSwayAmount = 0.15f;     // side-to-side sway distance
    public float tugSwaySpeed = 3f;         // sway frequency
    public float tugDownAmount = 0.08f;     // extra downward pull while tugging

    [Header("Lake Boundary")]
    public LayerMask shoreLayerMask;        // set to your lake edge/shore layer
    public float shoreCheckDistance = 1f;   // raycast distance before avoiding shore
    public float shoreAvoidStrength = 3f;   // how hard the tug pushes away from shore

    // ── Internal ─────────────────────────────────────────────────────
    private FishingRod fishingRod;
    private Rigidbody rb;
    private bool inWater = false;
    private float waterSurfaceY;
    private bool isSinking = false;
    private bool isTugging = false;
    private float tugTime = 0f;
    private Coroutine sinkCoroutine;
    private Coroutine exitCoroutine;

    // ─────────────────────────────────────────────────────────────────
    public void Initialize(FishingRod rod)
    {
        fishingRod = rod;
        rb = GetComponent<Rigidbody>();
        Debug.Log("[HookBobber] Initialized with FishingRod.");
    }

    void Update()
    {
        if (!inWater) return;

        if (isSinking) return;

        if (isTugging)
        {
            UpdateTug();
        }
        else
        {
            UpdateBob();
        }
    }

    // ── Gentle bob on surface ─────────────────────────────────────────
    void UpdateBob()
    {
        float bobOffset = Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
        Vector3 pos = transform.position;
        pos.y = waterSurfaceY + bobOffset;
        transform.position = pos;
    }

    // ── Fish tug with shore avoidance ─────────────────────────────────
    void UpdateTug()
    {
        tugTime += Time.deltaTime;

        // Irregular sway using two sine waves at different frequencies
        // for an organic, non-repeating feel
        float swayX = Mathf.Sin(tugTime * tugSwaySpeed) * tugSwayAmount
                    + Mathf.Sin(tugTime * tugSwaySpeed * 2.3f) * tugSwayAmount * 0.4f;

        float swayZ = Mathf.Sin(tugTime * tugSwaySpeed * 1.5f) * tugSwayAmount * 0.6f;

        float swayY = Mathf.Abs(Mathf.Sin(tugTime * tugSwaySpeed * 1.7f)) * -tugDownAmount;

        Vector3 tugDir = new Vector3(swayX, 0f, swayZ);

        // Raycast outward in four directions to detect shore proximity
        // and steer the tug away from land
        Vector3[] checkDirs = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        foreach (Vector3 dir in checkDirs)
        {
            if (Physics.Raycast(transform.position, dir, out RaycastHit hit,
                                shoreCheckDistance, shoreLayerMask))
            {
                // The closer to shore, the harder we push away
                float proximity = 1f - (hit.distance / shoreCheckDistance);
                tugDir -= dir * proximity * shoreAvoidStrength;
                Debug.Log($"[HookBobber] Shore detected in direction {dir}, proximity {proximity:F2} — steering away.");
            }
        }

        Vector3 newPos = transform.position;
        newPos += tugDir * Time.deltaTime;
        newPos.y += swayY * Time.deltaTime;

        // Keep the hook locked to water surface depth while tugging
        newPos.y = Mathf.Min(newPos.y, waterSurfaceY);

        transform.position = newPos;
    }

    // ── Start / stop tug (called by FishingRod) ───────────────────────
    public void StartTug()
    {
        isTugging = true;
        tugTime = 0f;
        Debug.Log("[HookBobber] Fish tug started.");
    }

    public void StopTug()
    {
        isTugging = false;
        Debug.Log("[HookBobber] Fish tug stopped.");
    }

    // ── Water trigger ─────────────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[HookBobber] OnTriggerEnter: {other.gameObject.name} (layer {other.gameObject.layer})");

        if (!IsWater(other))
        {
            Debug.Log("[HookBobber] Not a water collider — ignoring.");
            return;
        }

        // Cancel any pending exit so a brief bounce doesn't kill the bite
        if (exitCoroutine != null)
        {
            StopCoroutine(exitCoroutine);
            exitCoroutine = null;
            Debug.Log("[HookBobber] Cancelled pending water exit.");
        }

        if (inWater)
        {
            Debug.Log("[HookBobber] Already in water — ignoring re-entry.");
            return;
        }

        inWater = true;
        waterSurfaceY = transform.position.y;
        Debug.Log($"[HookBobber] Entered water. Surface Y = {waterSurfaceY}");

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        if (splashParticlePrefab != null)
            Instantiate(splashParticlePrefab, transform.position, Quaternion.identity);

        SetBobberColor(true);

        fishingRod.OnHookEnteredWater();
        Debug.Log("[HookBobber] Called OnHookEnteredWater — bite timer started.");
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsWater(other)) return;

        Debug.Log("[HookBobber] OnTriggerExit: hook left water — starting delayed exit check.");
        exitCoroutine = StartCoroutine(DelayedExit());
    }

    IEnumerator DelayedExit()
    {
        yield return new WaitForSeconds(0.3f);

        Debug.Log("[HookBobber] Delayed exit confirmed — hook is out of water. Bite cancelled.");
        inWater = false;
        isTugging = false;
        rb.isKinematic = false;
        SetBobberColor(false);
        fishingRod.OnHookLeftWater();
    }

    bool IsWater(Collider other)
    {
        return ((1 << other.gameObject.layer) & fishingRod.waterLayerMask) != 0;
    }

    // ── Sink animation (called by FishingRod on bite) ─────────────────
    public void TriggerSink()
    {
        Debug.Log("[HookBobber] TriggerSink called — starting sink animation.");
        if (sinkCoroutine != null) StopCoroutine(sinkCoroutine);
        sinkCoroutine = StartCoroutine(SinkAnimation());
    }

    IEnumerator SinkAnimation()
    {
        isSinking = true;

        // Small pre-dip to hint at the bite
        float preDipAmount = 0.06f;
        float t = 0f;
        float startY = transform.position.y;

        while (t < 1f)
        {
            t += Time.deltaTime / 0.15f;
            Vector3 p = transform.position;
            p.y = startY - Mathf.Sin(t * Mathf.PI) * preDipAmount;
            transform.position = p;
            yield return null;
        }

        // Actual sink
        t = 0f;
        float sinkTargetY = waterSurfaceY - sinkDepth;
        Debug.Log($"[HookBobber] Sinking from {waterSurfaceY} to {sinkTargetY}");

        while (t < 1f)
        {
            t += Time.deltaTime / sinkDuration;
            Vector3 p = transform.position;
            p.y = Mathf.Lerp(waterSurfaceY, sinkTargetY, Easing.EaseInCubic(t));
            transform.position = p;
            yield return null;
        }

        isSinking = false;
        Debug.Log("[HookBobber] Sink animation complete — waiting for player to reel.");
        SetBobberColor(false);
    }

    // ── Visuals ───────────────────────────────────────────────────────
    void SetBobberColor(bool halfSubmerged)
    {
        if (bobberRenderer == null) return;
        bobberRenderer.material.color = halfSubmerged ? aboveWaterColor : belowWaterColor;
    }

    void OnDestroy()
    {
        if (sinkCoroutine != null) StopCoroutine(sinkCoroutine);
        if (exitCoroutine != null) StopCoroutine(exitCoroutine);
    }
}

// ── Small easing utility ──────────────────────────────────────────────
public static class Easing
{
    public static float EaseInCubic(float t) => t * t * t;
    public static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    public static float EaseInOutCubic(float t) =>
        t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
}