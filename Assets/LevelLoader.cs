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
    public TMP_Text tipText;

    [TextArea]
    public string[] tips;

    public float tipChangeInterval = 4f;
    public float estimatedLoadTime = 35f;

    void Start()
    {
        StartCoroutine(LoadAsynchronously(2));
        StartCoroutine(CycleTips());
    }

    IEnumerator CycleTips()
    {
        while (true)
        {
            if (tips.Length > 0)
                tipText.text = tips[Random.Range(0, tips.Length)];
            yield return new WaitForSeconds(tipChangeInterval);
        }
    }

    IEnumerator LoadAsynchronously(int sceneIndex)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);
        operation.allowSceneActivation = false;
        loadingScreen.SetActive(true);

        float elapsed = 0f;

        while (elapsed < estimatedLoadTime)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / estimatedLoadTime);
            slider.value = progress;
            progressText.text = Mathf.RoundToInt(progress * 100f) + "%";
            yield return null;
        }

        slider.value = 1f;
        progressText.text = "100%";
        operation.allowSceneActivation = true;
    }
}