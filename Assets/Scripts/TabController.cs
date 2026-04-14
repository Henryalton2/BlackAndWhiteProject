using UnityEngine;
using UnityEngine.UI;

public class TabController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject[] panels;

    [Header("Buttons")]
    public Button[] tabButtons;

    [Header("Colors")]
    public Color activeColor = Color.white;
    public Color inactiveColor = new Color(0.7f, 0.7f, 0.7f);

    void Start()
    {
        ShowTab(0);
    }

    public void ShowTab(int index)
    {
        Debug.Log("ShowTab called: " + index);
        for (int i = 0; i < panels.Length; i++)
        {
            panels[i].SetActive(i == index);

            var colors = tabButtons[i].colors;
            colors.normalColor = (i == index) ? activeColor : inactiveColor;
            colors.highlightedColor = (i == index) ? activeColor : inactiveColor;
            colors.disabledColor = inactiveColor; // keep visible when disabled
            tabButtons[i].colors = colors;

            tabButtons[i].interactable = (i != index);
        }
    }
}