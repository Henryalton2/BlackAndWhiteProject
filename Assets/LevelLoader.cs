using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LevelLoader : MonoBehaviour
{
    public GameObject loadingScreen;
    public Slider slider;
    public TMP_Text progressText;

    public float estimatedLoadTime = 20f;

    void Start()
    {
        StartCoroutine(LoadAsynchronously(2));
    }

    IEnumerator LoadAsynchronously(int sceneIndex)
    {
        loadingScreen.SetActive(true);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);
        operation.allowSceneActivation = false;

        float elapsed = 0f;

        // Fill bar over estimated time
        while (elapsed < estimatedLoadTime)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / estimatedLoadTime);
            slider.value = progress;
            progressText.text = Mathf.RoundToInt(progress * 100f) + "%";
            yield return null;
        }

        // Bar is full, now let scene activate
        slider.value = 1f;
        progressText.text = "100%";
        operation.allowSceneActivation = true;

        // Wait for scene to actually finish
        while (!operation.isDone)
        {
            yield return null;
        }
    }
}