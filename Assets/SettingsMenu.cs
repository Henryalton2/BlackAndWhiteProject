using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.UI;
using TMPro;

public class SettingsMenu : MonoBehaviour
{
    public AudioMixer audioMixer;
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown windowModeDropdown;
    public Canvas settingsCanvas;

    Resolution[] resolutions;

    public void Initialize()
    {
        StartCoroutine(SetupAsync());
    }

    private IEnumerator SetupAsync()
    {
        // Spread setup across two frames so neither causes a visible hitch
        SetupResolutionDropdown();
        yield return null;
        SetupWindowModeDropdown();
        yield return null;

        resolutionDropdown.onValueChanged.AddListener((value) => { StartCoroutine(ForceCloseDropdown(resolutionDropdown)); });
        windowModeDropdown.onValueChanged.AddListener((value) => { StartCoroutine(ForceCloseDropdown(windowModeDropdown)); });
    }

    private IEnumerator ForceCloseDropdown(TMP_Dropdown dropdown)
    {
        dropdown.Hide();
        yield return new WaitForSecondsRealtime(0.15f);

        Transform[] allChildren = settingsCanvas.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in allChildren)
        {
            if (child.name == "Blocker")
            {
                Destroy(child.gameObject);
            }
        }

        dropdown.Hide();
    }

    private void SetupResolutionDropdown()
    {
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        List<Resolution> filteredResolutions = new List<Resolution>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            bool duplicate = false;
            for (int j = 0; j < filteredResolutions.Count; j++)
            {
                if (filteredResolutions[j].width == resolutions[i].width &&
                    filteredResolutions[j].height == resolutions[i].height)
                {
                    duplicate = true;
                    break;
                }
            }

            if (!duplicate)
            {
                filteredResolutions.Add(resolutions[i]);
                string option = resolutions[i].width + "x" + resolutions[i].height;
                options.Add(option);

                if (resolutions[i].width == Screen.currentResolution.width &&
                    resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = filteredResolutions.Count - 1;
                }
            }
        }

        resolutions = filteredResolutions.ToArray();
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    private void SetupWindowModeDropdown()
    {
        windowModeDropdown.ClearOptions();

        List<string> options = new List<string>()
        {
            "Fullscreen",
            "Windowed",
            "Borderless Window"
        };

        windowModeDropdown.AddOptions(options);

        switch (Screen.fullScreenMode)
        {
            case FullScreenMode.ExclusiveFullScreen:
                windowModeDropdown.value = 0;
                break;
            case FullScreenMode.Windowed:
                windowModeDropdown.value = 1;
                break;
            case FullScreenMode.FullScreenWindow:
                windowModeDropdown.value = 2;
                break;
            default:
                windowModeDropdown.value = 0;
                break;
        }

        windowModeDropdown.RefreshShownValue();
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreenMode);
    }

    public void SetWindowMode(int windowModeIndex)
    {
        FullScreenMode mode;

        switch (windowModeIndex)
        {
            case 0:
                mode = FullScreenMode.ExclusiveFullScreen;
                break;
            case 1:
                mode = FullScreenMode.Windowed;
                break;
            case 2:
                mode = FullScreenMode.FullScreenWindow;
                break;
            default:
                mode = FullScreenMode.ExclusiveFullScreen;
                break;
        }

        Screen.fullScreenMode = mode;
    }

    public void SetVolume(float volume)
    {
        audioMixer.SetFloat("volume", volume);
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }
}