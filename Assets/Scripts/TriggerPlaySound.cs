using System;
using System.Runtime.InteropServices;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

/// <summary>
/// Attach to your trigger box. Starts the FMOD song and fires OnBeat
/// so any listener (e.g. trees) can react without owning the audio.
/// Also fires OnBirdMarker when a named timeline marker is hit.
/// </summary>
public class TriggerPlaySound : MonoBehaviour
{
    [Header("FMOD Event to play")]
    public EventReference fmodEvent;

    [Header("Marker Settings")]
    [Tooltip("Name of the FMOD timeline marker that triggers bird chirps. Leave empty to disable.")]
    public string birdMarkerName = "";

    // Trees subscribe to this
    public static event Action OnBeat;

    // Birds subscribe to this — fires when the named marker is hit
    public static event Action OnBirdMarker;

    private EventInstance _eventInstance;
    private bool _hasPlayed = false;
    private GCHandle _gcHandle;

    private static EVENT_CALLBACK _callbackDelegate;

    void Awake()
    {
        _callbackDelegate = new EVENT_CALLBACK(BeatCallback);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!_hasPlayed && other.CompareTag("Player"))
        {
            _hasPlayed = true;
            StartSong(other.transform.position);
        }
    }

    void OnDestroy()
    {
        if (_eventInstance.isValid())
        {
            _eventInstance.setCallback(null);
            _eventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            _eventInstance.release();
        }
        if (_gcHandle.IsAllocated)
            _gcHandle.Free();
    }

    private void StartSong(Vector3 position)
    {
        _eventInstance = RuntimeManager.CreateInstance(fmodEvent);
        RuntimeManager.AttachInstanceToGameObject(_eventInstance, transform);

        _gcHandle = GCHandle.Alloc(this);
        _eventInstance.setUserData(GCHandle.ToIntPtr(_gcHandle));

        // Listen for beats always; also listen for markers if a name is set
        var callbackMask = EVENT_CALLBACK_TYPE.TIMELINE_BEAT;
        if (!string.IsNullOrEmpty(birdMarkerName))
            callbackMask |= EVENT_CALLBACK_TYPE.TIMELINE_MARKER;

        _eventInstance.setCallback(_callbackDelegate, callbackMask);
        _eventInstance.start();
        Debug.Log("Started FMOD event: " + fmodEvent);
    }

    // ── Static FMOD callback (audio thread — only set flags!) ──────────────────
    [AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
    private static FMOD.RESULT BeatCallback(EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr paramPtr)
    {
        EventInstance instance = new EventInstance(instancePtr);
        instance.getUserData(out IntPtr userData);
        if (userData == IntPtr.Zero) return FMOD.RESULT.OK;

        var handle = GCHandle.FromIntPtr(userData);
        if (!(handle.Target is TriggerPlaySound trigger)) return FMOD.RESULT.OK;

        if (type == EVENT_CALLBACK_TYPE.TIMELINE_BEAT)
        {
            trigger._pendingBeat = true;
        }
        else if (type == EVENT_CALLBACK_TYPE.TIMELINE_MARKER)
        {
            // Read the marker name from the parameter struct
            var props = (TIMELINE_MARKER_PROPERTIES)Marshal.PtrToStructure(
                paramPtr, typeof(TIMELINE_MARKER_PROPERTIES));

            if (props.name == trigger.birdMarkerName)
                trigger._pendingBirdMarker = true;
        }

        return FMOD.RESULT.OK;
    }

    private volatile bool _pendingBeat = false;
    private volatile bool _pendingBirdMarker = false;

    void Update()
    {
        if (_pendingBeat)
        {
            _pendingBeat = false;
            OnBeat?.Invoke();
        }

        if (_pendingBirdMarker)
        {
            _pendingBirdMarker = false;
            OnBirdMarker?.Invoke();
        }
    }
}