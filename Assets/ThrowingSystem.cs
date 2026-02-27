using UnityEngine;
using System.Collections.Generic;

public class ThrowingSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform throwOrigin;

    [Header("Throw Settings")]
    [SerializeField] private KeyCode cycleKey = KeyCode.Q;
    [SerializeField] private KeyCode throwKey = KeyCode.Mouse0;
    [SerializeField] private float aimHoldTime = 0.15f;

    [Header("Arc Preview")]
    [SerializeField] private int arcSegments = 30;
    [SerializeField] private float arcTimeStep = 0.05f;
    [SerializeField] private Color arcColor = new Color(1f, 0.8f, 0f, 0.8f);

    private List<Item> _throwables = new List<Item>();
    private int _selectedIndex = -1;
    private bool _isAiming = false;
    private float _aimTimer = 0f;
    private LineRenderer _arcLine;

    public Item SelectedThrowable => (_selectedIndex >= 0 && _selectedIndex < _throwables.Count)
                                     ? _throwables[_selectedIndex] : null;
    public bool IsAiming => _isAiming;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (throwOrigin == null)
        {
            GameObject origin = new GameObject("ThrowOrigin");
            origin.transform.SetParent(playerCamera.transform, false);
            origin.transform.localPosition = new Vector3(0.3f, -0.2f, 0.5f);
            throwOrigin = origin.transform;
        }

        SetupArcRenderer();
    }

    private void Start()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged += RefreshThrowableList;

        RefreshThrowableList();
    }

    private void Update()
    {
        if (Input.GetKeyDown(cycleKey))
            CycleThrowable();

        if (SelectedThrowable != null)
        {
            if (Input.GetKey(throwKey))
            {
                _aimTimer += Time.deltaTime;
                if (_aimTimer >= aimHoldTime)
                {
                    _isAiming = true;
                    DrawArcPreview();
                }
            }

            if (Input.GetKeyUp(throwKey))
            {
                HideArc();
                if (_aimTimer > 0f)
                    ExecuteThrow();

                _aimTimer = 0f;
                _isAiming = false;
            }
        }
        else
        {
            HideArc();
            _aimTimer = 0f;
            _isAiming = false;
        }
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= RefreshThrowableList;
    }

    public bool HasThrowables() => _throwables.Count > 0;

    public bool SelectThrowable(string itemName)
    {
        int idx = _throwables.FindIndex(i => i.itemName == itemName);
        if (idx < 0) return false;
        _selectedIndex = idx;
        LogSelection();
        return true;
    }

    private void RefreshThrowableList()
    {
        if (InventoryManager.Instance == null) return;

        string previousName = SelectedThrowable?.itemName;
        _throwables.Clear();

        foreach (Item item in InventoryManager.Instance.GetAllItems())
            if (item.isThrowable && item.quantity > 0)
                _throwables.Add(item);

        if (previousName != null)
            _selectedIndex = _throwables.FindIndex(i => i.itemName == previousName);

        if (_selectedIndex < 0)
            _selectedIndex = _throwables.Count > 0 ? 0 : -1;
    }

    private void CycleThrowable()
    {
        if (_throwables.Count == 0)
        {
            _selectedIndex = -1;
            Debug.Log("[Throw] No throwable items in inventory.");
            return;
        }

        _selectedIndex = (_selectedIndex + 1) % _throwables.Count;
        LogSelection();
    }

    private void ExecuteThrow()
    {
        Item item = SelectedThrowable;
        if (item == null) return;

        Vector3 aimDir = GetAimDirection();
        Vector3 launchVelocity = aimDir * item.throwForce;

        GameObject projectileGO;

        if (item.throwPrefab != null)
        {
            projectileGO = Instantiate(item.throwPrefab,
                                       throwOrigin.position,
                                       Quaternion.LookRotation(aimDir));
        }
        else
        {
            projectileGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileGO.transform.position = throwOrigin.position;
            projectileGO.transform.localScale = Vector3.one * 0.15f;
            projectileGO.transform.rotation = Quaternion.LookRotation(aimDir);

            Renderer r = projectileGO.GetComponent<Renderer>();
            if (r) r.material.color = new Color(0.6f, 0.6f, 0.6f);
        }

        ThrowableProjectile proj = projectileGO.GetComponent<ThrowableProjectile>();
        if (proj == null)
            proj = projectileGO.AddComponent<ThrowableProjectile>();

        proj.damage = item.throwDamage;
        proj.itemName = item.itemName;

        Rigidbody rb = projectileGO.GetComponent<Rigidbody>();
        if (rb == null)
            rb = projectileGO.AddComponent<Rigidbody>();

        rb.velocity = launchVelocity;

        Collider[] playerColliders = GetComponentsInChildren<Collider>();
        Collider projCollider = projectileGO.GetComponent<Collider>();
        if (projCollider != null)
            foreach (Collider pc in playerColliders)
                Physics.IgnoreCollision(projCollider, pc, true);

        if (item.consumeOnThrow)
            InventoryManager.Instance.RemoveItem(item.itemName, 1);

        Debug.Log($"[Throw] Threw {item.itemName}  force={item.throwForce}  dir={aimDir}");
    }

    private Vector3 GetAimDirection()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f,
                                                            Screen.height * 0.5f, 0));
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, 200f))
            targetPoint = hit.point;
        else
            targetPoint = ray.origin + ray.direction * 200f;

        return (targetPoint - throwOrigin.position).normalized;
    }

    private void SetupArcRenderer()
    {
        GameObject go = new GameObject("ThrowArcPreview");
        go.transform.SetParent(transform, false);

        _arcLine = go.AddComponent<LineRenderer>();
        _arcLine.positionCount = arcSegments;
        _arcLine.startWidth = 0.03f;
        _arcLine.endWidth = 0.01f;
        _arcLine.useWorldSpace = true;
        _arcLine.material = new Material(Shader.Find("Sprites/Default"));
        _arcLine.startColor = arcColor;
        _arcLine.endColor = new Color(arcColor.r, arcColor.g, arcColor.b, 0.1f);
        _arcLine.enabled = false;
    }

    private void DrawArcPreview()
    {
        if (SelectedThrowable == null) { HideArc(); return; }

        _arcLine.enabled = true;

        Vector3 velocity = GetAimDirection() * SelectedThrowable.throwForce;
        Vector3 pos = throwOrigin.position;
        float dt = arcTimeStep;

        for (int i = 0; i < arcSegments; i++)
        {
            _arcLine.SetPosition(i, pos);
            pos += velocity * dt;
            velocity += Physics.gravity * dt;
        }
    }

    private void HideArc()
    {
        if (_arcLine != null)
            _arcLine.enabled = false;
    }

    private void LogSelection()
    {
        if (SelectedThrowable != null)
            Debug.Log($"[Throw] Selected: {SelectedThrowable.itemName}  " +
                      $"(qty={SelectedThrowable.quantity}, force={SelectedThrowable.throwForce})");
    }
}