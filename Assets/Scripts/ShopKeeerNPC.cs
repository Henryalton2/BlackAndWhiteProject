using UnityEngine;
using TMPro;
/// Attach to your shopkeeper NPC.
/// Requires a trigger collider on the NPC.
/// Requires StoreUI to exist somewhere on the Canvas.
public class ShopkeeperNPC : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactRange = 3f;   // only used if not using trigger collider

    [Header("Prompt")]
    [SerializeField] private string promptMessage = "Press [E] to shop";
    [SerializeField] private Vector3 promptOffset = new(0, 2.5f, 0);

    //Runtime
    private StoreUI storeUI;
    private bool playerNearby = false;

    // World-space prompt label
    private GameObject promptObj;
    private TextMeshPro promptTMP;     // 3-D world-space TMP (not UGUI)

    
    private void Start()
    {
        storeUI = FindObjectOfType<StoreUI>();

        if (storeUI == null)
            Debug.LogWarning("[ShopkeeperNPC] No StoreUI found in scene.");

        BuildPromptLabel();
    }

    private void Update()
    {
        // Fallback proximity check (works even without a trigger collider)
        if (Camera.main != null)
        {
            float dist = Vector3.Distance(transform.position, Camera.main.transform.position);
            playerNearby = dist <= interactRange;
        }

        // Show / hide prompt
        if (promptObj != null)
        {
            promptObj.SetActive(playerNearby && (storeUI == null || !storeUI.IsOpen));

            // Always face the camera
            if (Camera.main != null)
                promptObj.transform.rotation =
                    Quaternion.LookRotation(promptObj.transform.position - Camera.main.transform.position);
        }

        // Interact
        if (playerNearby && Input.GetKeyDown(interactKey))
        {
            if (storeUI == null) return;

            if (storeUI.IsOpen)
                storeUI.CloseStore();
            else
                storeUI.OpenStore();
        }
    }

    //Trigger alternative (optional — works alongside proximity)

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) playerNearby = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = false;
            storeUI?.CloseStore();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerNearby = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = false;
            storeUI?.CloseStore();
        }
    }

    //Prompt label

    private void BuildPromptLabel()
    {
        promptObj = new GameObject("ShopPrompt");
        promptObj.transform.SetParent(transform, false);
        promptObj.transform.localPosition = promptOffset;

        // Use world-space TextMeshPro (not UGUI) so it floats above the NPC
        promptTMP = promptObj.AddComponent<TextMeshPro>();
        promptTMP.text = promptMessage;
        promptTMP.fontSize = 3f;
        promptTMP.color = new Color(1f, 0.92f, 0.3f);
        promptTMP.alignment = TextAlignmentOptions.Center;
        promptTMP.fontStyle = FontStyles.Bold;

        promptObj.SetActive(false);
    }

    //Editor gizmo
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.3f);
        Gizmos.DrawSphere(transform.position, interactRange);
    }
#endif
}