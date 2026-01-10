using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// Attach this to a Canvas
public class SimpleInventoryUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.G;
    [SerializeField] private int iconsPerRow = 5;
    [SerializeField] private float iconSize = 80f;
    [SerializeField] private float iconSpacing = 10f;

    private GameObject inventoryPanel;
    private GameObject itemContainer;
    private TextMeshProUGUI titleText;
    private List<GameObject> itemSlots = new List<GameObject>();
    private bool isVisible = false;

    private void Start()
    {
        CreateUI();
        UpdateInventoryDisplay();

        // Subscribe to inventory changes
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += UpdateInventoryDisplay;
        }
    }

    private void CreateUI()
    {
        // Create panel
        inventoryPanel = new GameObject("InventoryPanel");
        inventoryPanel.transform.SetParent(transform, false);

        RectTransform panelRect = inventoryPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.15f, 0.15f);
        panelRect.anchorMax = new Vector2(0.85f, 0.85f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = inventoryPanel.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        // Create title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(inventoryPanel.transform, false);

        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -20);
        titleRect.sizeDelta = new Vector2(0, 50);

        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "INVENTORY - Press G to close";
        titleText.fontSize = 28;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;

        // Create item container
        itemContainer = new GameObject("ItemContainer");
        itemContainer.transform.SetParent(inventoryPanel.transform, false);

        RectTransform containerRect = itemContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 0);
        containerRect.anchorMax = new Vector2(1, 1);
        containerRect.offsetMin = new Vector2(20, 20);
        containerRect.offsetMax = new Vector2(-20, -80);

        inventoryPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isVisible = !isVisible;
            inventoryPanel.SetActive(isVisible);
        }
    }

    private void UpdateInventoryDisplay()
    {
        if (InventoryManager.Instance == null || itemContainer == null) return;

        // Clear existing slots
        foreach (GameObject slot in itemSlots)
        {
            Destroy(slot);
        }
        itemSlots.Clear();

        var items = InventoryManager.Instance.GetAllItems();

        // Create slots for each item
        for (int i = 0; i < items.Count; i++)
        {
            Item item = items[i];
            GameObject slot = CreateItemSlot(item, i);
            itemSlots.Add(slot);
        }
    }

    private GameObject CreateItemSlot(Item item, int index)
    {
        // Calculate position
        int row = index / iconsPerRow;
        int col = index % iconsPerRow;

        float xPos = col * (iconSize + iconSpacing);
        float yPos = -row * (iconSize + iconSpacing);

        // Create slot
        GameObject slot = new GameObject($"Slot_{item.itemName}");
        slot.transform.SetParent(itemContainer.transform, false);

        RectTransform slotRect = slot.AddComponent<RectTransform>();
        slotRect.anchorMin = new Vector2(0, 1);
        slotRect.anchorMax = new Vector2(0, 1);
        slotRect.pivot = new Vector2(0, 1);
        slotRect.anchoredPosition = new Vector2(xPos, yPos);
        slotRect.sizeDelta = new Vector2(iconSize, iconSize);

        // Background
        Image bgImage = slot.AddComponent<Image>();
        bgImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

        // Icon
        if (item.icon != null)
        {
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slot.transform, false);

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(5, 20);
            iconRect.offsetMax = new Vector2(-5, -5);

            Image iconImage = iconObj.AddComponent<Image>();
            iconImage.sprite = item.icon;
            iconImage.preserveAspect = true;
        }

        // Name text
        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(slot.transform, false);

        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0);
        nameRect.anchorMax = new Vector2(1, 0);
        nameRect.pivot = new Vector2(0.5f, 0);
        nameRect.anchoredPosition = Vector2.zero;
        nameRect.sizeDelta = new Vector2(0, 15);

        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = item.itemName;
        nameText.fontSize = 12;
        nameText.color = Color.white;
        nameText.alignment = TextAlignmentOptions.Center;

        // Quantity text
        if (item.quantity > 1)
        {
            GameObject qtyObj = new GameObject("Quantity");
            qtyObj.transform.SetParent(slot.transform, false);

            RectTransform qtyRect = qtyObj.AddComponent<RectTransform>();
            qtyRect.anchorMin = new Vector2(1, 1);
            qtyRect.anchorMax = new Vector2(1, 1);
            qtyRect.pivot = new Vector2(1, 1);
            qtyRect.anchoredPosition = new Vector2(-5, -5);
            qtyRect.sizeDelta = new Vector2(30, 20);

            Image qtyBg = qtyObj.AddComponent<Image>();
            qtyBg.color = new Color(0, 0, 0, 0.7f);

            GameObject qtyTextObj = new GameObject("Text");
            qtyTextObj.transform.SetParent(qtyObj.transform, false);

            RectTransform qtyTextRect = qtyTextObj.AddComponent<RectTransform>();
            qtyTextRect.anchorMin = Vector2.zero;
            qtyTextRect.anchorMax = Vector2.one;
            qtyTextRect.offsetMin = Vector2.zero;
            qtyTextRect.offsetMax = Vector2.zero;

            TextMeshProUGUI qtyText = qtyTextObj.AddComponent<TextMeshProUGUI>();
            qtyText.text = item.quantity.ToString();
            qtyText.fontSize = 14;
            qtyText.color = Color.white;
            qtyText.alignment = TextAlignmentOptions.Center;
            qtyText.fontStyle = FontStyles.Bold;
        }

        return slot;
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= UpdateInventoryDisplay;
        }
    }
}