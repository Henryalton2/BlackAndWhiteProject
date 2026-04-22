using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays a popup when the player catches a fish.
///
/// Hierarchy setup inside your FishingCanvas:
///
///   CatchPopup                  (this script lives here, disabled by default)
///    ├── PanelAnimation          (Animator with your 3 frame PNGs)
///    └── Content
///         ├── FishIcon           (Image — displays the fish sprite)
///         ├── FishNameText       (TMP_Text)
///         ├── FishDescriptionText(TMP_Text)
///         └── DismissText        (TMP_Text)
///
/// Setup:
///   1. Attach this script to CatchPopup
///   2. Assign all fields in the Inspector
///   3. Disable CatchPopup by default
///   4. Assign CatchPopup to the Fish Catch UI field on FishingRod
/// </summary>
public class FishCatchUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject popupPanel;
    public Image fishIcon;
    public TMP_Text fishNameText;
    public TMP_Text fishDescriptionText;
    public TMP_Text dismissText;

    [Header("Settings")]
    public KeyCode dismissKey = KeyCode.F;
    public float autoDismissAfter = 0f;     // 0 = manual dismiss only, >0 = auto close
    public string dismissPrompt = "Press F to continue";

    private bool isShowing = false;
    private float dismissTimer = 0f;

    // ─────────────────────────────────────────────────────────────────
    void Update()
    {
        if (!isShowing) return;

        // Manual dismiss
        if (Input.GetKeyDown(dismissKey))
        {
            Dismiss();
            return;
        }

        // Auto dismiss countdown
        if (autoDismissAfter > 0f)
        {
            dismissTimer += Time.deltaTime;
            if (dismissTimer >= autoDismissAfter)
                Dismiss();
        }
    }

    // ── Called by FishingRod when a fish is caught ────────────────────
    public void ShowCatch(FishData fish)
    {
        if (fish == null)
        {
            Debug.LogWarning("[CatchUI] ShowCatch called with null fish.");
            return;
        }

        Debug.Log($"[CatchUI] Showing catch panel for: {fish.fishName}");

        // Populate text and icon
        if (fishNameText != null)
            fishNameText.text = fish.fishName;

        if (fishDescriptionText != null)
            fishDescriptionText.text = fish.description;

        if (fishIcon != null)
        {
            fishIcon.sprite = fish.fishIcon;
            fishIcon.gameObject.SetActive(fish.fishIcon != null);
        }

        if (dismissText != null)
            dismissText.text = dismissPrompt;

        dismissTimer = 0f;
        isShowing = true;
        popupPanel.SetActive(true);
    }

    // ── Dismiss ───────────────────────────────────────────────────────
    public void Dismiss()
    {
        isShowing = false;
        dismissTimer = 0f;
        popupPanel.SetActive(false);
        Debug.Log("[CatchUI] Catch panel dismissed.");
    }
}