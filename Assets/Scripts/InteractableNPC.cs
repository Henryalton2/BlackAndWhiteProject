using UnityEngine;
using TMPro;
using System.Collections;

public class InteractableNPC : MonoBehaviour
{
    [Header("NPC Settings")]
    [SerializeField] private string npcName = "NPC";
    [SerializeField] private string initialName = "???";
    [SerializeField] private int revealNameAtLine = -1; // -1 means never change, 0+ is the line number
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private bool showDebug = false;

    [Header("Dialogue")]
    [SerializeField][TextArea(3, 10)] private string[] dialogueLines;
    [SerializeField] private float typingSpeed = 0.05f;

    [Header("UI References")]
    [SerializeField] private GameObject promptUI;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private GameObject dialogueUI;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI npcNameText;

    private Transform player;
    private bool isInRange = false;
    private bool isDialogueActive = false;
    private int currentLineIndex = 0;
    private Coroutine typingCoroutine;
    private string currentDisplayName;

    void Start()
    {
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        // Set initial display name
        currentDisplayName = string.IsNullOrEmpty(initialName) ? npcName : initialName;

        // Hide UI at start
        if (promptUI != null) promptUI.SetActive(false);
        if (dialogueUI != null) dialogueUI.SetActive(false);

        // Set prompt text
        if (promptText != null)
        {
            promptText.text = $"Press {interactKey} to talk to {currentDisplayName}";
        }
    }

    void Update()
    {
        if (player == null)
        {
            if (showDebug) Debug.LogWarning($"{npcName}: Player not found!");
            return;
        }

        // Check distance to player
        float distance = Vector3.Distance(transform.position, player.position);
        isInRange = distance <= interactionDistance;

        if (showDebug)
        {
            Debug.Log($"{npcName}: Distance = {distance:F2}, In Range = {isInRange}, Prompt UI = {promptUI != null}");
        }

        // Cancel dialogue if player walks away
        if (isDialogueActive && !isInRange)
        {
            EndDialogue();
            return;
        }

        // Show/hide prompt
        if (promptUI != null && !isDialogueActive)
        {
            promptUI.SetActive(isInRange);
        }

        // Handle interaction
        if (isInRange && Input.GetKeyDown(interactKey))
        {
            if (!isDialogueActive)
            {
                StartDialogue();
            }
            else
            {
                ContinueDialogue();
            }
        }
    }

    void StartDialogue()
    {
        if (dialogueLines.Length == 0) return;

        isDialogueActive = true;
        currentLineIndex = 0;

        // Hide prompt, show dialogue
        if (promptUI != null) promptUI.SetActive(false);
        if (dialogueUI != null) dialogueUI.SetActive(true);

        // Set NPC name
        if (npcNameText != null)
        {
            npcNameText.text = currentDisplayName;
        }

        // Start typing first line
        DisplayLine(dialogueLines[currentLineIndex]);
    }

    void ContinueDialogue()
    {
        // If still typing, complete the line instantly
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            dialogueText.text = dialogueLines[currentLineIndex];
            typingCoroutine = null;
            return;
        }

        // Move to next line first
        currentLineIndex++;

        // Check if we should reveal the name at this line
        if (revealNameAtLine >= 0 && currentLineIndex == revealNameAtLine)
        {
            currentDisplayName = npcName;
            if (npcNameText != null)
            {
                npcNameText.text = currentDisplayName;
            }
            // Update prompt text for future interactions
            if (promptText != null)
            {
                promptText.text = $"Press {interactKey} to talk to {currentDisplayName}";
            }

            if (showDebug)
            {
                Debug.Log($"Name revealed! Changed from {initialName} to {npcName}");
            }
        }

        if (currentLineIndex < dialogueLines.Length)
        {
            DisplayLine(dialogueLines[currentLineIndex]);
        }
        else
        {
            EndDialogue();
        }
    }

    void DisplayLine(string line)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        typingCoroutine = StartCoroutine(TypeText(line));
    }

    IEnumerator TypeText(string line)
    {
        dialogueText.text = "";

        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        typingCoroutine = null;
    }

    void EndDialogue()
    {
        isDialogueActive = false;
        currentLineIndex = 0;

        // Stop any typing animation
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (dialogueUI != null) dialogueUI.SetActive(false);

        // Show prompt again if still in range
        if (promptUI != null && isInRange)
        {
            promptUI.SetActive(true);
        }
    }

    // Draw interaction range in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}