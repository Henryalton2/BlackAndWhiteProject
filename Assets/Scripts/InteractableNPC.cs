using UnityEngine;
using TMPro;
using System.Collections;

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
    [SerializeField] [TextArea(3, 10)] private string[] dialogueLines;
    [SerializeField] private float typingSpeed = 0.05f;

    [Header("UI References")]
    [SerializeField] private GameObject promptUI;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private GameObject dialogueUI;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI npcNameText;

    private Transform player;
    private bool isInRange;
    private bool isDialogueActive;
    private int currentLineIndex;
    private Coroutine typingCoroutine;
    private string currentDisplayName;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        currentDisplayName = string.IsNullOrEmpty(initialName) ? npcName : initialName;

        if (promptUI != null) promptUI.SetActive(false);
        if (dialogueUI != null) dialogueUI.SetActive(false);

        if (promptText != null)
            promptText.text = $"Press {interactKey} to talk to {currentDisplayName}";
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
            else
                ContinueDialogue();
        }
    }

    void StartDialogue()
    {
        if (dialogueLines.Length == 0) return;

        isDialogueActive = true;
        currentLineIndex = 0;

        if (promptUI != null) promptUI.SetActive(false);
        if (dialogueUI != null) dialogueUI.SetActive(true);

        if (npcNameText != null)
            npcNameText.text = currentDisplayName;

        DisplayLine(dialogueLines[currentLineIndex]);
    }

    void ContinueDialogue()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            dialogueText.text = dialogueLines[currentLineIndex];
            typingCoroutine = null;
            return;
        }

        currentLineIndex++;

        if (revealNameAtLine >= 0 && currentLineIndex == revealNameAtLine)
        {
            currentDisplayName = npcName;

            if (npcNameText != null)
                npcNameText.text = currentDisplayName;

            if (promptText != null)
                promptText.text = $"Press {interactKey} to talk to {currentDisplayName}";
        }

        if (currentLineIndex < dialogueLines.Length)
            DisplayLine(dialogueLines[currentLineIndex]);
        else
            EndDialogue();
    }

    void DisplayLine(string line)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

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

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (dialogueUI != null) dialogueUI.SetActive(false);

        if (promptUI != null && isInRange)
            promptUI.SetActive(true);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 interactionPoint = transform.position + interactionOffset;
        Gizmos.DrawWireSphere(interactionPoint, interactionDistance);
    }
}
