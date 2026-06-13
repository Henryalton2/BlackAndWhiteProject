using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;


/// Programmatic store UI — same code-driven approach as SimpleInventoryUI.
/// Call OpenStore() / CloseStore() from your NPC script.

public class StoreUI : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private float cardWidth = 160f;
    [SerializeField] private float cardHeight = 200f;
    [SerializeField] private float cardSpacing = 12f;
    [SerializeField] private int cardsPerRow = 3;

    //Runtime 
    private GameObject storePanel;
    private GameObject cardContainer;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI feedbackText;
    private float feedbackTimer;

    private List<GameObject> cards = new();
    private bool isOpen = false;

    //Colours (match existing UI palette)
    private static readonly Color BG = new(0.08f, 0.08f, 0.08f, 0.97f);
    private static readonly Color CardBG = new(0.18f, 0.18f, 0.18f, 1f);
    private static readonly Color BtnNormal = new(0.20f, 0.45f, 0.20f, 1f);
    private static readonly Color BtnNoFunds = new(0.45f, 0.20f, 0.20f, 1f);
    private static readonly Color BtnNoStock = new(0.35f, 0.35f, 0.35f, 1f);
    private static readonly Color Gold = new(1f, 0.85f, 0.2f, 1f);

       private void Start()
    {
        BuildPanel();

        if (StoreManager.Instance != null)
            StoreManager.Instance.OnStoreChanged += RefreshCards;

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged += RefreshCards;
    }

    private void Update()
    {
        // Dismiss feedback label after 2 s
        if (feedbackTimer > 0)
        {
            feedbackTimer -= Time.deltaTime;
            if (feedbackTimer <= 0 && feedbackText != null)
                feedbackText.text = "";
        }

        // ESC closes store
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            CloseStore();
    }

    //Public 

    public void OpenStore()
    {
        if (StoreManager.Instance == null) return;
        isOpen = true;
        storePanel.SetActive(true);
        RefreshCards();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseStore()
    {
        isOpen = false;
        storePanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public bool IsOpen => isOpen;

    //Build (called once) 

    private void BuildPanel()
    {
        storePanel = new GameObject("StorePanel");
        storePanel.transform.SetParent(transform, false);

        RectTransform pr = storePanel.AddComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.1f, 0.1f);
        pr.anchorMax = new Vector2(0.9f, 0.9f);
        pr.offsetMin = pr.offsetMax = Vector2.zero;

        storePanel.AddComponent<Image>().color = BG;

        //Title
        GameObject titleObj = new("StoreTitle");
        titleObj.transform.SetParent(storePanel.transform, false);

        RectTransform tr = titleObj.AddComponent<RectTransform>();
        tr.anchorMin = new Vector2(0, 1);
        tr.anchorMax = new Vector2(1, 1);
        tr.pivot = new Vector2(0.5f, 1);
        tr.anchoredPosition = new Vector2(0, -15);
        tr.sizeDelta = new Vector2(0, 50);

        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "SHOP  —  spend throwables, gain throwables  |  [ESC] close";
        titleText.fontSize = 22;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;

        //Feedback label 
        GameObject fbObj = new("Feedback");
        fbObj.transform.SetParent(storePanel.transform, false);

        RectTransform fr = fbObj.AddComponent<RectTransform>();
        fr.anchorMin = new Vector2(0, 0);
        fr.anchorMax = new Vector2(1, 0);
        fr.pivot = new Vector2(0.5f, 0);
        fr.anchoredPosition = new Vector2(0, 20);
        fr.sizeDelta = new Vector2(0, 30);

        feedbackText = fbObj.AddComponent<TextMeshProUGUI>();
        feedbackText.fontSize = 16;
        feedbackText.color = Gold;
        feedbackText.alignment = TextAlignmentOptions.Center;

        //Close button
        CreateCloseButton();

        //Card container 
        cardContainer = new GameObject("CardContainer");
        cardContainer.transform.SetParent(storePanel.transform, false);

        RectTransform cr = cardContainer.AddComponent<RectTransform>();
        cr.anchorMin = new Vector2(0, 0);
        cr.anchorMax = new Vector2(1, 1);
        cr.offsetMin = new Vector2(20, 60);
        cr.offsetMax = new Vector2(-20, -70);

        storePanel.SetActive(false);
    }

    private void CreateCloseButton()
    {
        GameObject btnObj = new("CloseBtn");
        btnObj.transform.SetParent(storePanel.transform, false);

        RectTransform br = btnObj.AddComponent<RectTransform>();
        br.anchorMin = new Vector2(1, 1);
        br.anchorMax = new Vector2(1, 1);
        br.pivot = new Vector2(1, 1);
        br.anchoredPosition = new Vector2(-15, -15);
        br.sizeDelta = new Vector2(100, 36);

        btnObj.AddComponent<Image>().color = new Color(0.5f, 0.1f, 0.1f, 1f);

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(CloseStore);

        AddLabel(btnObj, "Close", 16, Color.white);
    }

    //Refresh (called on purchase / inventory change)

    private void RefreshCards()
    {
        if (!isOpen) return;

        foreach (var c in cards) Destroy(c);
        cards.Clear();

        if (StoreManager.Instance == null) return;

        var catalogue = StoreManager.Instance.GetCatalogue();

        for (int i = 0; i < catalogue.Count; i++)
            cards.Add(BuildCard(catalogue[i], i));
    }

    //Card builder

    private GameObject BuildCard(StoreItem entry, int index)
    {
        int row = index / cardsPerRow;
        int col = index % cardsPerRow;

        float x = col * (cardWidth + cardSpacing);
        float y = -row * (cardHeight + cardSpacing);

        //Card root
        GameObject card = new($"Card_{entry.rewardItemName}");
        card.transform.SetParent(cardContainer.transform, false);

        RectTransform cr = card.AddComponent<RectTransform>();
        cr.anchorMin = new Vector2(0, 1);
        cr.anchorMax = new Vector2(0, 1);
        cr.pivot = new Vector2(0, 1);
        cr.anchoredPosition = new Vector2(x, y);
        cr.sizeDelta = new Vector2(cardWidth, cardHeight);

        card.AddComponent<Image>().color = CardBG;

        // Gold border if throwable
        if (entry.isThrowable)
            AddBorder(card, new Color(1f, 0.8f, 0f, 0.8f), 2f);

        //Icon
        if (entry.rewardIcon != null)
        {
            GameObject iconObj = new("Icon");
            iconObj.transform.SetParent(card.transform, false);

            RectTransform ir = iconObj.AddComponent<RectTransform>();
            ir.anchorMin = new Vector2(0.1f, 0.5f);
            ir.anchorMax = new Vector2(0.9f, 0.95f);
            ir.offsetMin = ir.offsetMax = Vector2.zero;

            Image img = iconObj.AddComponent<Image>();
            img.sprite = entry.rewardIcon;
            img.preserveAspect = true;
        }

        // Item name
        GameObject nameObj = new("Name");
        nameObj.transform.SetParent(card.transform, false);

        RectTransform nr = nameObj.AddComponent<RectTransform>();
        nr.anchorMin = new Vector2(0, 0.48f);
        nr.anchorMax = new Vector2(1, 0.60f);
        nr.offsetMin = nr.offsetMax = Vector2.zero;

        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = $"{entry.rewardItemName}  ×{entry.rewardQuantity}";
        nameText.fontSize = 14;
        nameText.color = Color.white;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.fontStyle = FontStyles.Bold;

        //Cost label 
        GameObject costObj = new("Cost");
        costObj.transform.SetParent(card.transform, false);

        RectTransform costr = costObj.AddComponent<RectTransform>();
        costr.anchorMin = new Vector2(0, 0.35f);
        costr.anchorMax = new Vector2(1, 0.47f);
        costr.offsetMin = costr.offsetMax = Vector2.zero;

        TextMeshProUGUI costText = costObj.AddComponent<TextMeshProUGUI>();
        costText.text = $"Cost: {entry.costQuantity}× {entry.costItemName}";
        costText.fontSize = 12;
        costText.color = Gold;
        costText.alignment = TextAlignmentOptions.Center;

        // Stock label 
        int stock = StoreManager.Instance.GetStock(index);
        GameObject stockObj = new("Stock");
        stockObj.transform.SetParent(card.transform, false);

        RectTransform sr = stockObj.AddComponent<RectTransform>();
        sr.anchorMin = new Vector2(0, 0.25f);
        sr.anchorMax = new Vector2(1, 0.35f);
        sr.offsetMin = sr.offsetMax = Vector2.zero;

        TextMeshProUGUI stockText = stockObj.AddComponent<TextMeshProUGUI>();
        stockText.text = stock < 0 ? "Stock: ∞" : $"Stock: {stock}";
        stockText.fontSize = 11;
        stockText.color = stock == 0 ? Color.red : new Color(0.7f, 0.7f, 0.7f);
        stockText.alignment = TextAlignmentOptions.Center;

        //Buy button
        bool canAfford = InventoryManager.Instance != null &&
                         InventoryManager.Instance.HasItem(entry.costItemName, entry.costQuantity);
        bool inStock = stock != 0;

        GameObject btnObj = new("BuyBtn");
        btnObj.transform.SetParent(card.transform, false);

        RectTransform btnr = btnObj.AddComponent<RectTransform>();
        btnr.anchorMin = new Vector2(0.05f, 0.02f);
        btnr.anchorMax = new Vector2(0.95f, 0.22f);
        btnr.offsetMin = btnr.offsetMax = Vector2.zero;

        Color btnColor = (!inStock) ? BtnNoStock : (!canAfford) ? BtnNoFunds : BtnNormal;
        btnObj.AddComponent<Image>().color = btnColor;

        Button buyBtn = btnObj.AddComponent<Button>();
        int capturedIndex = index;
        buyBtn.onClick.AddListener(() => OnBuyClicked(capturedIndex));
        buyBtn.interactable = inStock && canAfford;

        string btnLabel = !inStock ? "Out of Stock" : !canAfford ? "Can't Afford" : "Buy";
        AddLabel(btnObj, btnLabel, 14, Color.white);

        return card;
    }

    //Button callback

    private void OnBuyClicked(int index)
    {
        bool success = StoreManager.Instance.TryPurchase(index);

        if (feedbackText != null)
        {
            var entry = StoreManager.Instance.GetCatalogue()[index];
            feedbackText.text = success
                ? $"Bought {entry.rewardQuantity}× {entry.rewardItemName}!"
                : $"Not enough {entry.costItemName}!";
            feedbackText.color = success ? Gold : Color.red;
            feedbackTimer = 2f;
        }
    }

    //Helpers

    private void AddLabel(GameObject parent, string text, int size, Color color)
    {
        GameObject obj = new("Label");
        obj.transform.SetParent(parent.transform, false);

        RectTransform r = obj.AddComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    private void AddBorder(GameObject parent, Color color, float thickness)
    {
        string[] sides = { "Top", "Bottom", "Left", "Right" };
        foreach (string side in sides)
        {
            GameObject b = new($"Border{side}");
            b.transform.SetParent(parent.transform, false);

            RectTransform rt = b.AddComponent<RectTransform>();
            b.AddComponent<Image>().color = color;

            switch (side)
            {
                case "Top":
                    rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(0.5f, 1);
                    rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(0, thickness);
                    break;
                case "Bottom":
                    rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0);
                    rt.pivot = new Vector2(0.5f, 0);
                    rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(0, thickness);
                    break;
                case "Left":
                    rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(0, 1);
                    rt.pivot = new Vector2(0, 0.5f);
                    rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(thickness, 0);
                    break;
                case "Right":
                    rt.anchorMin = new Vector2(1, 0); rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(1, 0.5f);
                    rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(thickness, 0);
                    break;
            }
        }
    }

    private void OnDestroy()
    {
        if (StoreManager.Instance != null)
            StoreManager.Instance.OnStoreChanged -= RefreshCards;
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= RefreshCards;
    }
}