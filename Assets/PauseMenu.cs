using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool GameisPaused = false;

    [Header("UI Panels")]
    public GameObject pauseMenuUI;
    public GameObject settingsMenuUI;

    [Header("Settings")]
    [SerializeField] private float resumeBlendDuration = 0.15f;

    private Canvas canvas;
    private SettingsMenu settingsMenu;
    private bool isResuming = false;

    void Start()
    {
        // Hard reset all state on scene load — static bools survive scene changes so this is critical
        GameisPaused = false;
        isResuming = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        canvas = GetComponent<Canvas>();
        canvas.enabled = false;
        pauseMenuUI.SetActive(false);
        settingsMenuUI.SetActive(false);

        // Initialize settings in the background without toggling visibility
        settingsMenu = settingsMenuUI.GetComponent<SettingsMenu>();
        StartCoroutine(InitializeSettings());
    }

    private IEnumerator InitializeSettings()
    {
        // Let the scene fully settle before building dropdowns
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        settingsMenu.Initialize();
    }

    void Update()
    {
        if (isResuming) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameisPaused)
            {
                if (settingsMenuUI.activeSelf)
                    CloseSettings();
                else
                    Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        if (isResuming) return;
        StartCoroutine(ResumeRoutine());
    }

    private IEnumerator ResumeRoutine()
    {
        isResuming = true;

        // Hide UI immediately
        pauseMenuUI.SetActive(false);
        settingsMenuUI.SetActive(false);
        canvas.enabled = false;
        GameisPaused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Blend timeScale back to 1 using unscaledDeltaTime so it works even while paused
        float elapsed = 0f;
        while (elapsed < resumeBlendDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Lerp(0f, 1f, elapsed / resumeBlendDuration);
            yield return null;
        }

        Time.timeScale = 1f;
        isResuming = false;
    }

    private void Pause()
    {
        canvas.enabled = true;
        pauseMenuUI.SetActive(true);
        settingsMenuUI.SetActive(false);
        Time.timeScale = 0f;
        GameisPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OpenSettings()
    {
        pauseMenuUI.SetActive(false);
        settingsMenuUI.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsMenuUI.SetActive(false);
        pauseMenuUI.SetActive(true);
        // Ensure we're still properly paused when returning to pause menu
        Time.timeScale = 0f;
        GameisPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void LoadMenu()
    {
        // Clean up before leaving so the menu scene starts fresh
        StopAllCoroutines();
        Time.timeScale = 1f;
        GameisPaused = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        StartCoroutine(LoadSceneAsync("Menu"));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
            yield return null;
    }

    public void QuitGame()
    {
        Debug.Log("quit game");
        Application.Quit();
    }
}