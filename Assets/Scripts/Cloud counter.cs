using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CloudCounterUI : MonoBehaviour
{
    [Header("References")]
    public CloudPlatformSpawner spawner;

    [Header("Icon (optional)")]
    public Sprite cloudIcon;
    public float iconSize = 28f;

    [Header("Style")]
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.55f);
    public Color textColor = Color.white;
    public float padding = 10f;

    private TextMeshProUGUI label;

    private void Start()
    {
        if (spawner == null)
            spawner = FindObjectOfType<CloudPlatformSpawner>();

        BuildUI();
    }

    private void Update()
    {
        if (spawner != null && label != null)
            label.text = $"Clouds: {GetCloudsLeft()} / {spawner.maxClouds}";
    }

    private int GetCloudsLeft()
    {
        var field = typeof(CloudPlatformSpawner)
            .GetField("cloudsLeft", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null ? (int)field.GetValue(spawner) : 0;
    }

    private void BuildUI()
    {
        // Panel
        GameObject panel = new GameObject("CloudCounterPanel");
        panel.transform.SetParent(transform, false);

        float panelWidth = cloudIcon != null ? 190f : 160f;

        RectTransform pr = panel.AddComponent<RectTransform>();
        pr.anchorMin = new Vector2(0, 0);
        pr.anchorMax = new Vector2(0, 0);
        pr.pivot = new Vector2(0, 0);
        pr.anchoredPosition = new Vector2(20, 20);
        pr.sizeDelta = new Vector2(panelWidth, 40);

        panel.AddComponent<Image>().color = backgroundColor;

        float textOffsetLeft = padding;

        // Icon (optional)
        if (cloudIcon != null)
        {
            GameObject iconObj = new GameObject("CloudIcon");
            iconObj.transform.SetParent(panel.transform, false);

            RectTransform ir = iconObj.AddComponent<RectTransform>();
            ir.anchorMin = new Vector2(0, 0.5f);
            ir.anchorMax = new Vector2(0, 0.5f);
            ir.pivot = new Vector2(0, 0.5f);
            ir.anchoredPosition = new Vector2(padding, 0);
            ir.sizeDelta = new Vector2(iconSize, iconSize);

            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = cloudIcon;
            iconImg.preserveAspect = true;

            textOffsetLeft = padding + iconSize + 6f;
        }

        // Text
        GameObject textObj = new GameObject("CloudLabel");
        textObj.transform.SetParent(panel.transform, false);

        RectTransform tr = textObj.AddComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(textOffsetLeft, 0);
        tr.offsetMax = new Vector2(-padding, 0);

        label = textObj.AddComponent<TextMeshProUGUI>();
        label.text = "Clouds: - / -";
        label.fontSize = 16;
        label.color = textColor;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.MidlineLeft;
    }
}