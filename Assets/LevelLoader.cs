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
    [Range(0f, 1f)]
    public float finishAtPercent = 0.8f; // set to 0.8 for 80%, 0.75 for 75% etc

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

        // Fill bar to finishAtPercent over estimated load time
        while (elapsed < estimatedLoadTime)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / estimatedLoadTime) * finishAtPercent;
            slider.value = progress;
            progressText.text = Mathf.RoundToInt(progress * 100f) + "%";
            yield return null;
        }

        // Hold at finishAtPercent and activate scene
        slider.value = finishAtPercent;
        progressText.text = Mathf.RoundToInt(finishAtPercent * 100f) + "%";
        operation.allowSceneActivation = true;

        // Wait for scene to finish then show 100%
        while (!operation.isDone)
        {
            yield return null;
        }
    }
}