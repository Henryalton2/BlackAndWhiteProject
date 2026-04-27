using System.Collections;
using UnityEngine;

public class FishingRod : MonoBehaviour
{
    [Header("References")]
    public Transform rodTipTransform;
    public GameObject hookPrefab;
    public LineRenderer lineRenderer;
    public FishingMinigameUI minigameUI;
    public FishCatchUI fishCatchUI;

    [Header("Cast Settings")]
    public float castForce = 18f;
    public float castUpAngle = 40f;
    public KeyCode castKey = KeyCode.Mouse0;
    public KeyCode reelKey = KeyCode.Mouse0;

    [Header("Auto Reel - No Water")]
    public float maxCastHangTime = 4f;

    [Header("Auto Reel - Movement")]
    public float maxCastDistance = 8f;
    public float maxTurnAngle = 90f;

    [Header("Bite Timing")]
    public float minWaitSeconds = 3f;
    public float maxWaitSeconds = 9f;

    [Header("Reel In")]
    public float reelInDuration = 0.5f;

    [Header("Line")]
    public int lineSegments = 20;
    [Range(0f, 1f)]
    public float lineSagMultiplier = 0.08f;

    [Header("Water Detection")]
    public LayerMask waterLayerMask;

    [Header("Equipment")]
    [SerializeField] private string fishingRodItemName = "Fishing Rod";
    [SerializeField] private Sprite fishingRodIcon;
    [SerializeField] private GameObject rodVisual; // assign the rod mesh GameObject in Inspector

    // ── State ─────────────────────────────────────────────────────────
    public enum FishingState { Idle, Casting, WaitingForBite, Sinking, Minigame }
    public FishingState State { get; private set; } = FishingState.Idle;

    private bool isEquipped = false;
    private GameObject activeHook;
    private HookBobber hookBobber;
    private Rigidbody hookRb;
    private Coroutine biteCoroutine;
    private float castTimer = 0f;
    private Vector3 castOrigin;
    private Vector3 castForwardDir;

    // ── Events ────────────────────────────────────────────────────────
    public System.Action OnCast;
    public System.Action OnBite;
    public System.Action<FishData> OnCatch;
    public System.Action OnMiss;

    // ─────────────────────────────────────────────────────────────────
    void Start()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnEquipChanged += HandleEquipChanged;

            if (!InventoryManager.Instance.HasItem(fishingRodItemName))
            {
                Item rodItem = new Item(fishingRodItemName, fishingRodIcon, 1);
                rodItem.isEquippable = true;
                InventoryManager.Instance.AddItem(rodItem);
            }

            isEquipped = InventoryManager.Instance.IsEquipped(fishingRodItemName);
        }
        else
        {
            // No inventory system in scene — default to equipped so casting still works
            isEquipped = true;
        }

        UpdateRodVisual();
    }

    void HandleEquipChanged(string equippedName)
    {
        isEquipped = equippedName == fishingRodItemName;
        if (!isEquipped && State != FishingState.Idle)
            ForceReel();
        UpdateRodVisual();
    }

    void UpdateRodVisual()
    {
        if (rodVisual != null)
            rodVisual.SetActive(isEquipped);
    }

    void Update()
    {
        if (!isEquipped) return;

        CheckAutoReelConditions();
        UpdateLineRenderer();

        switch (State)
        {
            case FishingState.Idle:
                if (Input.GetKeyDown(castKey)) Cast();
                break;

            case FishingState.Casting:
                castTimer += Time.deltaTime;
                if (castTimer >= maxCastHangTime)
                {
                    Debug.Log("[Fishing] Hook didn't land in water — auto reeling in.");
                    ForceReel();
                }
                break;

            case FishingState.WaitingForBite:
                if (Input.GetKeyDown(reelKey)) EarlyReel();
                break;

            case FishingState.Sinking:
                if (Input.GetKeyDown(reelKey)) StartMinigame();
                break;
        }
    }

    // ── AUTO REEL - MOVEMENT / TURN ───────────────────────────────────
    void CheckAutoReelConditions()
    {
        if (State == FishingState.Idle || State == FishingState.Minigame) return;

        float distanceMoved = Vector3.Distance(transform.position, castOrigin);
        if (distanceMoved > maxCastDistance)
        {
            Debug.Log("[Fishing] Player moved too far — auto reeling in.");
            ForceReel();
            return;
        }

        float angle = Vector3.Angle(castForwardDir, transform.forward);
        if (angle > maxTurnAngle)
        {
            Debug.Log("[Fishing] Player turned too far — auto reeling in.");
            ForceReel();
        }
    }

    void ForceReel()
    {
        CancelBite();
        if (hookBobber != null) hookBobber.StopTug();
        StartCoroutine(ReelInHook());
        OnMiss?.Invoke();
    }

    // ── CAST ─────────────────────────────────────────────────────────
    void Cast()
    {
        if (activeHook != null) Destroy(activeHook);

        castTimer = 0f;
        castOrigin = transform.position;
        castForwardDir = transform.forward;

        activeHook = Instantiate(hookPrefab, rodTipTransform.position, Quaternion.identity);
        hookBobber = activeHook.GetComponent<HookBobber>();
        hookRb = activeHook.GetComponent<Rigidbody>();
        hookBobber.Initialize(this);

        Vector3 castDir = Quaternion.AngleAxis(-castUpAngle, Camera.main.transform.right)
                          * Camera.main.transform.forward;
        hookRb.AddForce(castDir * castForce, ForceMode.Impulse);

        State = FishingState.Casting;
        lineRenderer.enabled = true;
        lineRenderer.positionCount = lineSegments;
        OnCast?.Invoke();

        Debug.Log("[Fishing] Cast!");
    }

    // ── Water callbacks from HookBobber ──────────────────────────────
    public void OnHookEnteredWater()
    {
        if (State != FishingState.Casting) return;

        State = FishingState.WaitingForBite;
        biteCoroutine = StartCoroutine(WaitForBite());
        Debug.Log("[Fishing] Hook in water — waiting for bite.");
    }

    public void OnHookLeftWater()
    {
        if (State == FishingState.WaitingForBite)
        {
            CancelBite();
            castTimer = 0f;
            State = FishingState.Casting;
            Debug.Log("[Fishing] Hook left water — back to casting state.");
        }
    }

    // ── BITE SEQUENCE ─────────────────────────────────────────────────
    IEnumerator WaitForBite()
    {
        float waitTime = Random.Range(minWaitSeconds, maxWaitSeconds);
        yield return new WaitForSeconds(waitTime);

        if (State != FishingState.WaitingForBite) yield break;

        State = FishingState.Sinking;
        hookBobber.TriggerSink();
        hookBobber.StartTug();
        OnBite?.Invoke();
        Debug.Log("[Fishing] Bite! Player has 2.5s to reel.");

        yield return new WaitForSeconds(2.5f);

        if (State == FishingState.Sinking)
            MissedBite();
    }

    // ── REEL ACTIONS ──────────────────────────────────────────────────
    void EarlyReel()
    {
        CancelBite();
        StartCoroutine(ReelInHook());
        OnMiss?.Invoke();
        Debug.Log("[Fishing] Reeled in early — no fish.");
    }

    void MissedBite()
    {
        CancelBite();
        if (hookBobber != null) hookBobber.StopTug();
        StartCoroutine(ReelInHook());
        OnMiss?.Invoke();
        Debug.Log("[Fishing] Missed the bite window.");
    }

    void StartMinigame()
    {
        if (State != FishingState.Sinking) return;

        CancelBite();
        State = FishingState.Minigame;

        FishData fish = FishDatabase.GetRandomFish();

        if (fish == null)
        {
            Debug.LogError("[Fishing] FishDatabase returned null — check that FishDatabase asset is in Assets/Resources/ and has fish assigned.");
            ResetFishing();
            return;
        }

        minigameUI.StartMinigame(fish, OnMinigameComplete);
        Debug.Log($"[Fishing] Minigame started with fish: {fish.fishName}");
    }

    void OnMinigameComplete(bool success, FishData fish)
    {
        Debug.Log($"[Fishing] OnMinigameComplete — success: {success}, fish: {(fish == null ? "NULL" : fish.fishName)}, catchUI: {(fishCatchUI == null ? "NULL" : "assigned")}");

        if (success)
        {
            if (fish != null)
            {
                Debug.Log($"[Fishing] Caught: {fish.fishName}!");
                OnCatch?.Invoke(fish);
                InventoryManager.Instance?.AddItem(fish.fishName, fish.fishIcon, 1);

                if (fishCatchUI != null)
                    fishCatchUI.ShowCatch(fish);
                else
                    Debug.LogWarning("[Fishing] fishCatchUI is not assigned on FishingRod — catch panel won't show.");
            }
            else
            {
                Debug.LogError("[Fishing] Success but fish is null — cannot show catch panel.");
            }
        }
        else
        {
            Debug.Log("[Fishing] Fish escaped the minigame.");
            OnMiss?.Invoke();
        }

        if (hookBobber != null) hookBobber.StopTug();
        StartCoroutine(ReelInHook());
    }

    // ── REEL IN ANIMATION ────────────────────────────────────────────
    IEnumerator ReelInHook()
    {
        Debug.Log("[Fishing] ReelInHook started.");
        State = FishingState.Idle;

        if (activeHook == null)
        {
            ResetFishing();
            yield break;
        }

        float elapsed = 0f;
        Vector3 startPos = activeHook.transform.position;

        if (hookRb != null)
        {
            hookRb.velocity = Vector3.zero;
            hookRb.isKinematic = true;
        }

        while (elapsed < reelInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Easing.EaseInCubic(Mathf.Clamp01(elapsed / reelInDuration));

            if (activeHook != null)
                activeHook.transform.position = Vector3.Lerp(startPos, rodTipTransform.position, t);

            yield return null;
        }

        ResetFishing();
        Debug.Log("[Fishing] Hook reeled in.");
    }

    // ── CLEANUP ───────────────────────────────────────────────────────
    void CancelBite()
    {
        if (biteCoroutine != null)
        {
            StopCoroutine(biteCoroutine);
            biteCoroutine = null;
        }
    }

    void ResetFishing()
    {
        Debug.Log($"[Fishing] ResetFishing called — hook is {(activeHook == null ? "null" : "NOT null")}");

        if (activeHook != null)
        {
            Destroy(activeHook);
            activeHook = null;
            hookBobber = null;
            hookRb = null;
        }

        lineRenderer.enabled = false;
    }

    // ── LINE RENDERER ─────────────────────────────────────────────────
    void UpdateLineRenderer()
    {
        if (!lineRenderer.enabled || activeHook == null) return;

        Vector3 start = rodTipTransform.position;
        Vector3 end = activeHook.transform.position;

        float sag = 0f;
        if (State == FishingState.WaitingForBite)
            sag = Vector3.Distance(start, end) * lineSagMultiplier;

        for (int i = 0; i < lineSegments; i++)
        {
            float t = i / (float)(lineSegments - 1);
            Vector3 point = Vector3.Lerp(start, end, t);
            point.y -= Mathf.Sin(t * Mathf.PI) * sag;
            lineRenderer.SetPosition(i, point);
        }
    }

    void OnDestroy()
    {
        CancelBite();
        if (activeHook != null) Destroy(activeHook);
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnEquipChanged -= HandleEquipChanged;
    }
}
