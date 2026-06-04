using System.Collections;
using UnityEngine;

public class ShovelController : MonoBehaviour
{
    public enum ShovelMode { Dig, Throw, InFlight }

    [Header("References")]
    public Camera playerCamera;
    public Transform shovelVisual;
    public GameObject thrownShovelPrefab;

    [Header("Inventory")]
    [SerializeField] private string shovelItemName = "Shovel";
    [SerializeField] private Sprite shovelIcon;

    [Header("Dig")]
    public float digReach = 3f;
    public LayerMask diggableLayer;

    [Header("Throw")]
    public float throwForce = 22f;
    public LayerMask penetrableLayer;

    [Header("Hold Poses (local to shovelVisual parent)")]
    public Vector3 digLocalPos   = new Vector3(0.1f, -0.2f, 0.5f);
    public Vector3 digLocalEuler = new Vector3(5f, 5f, -35f);
    public Vector3 throwLocalPos   = new Vector3(0.1f, 0.25f, 0.45f);
    public Vector3 throwLocalEuler = new Vector3(-85f, 5f, 5f);
    [Range(1f, 25f)] public float poseLerpSpeed = 14f;

    [Header("Dig Animation")]
    public float digSwingDuration = 0.35f;

    [Header("Catch Boost")]
    [Tooltip("Speed added in the direction the shovel flew from when caught.")]
    public float catchBoostForce = 5f;

    public ShovelMode Mode { get; private set; } = ShovelMode.Dig;

    private bool _isEquipped;
    private ThrownShovel _activeShovel;
    private Coroutine _digCoroutine;
    private PlayerMovement _playerMovement;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    void Start()
    {
        // If the held visual has a Rigidbody (e.g. shared with a physics prefab),
        // neutralise it so gravity can't fight the pose lerp.
        _playerMovement = GetComponent<PlayerMovement>();

        if (shovelVisual != null)
        {
            var rb = shovelVisual.GetComponent<Rigidbody>();
            if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnEquipChanged += OnEquipChanged;

            if (!InventoryManager.Instance.HasItem(shovelItemName))
            {
                var item = new Item(shovelItemName, shovelIcon, 1) { isEquippable = true };
                InventoryManager.Instance.AddItem(item);
            }

            _isEquipped = InventoryManager.Instance.IsEquipped(shovelItemName);
        }
        else
        {
            _isEquipped = true;
        }

        RefreshVisual();
    }

    void OnDestroy()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnEquipChanged -= OnEquipChanged;
    }

    // ── Equip ──────────────────────────────────────────────────────────────

    void OnEquipChanged(string equipped)
    {
        _isEquipped = equipped == shovelItemName;
        if (!_isEquipped && Mode == ShovelMode.InFlight)
            RecallShovel();
        RefreshVisual();
    }

    void RefreshVisual()
    {
        if (shovelVisual != null)
            shovelVisual.gameObject.SetActive(_isEquipped && Mode != ShovelMode.InFlight);
    }

    // ── Update ─────────────────────────────────────────────────────────────

    void Update()
    {
        if (!_isEquipped || PauseMenu.GameisPaused) return;

        HandleInput();
        UpdatePose();
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            if (Mode == ShovelMode.InFlight)
                RecallShovel();
            else
                Mode = Mode == ShovelMode.Dig ? ShovelMode.Throw : ShovelMode.Dig;
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (Mode == ShovelMode.Dig)
                TryDig();
            else if (Mode == ShovelMode.Throw)
                LaunchShovel();
        }
    }

    // ── Dig ────────────────────────────────────────────────────────────────

    void TryDig()
    {
        if (_digCoroutine != null) return;

        var ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, digReach, diggableLayer))
        {
            var spot = hit.collider.GetComponent<DiggableSpot>();
            if (spot != null && !spot.HasBeenDug)
                _digCoroutine = StartCoroutine(SwingDig(spot));
        }
    }

    IEnumerator SwingDig(DiggableSpot spot)
    {
        if (shovelVisual == null) { spot.Dig(); _digCoroutine = null; yield break; }

        float half = digSwingDuration * 0.5f;
        Vector3 swingTarget = digLocalPos + new Vector3(0f, -0.18f, 0.12f);

        for (float t = 0f; t < half; t += Time.deltaTime)
        {
            shovelVisual.localPosition = Vector3.Lerp(digLocalPos, swingTarget, t / half);
            yield return null;
        }

        spot.Dig();

        for (float t = 0f; t < half; t += Time.deltaTime)
        {
            shovelVisual.localPosition = Vector3.Lerp(swingTarget, digLocalPos, t / half);
            yield return null;
        }

        shovelVisual.localPosition = digLocalPos;
        _digCoroutine = null;
    }

    // ── Throw ──────────────────────────────────────────────────────────────

    void LaunchShovel()
    {
        if (thrownShovelPrefab == null) return;

        Vector3 origin = playerCamera.transform.position + playerCamera.transform.forward * 0.6f;
        var go = Instantiate(thrownShovelPrefab, origin, playerCamera.transform.rotation);
        _activeShovel = go.GetComponent<ThrownShovel>();
        _activeShovel.Launch(this, playerCamera.transform.forward * throwForce, penetrableLayer);

        Mode = ShovelMode.InFlight;
        RefreshVisual();
    }

    // ── Recall ─────────────────────────────────────────────────────────────

    void RecallShovel()
    {
        if (_activeShovel != null)
            _activeShovel.Recall(OnShovelBack);
        else
            OnShovelBack(Vector3.zero);
    }

    void OnShovelBack(Vector3 boostDir)
    {
        _activeShovel = null;
        Mode = ShovelMode.Dig;
        RefreshVisual();

        if (boostDir.sqrMagnitude > 0.01f)
            _playerMovement?.ApplyExternalVelocity(boostDir * catchBoostForce);
    }

    // ── Pose Lerp ──────────────────────────────────────────────────────────

    void UpdatePose()
    {
        if (shovelVisual == null || Mode == ShovelMode.InFlight || _digCoroutine != null) return;

        var targetPos   = Mode == ShovelMode.Throw ? throwLocalPos   : digLocalPos;
        var targetEuler = Mode == ShovelMode.Throw ? throwLocalEuler : digLocalEuler;

        shovelVisual.localPosition = Vector3.Lerp(
            shovelVisual.localPosition, targetPos, Time.deltaTime * poseLerpSpeed);
        shovelVisual.localRotation = Quaternion.Slerp(
            shovelVisual.localRotation, Quaternion.Euler(targetEuler), Time.deltaTime * poseLerpSpeed);
    }
}
