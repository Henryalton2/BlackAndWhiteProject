using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SimpleInventoryUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.G;
    [SerializeField] private int iconsPerRow = 5;
    [SerializeField] private float iconSize = 80f;
    [SerializeField] private float iconSpacing = 10f;

    // ── ADDED: throwable highlight colours ───────────────────────────────────
    [Header("Throwable Highlight")]
    [SerializeField] private Color throwableBorderColor = new Color(1f, 0.8f, 0f, 1f);
    [SerializeField] private Color selectedOverlayColor = new Color(0.2f, 0.5f, 1f, 0.35f);
    // ────────────────────────────────────────────────────────────────────────

    private GameObject inventoryPanel;
    private GameObject itemContainer;
    private TextMeshProUGUI titleText;
    private List<GameObject> itemSlots = new List<GameObject>();
    private bool isVisible = false;

    // ── ADDED: HUD strip showing selected throwable outside the inventory ─────
    private GameObject throwHUD;
    private TextMeshProUGUI throwHUDText;
    // ────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        CreateUI();
        CreateThrowHUD(); // ── ADDED
        UpdateInventoryDisplay();

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged += UpdateInventoryDisplay;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isVisible = !isVisible;
            inventoryPanel.SetActive(isVisible);

            if (isVisible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        UpdateThrowHUD(); // ── ADDED
    }

    private void CreateUI()
    {
        inventoryPanel = new GameObject("InventoryPanel");
        inventoryPanel.transform.SetParent(transform, false);

        RectTransform panelRect = inventoryPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.15f, 0.15f);
        panelRect.anchorMax = new Vector2(0.85f, 0.85f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = inventoryPanel.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(inventoryPanel.transform, false);

        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -20);
        titleRect.sizeDelta = new Vector2(0, 50);

        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "INVENTORY - Press G to close  |  [Q] Cycle throwable  |  Gold = throwable"; // ── ADDED hint
        titleText.fontSize = 28;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;

        itemContainer = new GameObject("ItemContainer");
        itemContainer.transform.SetParent(inventoryPanel.transform, false);

        RectTransform containerRect = itemContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 0);
        containerRect.anchorMax = new Vector2(1, 1);
        containerRect.offsetMin = new Vector2(20, 20);
        containerRect.offsetMax = new Vector2(-20, -80);

        inventoryPanel.SetActive(false);
    }

    // ── ADDED: creates the persistent top-screen HUD strip 
    private void CreateThrowHUD()
    {
        throwHUD = new GameObject("ThrowHUD");
        throwHUD.transform.SetParent(transform, false);

        RectTransform rt = throwHUD.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.3f, 0.92f);
        rt.anchorMax = new Vector2(0.7f, 0.98f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        throwHUD.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(throwHUD.transform, false);

        RectTransform textRT = textObj.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        throwHUDText = textObj.AddComponent<TextMeshProUGUI>();
        throwHUDText.fontSize = 16;
        throwHUDText.color = new Color(1f, 0.9f, 0.3f);
        throwHUDText.alignment = TextAlignmentOptions.Center;
    }

        private void UpdateThrowHUD()
    {
        ThrowingSystem ts = FindObjectOfType<ThrowingSystem>();
        if (ts == null) { throwHUD.SetActive(false); return; }

        Item sel = ts.SelectedThrowable;
        if (sel == null)
        {
            throwHUDText.text = "[Q] No throwable selected";
            throwHUDText.color = Color.gray;
        }
        else
        {
            string aimHint = ts.IsAiming ? "  AIMING..." : "  [Hold LMB] to aim";
            throwHUDText.text = $"THROW: {sel.itemName}  x{sel.quantity}{aimHint}";
            throwHUDText.color = new Color(1f, 0.9f, 0.3f);
        }

        throwHUD.SetActive(!isVisible);
    }
    // ────────────────────────────────────────────────────────────────────────

    private void UpdateInventoryDisplay()
    {
        if (InventoryManager.Instance == null || itemContainer == null) return;

        foreach (GameObject slot in itemSlots)
            Destroy(slot);
        itemSlots.Clear();

        var items = InventoryManager.Instance.GetAllItems();

        for (int i = 0; i < items.Count; i++)
        {
            Item item = items[i];
            GameObject slot = CreateItemSlot(item, i);
            itemSlots.Add(slot);
        }
    }

    private GameObject CreateItemSlot(Item item, int index)
    {
        int row = index / iconsPerRow;
        int col = index % iconsPerRow;

        float xPos = col * (iconSize + iconSpacing);
        float yPos = -row * (iconSize + iconSpacing);

        GameObject slot = new GameObject($"Slot_{item.itemName}");
        slot.transform.SetParent(itemContainer.transform, false);

        RectTransform slotRect = slot.AddComponent<RectTransform>();
        slotRect.anchorMin = new Vector2(0, 1);
        slotRect.anchorMax = new Vector2(0, 1);
        slotRect.pivot = new Vector2(0, 1);
        slotRect.anchoredPosition = new Vector2(xPos, yPos);
        slotRect.sizeDelta = new Vector2(iconSize, iconSize);

        Image bgImage = slot.AddComponent<Image>();
        bgImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

        // ── ADDED: gold border and selected overlay for throwable items ───────
        if (item.isThrowable)
        {
            AddBorder(slot, throwableBorderColor, 3f);

            ThrowingSystem ts = FindObjectOfType<ThrowingSystem>();
            if (ts != null && ts.SelectedThrowable?.itemName == item.itemName)
            {
                GameObject overlay = new GameObject("SelectedOverlay");
                overlay.transform.SetParent(slot.transform, false);

                RectTransform ovRT = overlay.AddComponent<RectTransform>();
                ovRT.anchorMin = Vector2.zero;
                ovRT.anchorMax = Vector2.one;
                ovRT.offsetMin = Vector2.zero;
                ovRT.offsetMax = Vector2.zero;

                overlay.AddComponent<Image>().color = selectedOverlayColor;
            }
        }
        // ────────────────────────────────────────────────────────────────────

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

        // ── ADDED: THROW badge on throwable slots ─────────────────────────────
        if (item.isThrowable)
        {
            GameObject badgeObj = new GameObject("ThrowBadge");
            badgeObj.transform.SetParent(slot.transform, false);

            RectTransform badgeRect = badgeObj.AddComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0, 0);
            badgeRect.anchorMax = new Vector2(0, 0);
            badgeRect.pivot = new Vector2(0, 0);
            badgeRect.anchoredPosition = new Vector2(3, 16);
            badgeRect.sizeDelta = new Vector2(40, 14);

            badgeObj.AddComponent<Image>().color = new Color(0.8f, 0.6f, 0f, 0.85f);

            GameObject badgeTextObj = new GameObject("BadgeText");
            badgeTextObj.transform.SetParent(badgeObj.transform, false);

            RectTransform btRect = badgeTextObj.AddComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.offsetMin = Vector2.zero;
            btRect.offsetMax = Vector2.zero;

            TextMeshProUGUI badgeText = badgeTextObj.AddComponent<TextMeshProUGUI>();
            badgeText.text = "THROW";
            badgeText.fontSize = 8;
            badgeText.color = Color.black;
            badgeText.alignment = TextAlignmentOptions.Center;
            badgeText.fontStyle = FontStyles.Bold;
        }
        // ────────────────────────────────────────────────────────────────────

        return slot;
    }

    // ── ADDED: helper that draws a programmatic border around a slot ──────────
    private void AddBorder(GameObject parent, Color color, float thickness)
    {
        string[] sides = { "BorderTop", "BorderBottom", "BorderLeft", "BorderRight" };
        foreach (string side in sides)
        {
            GameObject border = new GameObject(side);
            border.transform.SetParent(parent.transform, false);

            RectTransform rt = border.AddComponent<RectTransform>();
            border.AddComponent<Image>().color = color;

            switch (side)
            {
                case "BorderTop":
                    rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(0.5f, 1);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(0, thickness);
                    break;
                case "BorderBottom":
                    rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0);
                    rt.pivot = new Vector2(0.5f, 0);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(0, thickness);
                    break;
                case "BorderLeft":
                    rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(0, 1);
                    rt.pivot = new Vector2(0, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(thickness, 0);
                    break;
                case "BorderRight":
                    rt.anchorMin = new Vector2(1, 0); rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(1, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(thickness, 0);
                    break;
            }
        }
    }
    // ────────────────────────────────────────────────────────────────────────

    public bool IsInventoryOpen()
    {
        return isVisible;
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= UpdateInventoryDisplay;
    }
}