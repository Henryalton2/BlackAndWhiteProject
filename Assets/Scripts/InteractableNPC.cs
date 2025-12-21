using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class DialogueSection
{
    public string sectionName;
    [TextArea(3, 10)] public string[] dialogueLines;
    public int revealNameAtLine = -1;
    public bool hasBeenPlayed = false;
}

public class InteractableNPC : MonoBehaviour
{
    [Header("NPC Settings")]
    [SerializeField] private string npcName = "NPC";
    [SerializeField] private string initialName = "???";
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private bool showDebug = false;

    [Header("Dialogue Sections")]
    [SerializeField] private List<DialogueSection> dialogueSections = new List<DialogueSection>();
    [SerializeField] private int currentSectionIndex = 0;

    [Header("Repeat Dialogue")]
    [Tooltip("This dialogue plays when talking to NPC after completing current section but before unlocking next")]
    [SerializeField][TextArea(3, 10)] private string[] repeatDialogue;

    [Header("Settings")]
    [SerializeField] private float typingSpeed = 0.05f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip typingSFX;
    [SerializeField] private bool playSFXOnEveryLetter = true;
    [Tooltip("Play sound every N letters (1 = every letter, 2 = every other letter, etc.)")]
    [SerializeField] private int sfxFrequency = 1;

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
    private string[] currentDialogueLines;

    void Start()
    {
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        // Set up audio source if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && typingSFX != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
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
            Debug.Log($"{npcName}: Distance = {distance:F2}, In Range = {isInRange}, Current Section = {currentSectionIndex}");
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
        // Determine which dialogue to play
        if (currentSectionIndex < dialogueSections.Count)
        {
            DialogueSection currentSection = dialogueSections[currentSectionIndex];

            // If this section hasn't been played, play it
            if (!currentSection.hasBeenPlayed)
            {
                currentDialogueLines = currentSection.dialogueLines;

                if (showDebug)
                {
                    Debug.Log($"Playing section: {currentSection.sectionName}");
                }
            }
            else
            {
                // Section already played, use repeat dialogue
                currentDialogueLines = repeatDialogue;

                if (showDebug)
                {
                    Debug.Log($"Section '{currentSection.sectionName}' already played. Using repeat dialogue.");
                }
            }
        }
        else
        {
            // All sections completed, use repeat dialogue
            currentDialogueLines = repeatDialogue;

            if (showDebug)
            {
                Debug.Log("All sections completed. Using repeat dialogue.");
            }
        }

        if (currentDialogueLines == null || currentDialogueLines.Length == 0)
        {
            if (showDebug) Debug.LogWarning("No dialogue lines available!");
            return;
        }

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
        DisplayLine(currentDialogueLines[currentLineIndex]);
    }

    void ContinueDialogue()
    {
        // If still typing, complete the line instantly
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            dialogueText.text = currentDialogueLines[currentLineIndex];
            typingCoroutine = null;
            return;
        }

        // Move to next line first
        currentLineIndex++;

        // Check if we should reveal the name at this line (only for section dialogue)
        if (currentSectionIndex < dialogueSections.Count &&
            !dialogueSections[currentSectionIndex].hasBeenPlayed)
        {
            DialogueSection currentSection = dialogueSections[currentSectionIndex];

            if (currentSection.revealNameAtLine >= 0 && currentLineIndex == currentSection.revealNameAtLine)
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
        }

        if (currentLineIndex < currentDialogueLines.Length)
        {
            DisplayLine(currentDialogueLines[currentLineIndex]);
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
        int letterCount = 0;

        foreach (char c in line)
        {
            dialogueText.text += c;
            letterCount++;

            // Play typing sound effect
            if (playSFXOnEveryLetter && typingSFX != null && audioSource != null)
            {
                // Check if we should play sound based on frequency
                if (letterCount % sfxFrequency == 0)
                {
                    audioSource.PlayOneShot(typingSFX);
                }
            }

            yield return new WaitForSeconds(typingSpeed);
        }

        typingCoroutine = null;
    }

    void EndDialogue()
    {
        // Mark current section as played if it was a section dialogue
        if (currentSectionIndex < dialogueSections.Count &&
            !dialogueSections[currentSectionIndex].hasBeenPlayed &&
            currentDialogueLines == dialogueSections[currentSectionIndex].dialogueLines)
        {
            dialogueSections[currentSectionIndex].hasBeenPlayed = true;

            if (showDebug)
            {
                Debug.Log($"Section '{dialogueSections[currentSectionIndex].sectionName}' marked as complete.");
            }
        }

        isDialogueActive = false;
        currentLineIndex = 0;

        if (dialogueUI != null) dialogueUI.SetActive(false);

        // Show prompt again if still in range
        if (promptUI != null && isInRange)
        {
            promptUI.SetActive(true);
        }
    }

    // Public method to unlock the next dialogue section
    public void UnlockNextSection()
    {
        if (currentSectionIndex < dialogueSections.Count - 1)
        {
            currentSectionIndex++;

            if (showDebug)
            {
                Debug.Log($"Unlocked section {currentSectionIndex}: {dialogueSections[currentSectionIndex].sectionName}");
            }
        }
        else
        {
            if (showDebug)
            {
                Debug.Log("No more sections to unlock.");
            }
        }
    }

    // Public method to unlock a specific section by index
    public void UnlockSection(int sectionIndex)
    {
        if (sectionIndex >= 0 && sectionIndex < dialogueSections.Count)
        {
            currentSectionIndex = sectionIndex;

            if (showDebug)
            {
                Debug.Log($"Unlocked section {sectionIndex}: {dialogueSections[sectionIndex].sectionName}");
            }
        }
    }

    // Public method to check current section
    public int GetCurrentSectionIndex()
    {
        return currentSectionIndex;
    }

    // Public method to check if a section has been played
    public bool HasSectionBeenPlayed(int sectionIndex)
    {
        if (sectionIndex >= 0 && sectionIndex < dialogueSections.Count)
        {
            return dialogueSections[sectionIndex].hasBeenPlayed;
        }
        return false;
    }

    // Draw interaction range in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}