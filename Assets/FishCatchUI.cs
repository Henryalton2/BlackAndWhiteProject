using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays a popup when the player catches a fish.
///
/// Hierarchy setup inside your existing Canvas:
///
///   CatchPopup                  (this script lives here)
///    ├── Background              (Image — your panel art)
///    ├── FishIcon                (Image — displays the fish sprite)
///    ├── FishNameText            (TMP_Text — large, bold)
///    ├── FishDescriptionText     (TMP_Text — smaller, flavour text)
///    └── DismissText             (TMP_Text — e.g. "Press F to continue")
///
/// Setup:
///   1. Build the hierarchy above inside your Canvas
///   2. Attach this script to CatchPopup
///   3. Assign all fields in the Inspector
///   4. Disable CatchPopup by default in the Inspector
///   5. On your FishingRod GameObject, assign CatchPopup to the
///      Fish Catch UI field and it will be called automatically via OnCatch
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
    public float autoDismissAfter = 0f;     // set > 0 to auto-dismiss, 0 = manual only
    public string dismissPrompt = "Press F to continue";

    [Header("Animation")]
    public float fadeInDuration = 0.3f;
    public float fadeOutDuration = 0.2f;

    private CanvasGroup canvasGroup;
    private Coroutine showCoroutine;
    private bool isShowing = false;

    // ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        // CanvasGroup lets us fade the whole popup in/out
        canvasGroup = popupPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = popupPanel.AddComponent<CanvasGroup>();

        popupPanel.SetActive(false);

        if (dismissText != null)
            dismissText.text = dismissPrompt;
    }

    void Update()
    {
        if (!isShowing) return;

        if (Input.GetKeyDown(dismissKey))
            Dismiss();
    }

    // ── Called by FishingRod via OnCatch event ────────────────────────
    public void ShowCatch(FishData fish)
    {
        if (fish == null) return;

        // Populate UI
        if (fishNameText != null)
            fishNameText.text = fish.fishName;

        if (fishDescriptionText != null)
            fishDescriptionText.text = fish.description;

        if (fishIcon != null)
        {
            fishIcon.sprite = fish.fishIcon;
            fishIcon.gameObject.SetActive(fish.fishIcon != null);
        }

        if (showCoroutine != null) StopCoroutine(showCoroutine);
        showCoroutine = StartCoroutine(ShowSequence());
    }

    // ── Show / hide sequence ──────────────────────────────────────────
    IEnumerator ShowSequence()
    {
        isShowing = true;
        popupPanel.SetActive(true);
        canvasGroup.alpha = 0f;

        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // Auto dismiss if set
        if (autoDismissAfter > 0f)
        {
            yield return new WaitForSeconds(autoDismissAfter);
            if (isShowing) Dismiss();
        }
    }

    public void Dismiss()
    {
        if (!isShowing) return;
        if (showCoroutine != null) StopCoroutine(showCoroutine);
        showCoroutine = StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        isShowing = false;

        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        popupPanel.SetActive(false);
    }
}