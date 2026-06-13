using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class Reversenightmenu : MonoBehaviour
{
    [Header("Find NightModeToggle automatically")]
    public NightModeToggle nightModeToggle;

    [Header("Search Settings")]
    [Tooltip("Find all TMP texts in children automatically")]
    public bool autoFindTexts = true;

    [Tooltip("Find all buttons in children automatically")]
    public bool autoFindButtons = true;

    [Header("Manual Assignment (optional)")]
    public TextMeshProUGUI[] manualTMPTexts;
    public Button[] manualButtons;

    [Header("Day/Night Colors")]
    public Color dayTextColor = Color.black;
    public Color nightTextColor = Color.white;

    [Header("Outline/Underlay Colors (optional)")]
    [Tooltip("Enable to also change outline/underlay colors")]
    public bool changeOutlineColors = false;
    public Color dayOutlineColor = Color.white;
    public Color nightOutlineColor = Color.black;

    [Header("Text Fill Settings")]
    [Tooltip("Make text fully solid (removes outline showing through)")]
    public bool makeTextSolid = true;
    [Range(-1f, 1f)]
    [Tooltip("Face Dilate - higher values make text more solid (try 0.5)")]
    public float faceDilate = 0.5f;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private Camera cam;
    private List<TextMeshProUGUI> allTexts = new List<TextMeshProUGUI>();
    private bool isInitialized = false;
    private float lastUpdateProgress = -1f;

    void OnEnable()
    {
        // Initialize when pause menu becomes active
        if (!isInitialized)
        {
            Initialize();
        }

        // Force immediate update to current state
        if (isInitialized && cam != null)
        {
            float progress = GetCurrentProgress();
            UpdateAllTexts(progress);

            if (showDebugLogs)
            {
                Debug.Log($"[Pause Menu] OnEnable - Applied current state. Progress: {progress:F2}");
            }
        }

        // Start coroutine that updates even when paused
        StartCoroutine(UpdateWhilePaused());
    }

    void OnDisable()
    {
        // Stop the coroutine when menu closes
        StopAllCoroutines();
    }

    IEnumerator UpdateWhilePaused()
    {
        while (true)
        {
            if (isInitialized && cam != null)
            {
                float progress = GetCurrentProgress();

                // Only update if progress changed significantly
                if (Mathf.Abs(progress - lastUpdateProgress) > 0.01f)
                {
                    Color textColor = Color.Lerp(dayTextColor, nightTextColor, progress);

                    if (showDebugLogs)
                    {
                        Debug.Log($"[UPDATE] Progress changed to: {progress:F3}");
                        Debug.Log($"  Camera BG: {cam.backgroundColor}");
                        Debug.Log($"  Calculated Text Color: {textColor}");
                    }

                    UpdateAllTexts(progress);
                    lastUpdateProgress = progress;
                }
            }

            // Use unscaled time so it works when Time.timeScale = 0
            yield return new WaitForSecondsRealtime(0.02f);
        }
    }

    void Initialize()
    {
        if (showDebugLogs)
        {
            Debug.Log("=== Initializing PauseMenuNightModeSync ===");
        }

        // Find NightModeToggle
        if (nightModeToggle == null)
        {
            nightModeToggle = FindObjectOfType<NightModeToggle>();
        }

        if (nightModeToggle == null)
        {
            Debug.LogError("[Pause Menu] Cannot find NightModeToggle in scene!");
            return;
        }

        // Get camera
        cam = nightModeToggle.mainCamera;
        if (cam == null) cam = Camera.main;

        if (cam == null)
        {
            Debug.LogError("[Pause Menu] No camera found!");
            return;
        }

        // Clear previous list
        allTexts.Clear();

        // Auto-find texts in children
        if (autoFindTexts)
        {
            TextMeshProUGUI[] foundTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
            allTexts.AddRange(foundTexts);

            if (showDebugLogs)
            {
                Debug.Log($"  Auto-found {foundTexts.Length} TMP texts in children");
            }
        }

        // Auto-find buttons and their texts
        if (autoFindButtons)
        {
            Button[] foundButtons = GetComponentsInChildren<Button>(true);

            foreach (var btn in foundButtons)
            {
                // Get text from button
                TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null && !allTexts.Contains(btnText))
                {
                    allTexts.Add(btnText);
                }
            }

            if (showDebugLogs)
            {
                Debug.Log($"  Found {foundButtons.Length} buttons");
            }
        }

        // Add manual texts
        if (manualTMPTexts != null)
        {
            foreach (var txt in manualTMPTexts)
            {
                if (txt != null && !allTexts.Contains(txt))
                {
                    allTexts.Add(txt);
                }
            }
        }

        // Add manual button texts
        if (manualButtons != null)
        {
            foreach (var btn in manualButtons)
            {
                if (btn != null)
                {
                    TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null && !allTexts.Contains(btnText))
                    {
                        allTexts.Add(btnText);
                    }
                }
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"  TOTAL: {allTexts.Count} TMP texts to update");
            Debug.Log($"  Day Color: {dayTextColor}");
            Debug.Log($"  Night Color: {nightTextColor}");
            Debug.Log($"  Current Camera Color: {cam.backgroundColor}");

            // List all texts
            foreach (var txt in allTexts)
            {
                Debug.Log($"    - {GetGameObjectPath(txt.gameObject)}");
            }
        }

        isInitialized = true;
    }

    float GetCurrentProgress()
    {
        if (cam == null || nightModeToggle == null) return 0f;

        Color currentColor = cam.backgroundColor;
        Color dayColor = nightModeToggle.daySky;
        Color nightColor = nightModeToggle.nightSky;

        // Calculate how close we are to night (0 = day, 1 = night)
        float distanceToDay = ColorDist(currentColor, dayColor);
        float totalDistance = ColorDist(dayColor, nightColor);

        if (totalDistance < 0.01f) return 0f;

        return Mathf.Clamp01(distanceToDay / totalDistance);
    }

    void UpdateAllTexts(float progress)
    {
        Color textColor = Color.Lerp(dayTextColor, nightTextColor, progress);
        Color outlineColor = Color.Lerp(dayOutlineColor, nightOutlineColor, progress);

        int successCount = 0;
        foreach (var txt in allTexts)
        {
            if (txt != null)
            {
                // Set the face color (the main text color)
                txt.faceColor = textColor;

                // Also set vertex color to ensure full coverage
                txt.color = textColor;

                // Make text solid if enabled
                if (makeTextSolid && txt.fontMaterial != null)
                {
                    txt.fontMaterial.SetFloat("_FaceDilate", faceDilate);
                    txt.fontMaterial.SetFloat("_OutlineSoftness", 0f);
                }

                // Set ALL color properties to ensure complete coverage
                if (txt.fontMaterial != null)
                {
                    // Main face color
                    txt.fontMaterial.SetColor("_FaceColor", textColor);

                    if (changeOutlineColors)
                    {
                        // Outline colors
                        txt.outlineColor = outlineColor;
                        txt.fontMaterial.SetColor("_OutlineColor", outlineColor);
                        txt.fontMaterial.SetColor("_UnderlayColor", outlineColor);
                    }
                    else
                    {
                        // If not changing outline colors, set them to match text
                        txt.fontMaterial.SetColor("_OutlineColor", textColor);
                        txt.fontMaterial.SetColor("_UnderlayColor", textColor);
                    }

                    // Glow/Highlight colors (set to match main color)
                    txt.fontMaterial.SetColor("_GlowColor", textColor);
                }

                if (showDebugLogs && successCount == 0) // Log first text only
                {
                    Debug.Log($"  TEXT UPDATE: '{txt.text}' on {txt.gameObject.name}");
                    Debug.Log($"    Set faceColor to: {textColor}");
                    if (makeTextSolid)
                    {
                        Debug.Log($"    Set face dilate to: {faceDilate}");
                    }
                    if (changeOutlineColors)
                    {
                        Debug.Log($"    Set outlineColor to: {outlineColor}");
                    }
                }

                successCount++;
            }
        }
    }

    float ColorDist(Color a, Color b)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float db = a.b - b.b;
        return Mathf.Sqrt(dr * dr + dg * dg + db * db);
    }

    string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;

        while (parent != null && parent != transform)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    // Public method to manually refresh
    public void RefreshTexts()
    {
        isInitialized = false;
        Initialize();

        if (cam != null)
        {
            float progress = GetCurrentProgress();
            UpdateAllTexts(progress);
        }
    }

    // Public method to force a specific state
    public void ForceState(bool isNight)
    {
        float progress = isNight ? 1f : 0f;
        UpdateAllTexts(progress);

        if (showDebugLogs)
        {
            Debug.Log($"[Pause Menu] Forced to {(isNight ? "NIGHT" : "DAY")} mode");
        }
    }
}

