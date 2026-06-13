using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.UI;

[System.Serializable]
public class DialogueChoice
{
    public string choiceText;
    public int nextLineIndex = -1; // -1 means end dialogue, otherwise jump to that line number
    public UnityEvent onChoiceSelected;
}

[System.Serializable]
public class DialogueLine
{
    [TextArea(3, 10)] public string text;
    public bool hasChoices;
    public DialogueChoice[] choices;
}

public class InteractableNPC : MonoBehaviour
{
    [Header("NPC Settings")]
    [SerializeField] private string npcName = "NPC";
    [SerializeField] private string initialName = "???";
    [SerializeField] private int revealNameAtLine = -1;
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private Vector3 interactionOffset;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private bool showDebug = false;

    [Header("Dialogue")]
    [SerializeField] private DialogueLine[] dialogueLines;
    [SerializeField] private float typingSpeed = 0.05f;

    [Header("UI References")]
    [SerializeField] private GameObject promptUI;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private GameObject dialogueUI;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI npcNameText;

    [Header("Choice Buttons - Pre-made in Unity")]
    [SerializeField] private GameObject choiceContainer;
    [SerializeField] private Button choiceButton1;
    [SerializeField] private Button choiceButton2;
    [SerializeField] private Button choiceButton3;
    [SerializeField] private Button choiceButton4;

    [Header("Pause Menu (Optional - to hide during dialogue)")]
    [SerializeField] private GameObject pauseMenu;

    [Header("Camera Lock (Optional - drag your camera here)")]
    [SerializeField] private GameObject cameraToLock; // Just drag your Main Camera here!

    private Transform player;
    private bool isInRange;
    private bool isDialogueActive;
    private int currentLineIndex;
    private Coroutine typingCoroutine;
    private string currentDisplayName;
    private bool isShowingChoices;
    private DialogueChoice[] currentChoices;
    private Button[] allButtons;

    void Start()
    {
        ValidateReferences();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else if (showDebug)
            Debug.LogWarning($"[{gameObject.name}] No GameObject with 'Player' tag found!");

        currentDisplayName = string.IsNullOrEmpty(initialName) ? npcName : initialName;

        if (promptUI != null) promptUI.SetActive(false);
        if (dialogueUI != null) dialogueUI.SetActive(false);
        if (choiceContainer != null) choiceContainer.SetActive(false);

        if (promptText != null)
            promptText.text = $"Press {interactKey} to talk to {currentDisplayName}";

        // Store all buttons in array for easy access
        allButtons = new Button[] { choiceButton1, choiceButton2, choiceButton3, choiceButton4 };

        // Set up button click listeners
        SetupButtons();
    }

    void SetupButtons()
    {
        Debug.Log($"[{gameObject.name}] Setting up buttons...");

        // Check for EventSystem
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            Debug.LogError($"[{gameObject.name}] NO EVENTSYSTEM FOUND! Buttons will not work. Creating one now.");
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        else
        {
            Debug.Log($"[{gameObject.name}] EventSystem found: {UnityEngine.EventSystems.EventSystem.current.name}");
        }

        // Check for GraphicRaycaster on Canvas
        if (choiceContainer != null)
        {
            Canvas canvas = choiceContainer.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster == null)
                {
                    Debug.LogError($"[{gameObject.name}] Canvas '{canvas.name}' has NO GraphicRaycaster! Adding one now.");
                    canvas.gameObject.AddComponent<GraphicRaycaster>();
                }
                else
                {
                    Debug.Log($"[{gameObject.name}] GraphicRaycaster found on Canvas: {canvas.name}");
                }
            }
        }

        for (int i = 0; i < allButtons.Length; i++)
        {
            if (allButtons[i] != null)
            {
                int index = i; // Capture index for closure
                allButtons[i].onClick.RemoveAllListeners();
                allButtons[i].onClick.AddListener(() => {
                    Debug.Log($"[{gameObject.name}] *** BUTTON {index + 1} CLICKED! ***");
                    SelectChoice(index);
                });
                allButtons[i].gameObject.SetActive(false); // Hide all buttons initially
                allButtons[i].interactable = true; // Ensure button is interactable

                Debug.Log($"[{gameObject.name}] Button {i + 1} setup complete. Interactable: {allButtons[i].interactable}");
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] Button {i + 1} is NULL!");
            }
        }
    }

    void ValidateReferences()
    {
        if (dialogueLines == null || dialogueLines.Length == 0)
            Debug.LogError($"[{gameObject.name}] No dialogue lines assigned!");

        if (promptUI == null)
            Debug.LogWarning($"[{gameObject.name}] Prompt UI not assigned!");

        if (dialogueUI == null)
            Debug.LogWarning($"[{gameObject.name}] Dialogue UI not assigned!");

        if (dialogueText == null)
            Debug.LogError($"[{gameObject.name}] Dialogue Text not assigned!");

        if (npcNameText == null)
            Debug.LogWarning($"[{gameObject.name}] NPC Name Text not assigned - name display will not work!");

        if (choiceContainer == null)
            Debug.LogWarning($"[{gameObject.name}] Choice Container not assigned - choices will not work!");

        if (choiceButton1 == null)
            Debug.LogWarning($"[{gameObject.name}] Choice Button 1 not assigned!");
        if (choiceButton2 == null)
            Debug.LogWarning($"[{gameObject.name}] Choice Button 2 not assigned!");
        if (choiceButton3 == null)
            Debug.LogWarning($"[{gameObject.name}] Choice Button 3 not assigned!");
        if (choiceButton4 == null)
            Debug.LogWarning($"[{gameObject.name}] Choice Button 4 not assigned!");
    }

    void Update()
    {
        if (player == null) return;

        Vector3 interactionPoint = transform.position + interactionOffset;
        float distance = Vector3.Distance(interactionPoint, player.position);
        isInRange = distance <= interactionDistance;

        if (isDialogueActive && !isInRange)
        {
            EndDialogue();
            return;
        }

        if (promptUI != null && !isDialogueActive)
            promptUI.SetActive(isInRange);

        if (isInRange && Input.GetKeyDown(interactKey))
        {
            if (!isDialogueActive)
                StartDialogue();
            else if (!isShowingChoices)
                ContinueDialogue();
        }
    }

    void StartDialogue()
    {
        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            Debug.LogError($"[{gameObject.name}] Cannot start dialogue - no dialogue lines!");
            return;
        }

        isDialogueActive = true;
        currentLineIndex = 0;

        if (promptUI != null) promptUI.SetActive(false);
        if (dialogueUI != null) dialogueUI.SetActive(true);

        // Hide pause menu if it exists
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
            Debug.Log($"[{gameObject.name}] Hiding pause menu during dialogue");
        }

        // Disable camera control scripts
        SetCameraControlEnabled(false);

        UpdateNPCName(currentDisplayName);

        DisplayLine(currentLineIndex);
    }

    void ContinueDialogue()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            if (dialogueText != null && currentLineIndex < dialogueLines.Length)
                dialogueText.text = dialogueLines[currentLineIndex].text;
            typingCoroutine = null;

            // Check if this line has choices
            if (currentLineIndex < dialogueLines.Length && dialogueLines[currentLineIndex].hasChoices)
            {
                ShowChoices(dialogueLines[currentLineIndex].choices);
            }
            return;
        }

        currentLineIndex++;

        if (revealNameAtLine >= 0 && currentLineIndex == revealNameAtLine)
        {
            currentDisplayName = npcName;
            UpdateNPCName(currentDisplayName);

            if (promptText != null)
                promptText.text = $"Press {interactKey} to talk to {currentDisplayName}";
        }

        if (currentLineIndex < dialogueLines.Length)
            DisplayLine(currentLineIndex);
        else
            EndDialogue();
    }

    void UpdateNPCName(string name)
    {
        if (npcNameText != null)
            npcNameText.text = name;
    }

    void DisplayLine(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= dialogueLines.Length)
        {
            Debug.LogError($"[{gameObject.name}] Invalid dialogue line index: {lineIndex}");
            EndDialogue();
            return;
        }

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(dialogueLines[lineIndex].text, lineIndex));
    }

    IEnumerator TypeText(string line, int lineIndex)
    {
        if (dialogueText == null)
        {
            Debug.LogError($"[{gameObject.name}] Dialogue Text is null!");
            yield break;
        }

        dialogueText.text = "";

        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        typingCoroutine = null;

        // Show choices after typing is complete
        if (lineIndex < dialogueLines.Length && dialogueLines[lineIndex].hasChoices)
        {
            ShowChoices(dialogueLines[lineIndex].choices);
        }
    }

    void ShowChoices(DialogueChoice[] choices)
    {
        if (choiceContainer == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Cannot show choices - missing Choice Container!");
            return;
        }

        if (choices == null || choices.Length == 0)
        {
            Debug.LogWarning($"[{gameObject.name}] No choices to display!");
            return;
        }

        currentChoices = choices;
        isShowingChoices = true;
        choiceContainer.SetActive(true);

        Debug.Log($"[{gameObject.name}] ShowChoices: Displaying {choices.Length} buttons");

        // Show and configure buttons based on number of choices
        for (int i = 0; i < allButtons.Length; i++)
        {
            if (allButtons[i] != null)
            {
                if (i < choices.Length)
                {
                    // Enable button and set text
                    allButtons[i].gameObject.SetActive(true);
                    allButtons[i].interactable = true;

                    TextMeshProUGUI buttonText = allButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        buttonText.text = choices[i].choiceText;
                        Debug.Log($"[{gameObject.name}] Button {i + 1} enabled with text: '{choices[i].choiceText}'");
                    }
                    else
                    {
                        Debug.LogWarning($"[{gameObject.name}] Button {i + 1} has no TextMeshProUGUI child!");
                    }

                    // Debug button state
                    Image btnImage = allButtons[i].GetComponent<Image>();
                    Debug.Log($"[{gameObject.name}] Button {i + 1} - Active: {allButtons[i].gameObject.activeSelf}, Interactable: {allButtons[i].interactable}, Has Image: {btnImage != null}, Image Raycast: {(btnImage != null ? btnImage.raycastTarget.ToString() : "N/A")}");
                }
                else
                {
                    // Hide unused buttons
                    allButtons[i].gameObject.SetActive(false);
                }
            }
        }

        if (showDebug)
            Debug.Log($"[{gameObject.name}] Showing {choices.Length} choice buttons");
    }

    void SelectChoice(int choiceIndex)
    {
        if (currentChoices == null || choiceIndex >= currentChoices.Length)
        {
            Debug.LogWarning($"[{gameObject.name}] Invalid choice index: {choiceIndex}");
            return;
        }

        DialogueChoice selectedChoice = currentChoices[choiceIndex];

        if (showDebug)
            Debug.Log($"[{gameObject.name}] Choice {choiceIndex + 1} selected: '{selectedChoice.choiceText}' -> Jumping to Line {selectedChoice.nextLineIndex}");

        // Invoke the choice event
        selectedChoice.onChoiceSelected?.Invoke();

        ClearChoices();
        isShowingChoices = false;

        // Move to the next line based on choice
        if (selectedChoice.nextLineIndex == -1)
        {
            if (showDebug)
                Debug.Log($"[{gameObject.name}] Choice ends dialogue (nextLineIndex = -1)");
            EndDialogue();
        }
        else
        {
            currentLineIndex = selectedChoice.nextLineIndex;
            if (currentLineIndex < dialogueLines.Length)
            {
                if (showDebug)
                    Debug.Log($"[{gameObject.name}] Jumping to dialogue line {currentLineIndex}");
                DisplayLine(currentLineIndex);
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] Choice leads to invalid line index: {selectedChoice.nextLineIndex} (max: {dialogueLines.Length - 1})");
                EndDialogue();
            }
        }
    }

    void ClearChoices()
    {
        currentChoices = null;

        // Hide all buttons
        for (int i = 0; i < allButtons.Length; i++)
        {
            if (allButtons[i] != null)
            {
                allButtons[i].gameObject.SetActive(false);
            }
        }

        if (choiceContainer != null)
            choiceContainer.SetActive(false);
    }

    void EndDialogue()
    {
        if (showDebug)
            Debug.Log($"[{gameObject.name}] Dialogue ended");

        isDialogueActive = false;
        isShowingChoices = false;
        currentLineIndex = 0;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        ClearChoices();

        if (dialogueUI != null) dialogueUI.SetActive(false);

        if (promptUI != null && isInRange)
            promptUI.SetActive(true);

        // Re-enable camera control scripts
        SetCameraControlEnabled(true);
    }

    void SetCameraControlEnabled(bool enabled)
    {
        if (cameraToLock == null) return;

        // Get all MonoBehaviour scripts on the camera
        MonoBehaviour[] scripts = cameraToLock.GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour script in scripts)
        {
            if (script != null)
            {
                // Skip built-in Unity components that shouldn't be disabled
                string scriptType = script.GetType().Name;
                if (scriptType != "Camera" &&
                    scriptType != "AudioListener" &&
                    scriptType != "Transform" &&
                    scriptType != "FlareLayer")
                {
                    script.enabled = enabled;
                    if (showDebug)
                        Debug.Log($"[{gameObject.name}] Camera script '{scriptType}' enabled: {enabled}");
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 interactionPoint = transform.position + interactionOffset;
        Gizmos.DrawWireSphere(interactionPoint, interactionDistance);
    }
}