using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Runtime.InteropServices;
using System;

/// <summary>
/// Attach to any GameObject with a SpriteRenderer.
/// Swaps to altSprite when the "swap" marker is hit, returns to
/// defaultSprite when the "swap back" marker is hit.
/// Listens to TriggerPlaySound's event instance via static events.
/// </summary>
public class MarkerSpriteSwapper : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite defaultSprite;
    public Sprite altSprite;

    [Header("Marker Names (must match FMOD Studio exactly)")]
    public string swapMarkerName = "";
    public string swapBackMarkerName = "";

    private SpriteRenderer _sr;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (defaultSprite == null && _sr != null)
            defaultSprite = _sr.sprite; // use whatever is already assigned as default
    }

    void OnEnable() => TriggerPlaySound.OnMarker += HandleMarker;
    void OnDisable() => TriggerPlaySound.OnMarker -= HandleMarker;

    private void HandleMarker(string markerName)
    {
        if (markerName == swapMarkerName)
            _sr.sprite = altSprite;
        else if (markerName == swapBackMarkerName)
            _sr.sprite = defaultSprite;
    }
}