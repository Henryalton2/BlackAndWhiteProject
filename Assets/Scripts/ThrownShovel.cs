using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ThrownShovel : MonoBehaviour
{
    [Header("Orientation Points")]
    [Tooltip("Empty child GameObject placed at the very tip/blade of the shovel sprite.")]
    public Transform tipPoint;
    [Tooltip("Empty child GameObject placed at the end of the handle.")]
    public Transform handlePoint;

    [Header("Platform")]
    [Tooltip("The collider the player stands on when the shovel is stuck. " +
             "Should be a separate BoxCollider sitting on the flat of the blade.")]
    public Collider platformCollider;

    [Header("Stick")]
    public float embedDepth = 0.15f;

    [Header("Cloud Boost")]
    [Tooltip("Layer your cloud platform prefab lives on. " +
             "Shovel passing through a cloud multiplies the catch boost.")]
    public LayerMask cloudLayer;
    [Tooltip("How much stronger the catch boost is after passing through a cloud.")]
    public float cloudBoostMultiplier = 2.5f;

    [Header("Recall")]
    [Tooltip("Units per second — determines how long the recall arc takes relative to distance.")]
    public float recallSpeed = 25f;
    [Tooltip("Arc height as a fraction of recall distance (0.3 = 30%). Automatically larger on long throws.")]
    public float arcHeight = 0.3f;
    [Tooltip("Arc side-sweep as a fraction of recall distance. Positive = right of the path, negative = left.")]
    public float arcSideOffset = 0.3f;
    [Tooltip("Degrees per second the shovel spins in the camera plane during recall (always face-on, never edge-on).")]
    public float recallSpinSpeed = 600f;
    [Tooltip("Degrees the shovel banks to one side as it returns — gives a curving-in feel. Negative tilts the other way.")]
    public float recallTiltDegrees = 20f;

    private Rigidbody _rb;
    private ShovelController _owner;
    private LayerMask _penetrableMask;
    private System.Action<Vector3> _onRecalled;
    private bool _stuck;
    private bool _recalling;
    private bool _passedThroughCloud;

    // ── Init ───────────────────────────────────────────────────────────────

    public void Launch(ShovelController owner, Vector3 velocity, LayerMask penetrable)
    {
        _owner = owner;
        _penetrableMask = penetrable;
        _rb = GetComponent<Rigidbody>();
        _rb.velocity = velocity;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (platformCollider != null) platformCollider.enabled = false;
    }

    // ── Flight ─────────────────────────────────────────────────────────────

    void Update()
    {
        if (_stuck || _recalling) return;

        if (_rb != null && _rb.velocity.sqrMagnitude > 1f)
            AlignTipToFacing(_rb.velocity.normalized);
    }

    // Aligns the tip toward worldDir AND axially spins so the sprite face
    // is always visible to the owner's camera (one-axis billboard).
    //
    // How it works:
    //   1. Base rotation  — FromToRotation(localTipDir, velocity) makes the
    //      tip track the flight direction, exactly as before.
    //   2. Axial billboard — project both the sprite's face normal and the
    //      camera direction onto the plane perpendicular to the velocity axis,
    //      then rotate around that axis by the signed angle between them.
    //      This keeps the face always turned toward the player without
    //      disturbing the tip-to-handle alignment.
    void AlignTipToFacing(Vector3 worldDir)
    {
        if (tipPoint == null || handlePoint == null) return;

        Vector3 localTipDir = (tipPoint.localPosition - handlePoint.localPosition).normalized;

        // Step 1 – tip tracks velocity.
        Quaternion baseRot = Quaternion.FromToRotation(localTipDir, worldDir);

        if (_owner == null || _owner.playerCamera == null)
        {
            transform.rotation = baseRot;
            return;
        }

        // Step 2 – spin around the velocity axis so the sprite face (local +Z)
        // stays as close to facing the camera as the constraint allows.
        Vector3 toCam      = (_owner.playerCamera.transform.position - transform.position).normalized;
        Vector3 spriteFace = baseRot * Vector3.forward;                       // face normal after base rotation

        Vector3 faceOnPlane = Vector3.ProjectOnPlane(spriteFace, worldDir);   // project onto ⊥ plane
        Vector3 camOnPlane  = Vector3.ProjectOnPlane(toCam,      worldDir);

        if (faceOnPlane.sqrMagnitude > 0.001f && camOnPlane.sqrMagnitude > 0.001f)
        {
            float     angle     = Vector3.SignedAngle(faceOnPlane.normalized, camOnPlane.normalized, worldDir);
            Quaternion axialSpin = Quaternion.AngleAxis(angle, worldDir);
            transform.rotation   = axialSpin * baseRot;
        }
        else
        {
            transform.rotation = baseRot;   // degenerate case: flying straight at camera
        }
    }

    // Rotates the shovel so the tip→handle axis points toward 'worldDir'.
    // Used for stick orientation only — no billboard needed when embedded.
    void AlignTipTo(Vector3 worldDir)
    {
        if (tipPoint == null || handlePoint == null) return;

        // Vector from handle to tip expressed in this object's local space.
        // Because both points are children of this transform, subtracting their
        // localPositions gives us a stable local-space direction regardless of
        // how the object is currently rotated.
        Vector3 localTipDir = (tipPoint.localPosition - handlePoint.localPosition).normalized;

        // FromToRotation(from, to) produces a rotation R such that R * from == to.
        // Setting transform.rotation = R makes the local 'localTipDir' point
        // toward 'worldDir' in world space.
        transform.rotation = Quaternion.FromToRotation(localTipDir, worldDir);
    }

    // ── Surface Hit ────────────────────────────────────────────────────────

    void OnCollisionEnter(Collision col)
    {
        if (_stuck || _recalling) return;

        // Cloud platform with a solid collider — flag and let physics bounce off it.
        if (cloudLayer != 0 && ((1 << col.gameObject.layer) & cloudLayer) != 0)
        {
            _passedThroughCloud = true;
            return;
        }

        bool isPenetrable = ((1 << col.gameObject.layer) & _penetrableMask) != 0;
        if (!isPenetrable) return;

        Stick(col);
    }

    // Cloud platform with a trigger collider — shovel flies straight through.
    void OnTriggerEnter(Collider other)
    {
        if (_stuck || _recalling) return;

        if (cloudLayer != 0 && ((1 << other.gameObject.layer) & cloudLayer) != 0)
            _passedThroughCloud = true;
    }

    void Stick(Collision col)
    {
        _stuck = true;

        _rb.velocity        = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.isKinematic     = true;

        // Point the tip into the surface using the same logic as flight
        ContactPoint cp = col.contacts[0];
        AlignTipTo(-cp.normal);
        transform.position = cp.point + cp.normal * embedDepth;

        // Parent to the surface so the shovel moves with it (moving platforms, etc.)
        transform.SetParent(col.transform);

        // Convert every non-platform collider to a trigger so the player's
        // CharacterController isn't blocked by the shovel body.
        // Works correctly with any number of colliders on the prefab.
        foreach (var c in GetComponents<Collider>())
            if (c != platformCollider) c.isTrigger = true;

        // Enable the standing platform
        if (platformCollider != null) platformCollider.enabled = true;
    }

    // ── Recall ─────────────────────────────────────────────────────────────

    public void Recall(System.Action<Vector3> onComplete)
    {
        _onRecalled = onComplete;
        StartCoroutine(RecallCoroutine());
    }

    IEnumerator RecallCoroutine()
    {
        // Capture stuck state before clearing it — boost only makes sense if the
        // shovel had actually embedded in a surface and traveled back from distance.
        bool wasStuck = _stuck;

        _recalling = true;
        _stuck     = false;

        transform.SetParent(null);
        if (platformCollider != null) platformCollider.enabled = false;
        if (_rb != null) _rb.isKinematic = true;

        // Disable all colliders so the kinematic shovel can't push the player.
        foreach (var c in GetComponents<Collider>())
            c.enabled = false;

        // Boost direction: world-space vector from stuck position to player body,
        // captured now before anything moves.
        Vector3 boostDir = wasStuck
            ? (_owner.transform.position - transform.position).normalized
            : Vector3.zero;

        // Cloud multiplier is applied AFTER the loop so it catches clouds hit
        // during both the outbound throw and the return path.

        // Arc setup ───────────────────────────────────────────────────────────
        // Start is fixed in world space; end tracks the player live so the arc
        // adjusts if they move (feels magnetic, like the axe homing in).
        Vector3 startPos      = transform.position;
        Vector3 initialTarget = _owner.playerCamera.transform.position
                              + _owner.playerCamera.transform.forward * 0.4f;

        // Capture distance once — used for both duration and arc scaling.
        float totalDist = Vector3.Distance(startPos, initialTarget);
        float duration  = Mathf.Max(totalDist / recallSpeed, 0.25f);
        float elapsed   = 0f;
        float spinAngle = 0f;

        while (_owner != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Quadratic ease-in: crawls off the surface, then whooshes into hand.
            float easedT = t * t;

            Vector3 handTarget = _owner.playerCamera.transform.position
                               + _owner.playerCamera.transform.forward * 0.4f;

            // Bezier arc: offset the control point up AND to the side.
            // The lateral direction is perpendicular to the horizontal path,
            // so the curve always sweeps sideways relative to wherever the
            // shovel is coming from rather than a fixed world direction.
            Vector3 mid        = Vector3.Lerp(startPos, handTarget, 0.5f);
            Vector3 flatPath   = new Vector3(handTarget.x - startPos.x, 0f, handTarget.z - startPos.z);
            Vector3 lateral    = flatPath.sqrMagnitude > 0.001f
                                 ? Vector3.Cross(flatPath.normalized, Vector3.up) * (arcSideOffset * totalDist)
                                 : Vector3.right * (arcSideOffset * totalDist);
            Vector3 control    = mid + Vector3.up * (arcHeight * totalDist) + lateral;
            transform.position = QuadraticBezier(startPos, control, handTarget, easedT);

            // Orientation during recall — billboard + camera-plane spin.
            //
            // The old approach (spin around the tip axis) made the sprite invisible
            // because the tip was pointing AT the camera, so spinning around it was
            // like spinning a coin aimed at your eye — you only ever see the edge.
            //
            // Instead:
            //   bill  — face the camera so the sprite is always fully visible.
            //   tilt  — bank slightly to one side (Y rotation in camera space),
            //            so it arrives at an angle and looks like it's curving in.
            //   spin  — rotate in the plane of the sprite (Z rotation), which is
            //            the camera plane after billboarding, so it always stays face-on.
            //
            // Order: spin first (local), tilt, billboard — final result is a
            // face-on, banked, spinning sprite, like a boomerang flying back flat.
            spinAngle += recallSpinSpeed * Time.deltaTime;

            Vector3    toCam = (_owner.playerCamera.transform.position - transform.position).normalized;
            Quaternion bill  = Quaternion.LookRotation(toCam, Vector3.up);
            Quaternion tilt  = Quaternion.Euler(0f, recallTiltDegrees, 0f);
            Quaternion spin  = Quaternion.AngleAxis(spinAngle, Vector3.forward);
            transform.rotation = bill * tilt * spin;

            // Cloud detection during recall — colliders are disabled so physics
            // callbacks won't fire. CheckSphere polls directly each frame instead.
            if (cloudLayer != 0 && !_passedThroughCloud)
                if (Physics.CheckSphere(transform.position, 0.5f, cloudLayer))
                    _passedThroughCloud = true;

            if (t >= 1f) break;

            yield return null;
        }

        // Apply cloud multiplier here so it covers clouds hit on both the
        // outbound throw (flagged via OnCollisionEnter/OnTriggerEnter) and
        // the return path (flagged via CheckSphere above).
        if (_passedThroughCloud && boostDir.sqrMagnitude > 0.01f)
            boostDir *= cloudBoostMultiplier;

        _onRecalled?.Invoke(boostDir);
        Destroy(gameObject);
    }

    static Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1f - t;
        return (u * u * p0) + (2f * u * t * p1) + (t * t * p2);
    }
}
