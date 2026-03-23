using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Stardew Valley-style fishing minigame.
///
/// Hierarchy:
///   MinigamePanel
///    ├── Track
///    │    ├── FishZone         (pivot 0.5, 0.5)
///    │    │    └── Hitbox      (empty GameObject — resize to match art)
///    │    └── CatchBar         (pivot 0.5, 0.5)
///    │         └── Hitbox      (empty GameObject — resize to match art)
///    ├── ProgressFill          (Image, Filled, Vertical, Bottom)
///    └── ResultText            (TMP_Text)
///
/// Y=0 is the bottom of the track, Y=trackHeight is the top.
/// </summary>
public class FishingMinigameUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject minigamePanel;
    public RectTransform trackRect;
    public RectTransform catchBar;
    public RectTransform fishZone;
    public Image progressFill;
    public Image progressImage;
    public TMP_Text resultText;

    [Header("Hitboxes")]
    public RectTransform catchBarHitbox;
    public RectTransform fishZoneHitbox;

    [Header("Catch Bar Physics")]
    public float gravity = 900f;
    public float riseForce = 1400f;

    [Header("Fish Zone")]
    public float fishZoneMinSpeed = 80f;
    public float fishZoneMaxSpeed = 220f;
    public float fishZoneDirectionChangeCooldown = 0.6f;

    [Header("Progress")]
    public float progressGainRate = 28f;
    public float progressLossRate = 18f;
    public float progressStartAmount = 50f;

    [Header("Colors")]
    public Color progressColorNormal = new Color(0.31f, 0.76f, 0.97f);
    public Color progressColorOverlap = new Color(0.31f, 0.97f, 0.48f);

    [Header("Input")]
    public KeyCode holdKey = KeyCode.R;

    // ── Internal ──────────────────────────────────────────────────────
    private float trackHeight;

    private float catchBarY;
    private float catchBarVel;
    private float catchBarHalfH;

    private float fishZoneY;
    private float fishZoneVel;
    private float fishZoneHalfH;
    private float fishZoneDirTimer;

    private float progress;

    private bool isActive = false;
    private Action<bool, FishData> onComplete;
    private FishData currentFish;

    // ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        minigamePanel.SetActive(false);
        if (resultText != null) resultText.gameObject.SetActive(false);
    }

    public void StartMinigame(FishData fish, Action<bool, FishData> callback)
    {
        currentFish = fish;
        onComplete = callback;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(trackRect);

        trackHeight = trackRect.rect.height;
        Debug.Log($"[Minigame] Track height: {trackHeight}");

        // Read half-heights from the actual rects so nothing is hardcoded
        catchBarHalfH = catchBar.rect.height / 2f;
        fishZoneHalfH = fishZone.rect.height / 2f;

        // Apply fish-specific overrides if set
        float zoneMaxSpd = (fish != null && fish.fishZoneMaxSpeedOverride > 0f)
            ? fish.fishZoneMaxSpeedOverride : fishZoneMaxSpeed;

        // Start catch bar in the center
        catchBarY = trackHeight / 2f;
        catchBarVel = 0f;

        // Start fish zone at a random position fully inside the track
        fishZoneY = UnityEngine.Random.Range(fishZoneHalfH, trackHeight - fishZoneHalfH);
        fishZoneVel = UnityEngine.Random.Range(fishZoneMinSpeed, zoneMaxSpd)
                      * (UnityEngine.Random.value > 0.5f ? 1f : -1f);
        fishZoneDirTimer = 0f;

        progress = progressStartAmount;

        ApplyPositions();
        UpdateProgressBar();
        if (progressImage != null) progressImage.color = progressColorNormal;

        isActive = true;
        minigamePanel.SetActive(true);
        Debug.Log("[Minigame] Started.");
    }

    void Update()
    {
        if (!isActive) return;

        float dt = Time.deltaTime;

        UpdateCatchBar(dt);
        UpdateFishZone(dt);
        UpdateProgress(dt);
        ApplyPositions();
        CheckCompletion();
    }

    // ── Catch bar ─────────────────────────────────────────────────────
    void UpdateCatchBar(float dt)
    {
        bool holding = Input.GetKey(holdKey);

        catchBarVel += (holding ? -riseForce : gravity) * dt;
        catchBarVel = Mathf.Clamp(catchBarVel, -riseForce, riseForce);
        catchBarY -= catchBarVel * dt;

        catchBarY = Mathf.Clamp(catchBarY, catchBarHalfH, trackHeight - catchBarHalfH);
    }

    // ── Fish zone ─────────────────────────────────────────────────────
    void UpdateFishZone(float dt)
    {
        fishZoneDirTimer -= dt;

        if (fishZoneDirTimer <= 0f)
        {
            float speed = UnityEngine.Random.Range(fishZoneMinSpeed, fishZoneMaxSpeed);
            float dir = (catchBarY < fishZoneY) ? -1f : 1f;
            if (UnityEngine.Random.value < 0.3f) dir = -dir;
            fishZoneVel = speed * dir;
            fishZoneDirTimer = fishZoneDirectionChangeCooldown
                               + UnityEngine.Random.Range(0f, 0.4f);
        }

        fishZoneY += fishZoneVel * dt;

        if (fishZoneY < fishZoneHalfH)
        {
            fishZoneY = fishZoneHalfH;
            fishZoneVel = Mathf.Abs(fishZoneVel);
        }
        else if (fishZoneY > trackHeight - fishZoneHalfH)
        {
            fishZoneY = trackHeight - fishZoneHalfH;
            fishZoneVel = -Mathf.Abs(fishZoneVel);
        }
    }

    // ── Progress ─────────────────────────────────────────────────────
    void UpdateProgress(float dt)
    {
        bool overlapping = IsOverlapping();

        if (overlapping)
        {
            progress += progressGainRate * dt;
            if (progressImage != null) progressImage.color = progressColorOverlap;
        }
        else
        {
            progress -= progressLossRate * dt;
            if (progressImage != null) progressImage.color = progressColorNormal;
        }

        progress = Mathf.Clamp(progress, 0f, 100f);
        UpdateProgressBar();
    }

    // Uses world-space corners from the hitbox rects so it matches
    // exactly what the player sees regardless of scaling or pivot
    bool IsOverlapping()
    {
        RectTransform catchHB = catchBarHitbox != null ? catchBarHitbox : catchBar;
        RectTransform fishHB = fishZoneHitbox != null ? fishZoneHitbox : fishZone;

        Vector3[] catchCorners = new Vector3[4];
        Vector3[] fishCorners = new Vector3[4];

        catchHB.GetWorldCorners(catchCorners);
        fishHB.GetWorldCorners(fishCorners);

        // corners: 0=bottomLeft, 1=topLeft, 2=topRight, 3=bottomRight
        float catchBot = catchCorners[0].y;
        float catchTop = catchCorners[1].y;
        float fishBot = fishCorners[0].y;
        float fishTop = fishCorners[1].y;

        return catchBot < fishTop && catchTop > fishBot;
    }

    // ── Win / Lose ────────────────────────────────────────────────────
    void CheckCompletion()
    {
        if (progress >= 100f)
        {
            isActive = false;
            StartCoroutine(ShowResultThenClose(true));
        }
        else if (progress <= 0f)
        {
            isActive = false;
            StartCoroutine(ShowResultThenClose(false));
        }
    }

    IEnumerator ShowResultThenClose(bool success)
    {
        Debug.Log($"[Minigame] Ended — success: {success}");

        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);
            resultText.text = success ? "GOT IT!" : "ESCAPED!";
            resultText.color = success
                ? new Color(0.31f, 0.97f, 0.48f)
                : new Color(0.97f, 0.37f, 0.37f);
        }

        yield return new WaitForSeconds(1.2f);

        if (resultText != null) resultText.gameObject.SetActive(false);
        minigamePanel.SetActive(false);

        onComplete?.Invoke(success, currentFish);
    }

    // ── Positioning ───────────────────────────────────────────────────
    void ApplyPositions()
    {
        catchBar.anchoredPosition = new Vector2(
            catchBar.anchoredPosition.x,
            catchBarY - trackHeight / 2f);

        fishZone.anchoredPosition = new Vector2(
            fishZone.anchoredPosition.x,
            fishZoneY - trackHeight / 2f);
    }

    void UpdateProgressBar()
    {
        if (progressFill != null)
            progressFill.fillAmount = progress / 100f;
    }
}