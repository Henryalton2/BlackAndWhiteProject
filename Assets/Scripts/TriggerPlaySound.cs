using System;
using System.Runtime.InteropServices;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

/// <summary>
/// Attach to your trigger box. Starts the FMOD song and broadcasts
/// beat and named marker events to any listeners in the scene.
/// </summary>
public class TriggerPlaySound : MonoBehaviour
{
    [Header("FMOD Event to play")]
    public EventReference fmodEvent;

    [Header("Marker Settings")]
    [Tooltip("At least one marker name must be set for marker callbacks to fire.")]
    public string birdMarkerName = "";

    // Beats
    public static event Action OnBeat;

    // All markers — passes the marker name so any listener can filter
    public static event Action<string> OnMarker;

    // Kept for backwards compatibility with BirdMarkerSync
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
        _eventInstance.setCallback(_callbackDelegate,
            EVENT_CALLBACK_TYPE.TIMELINE_BEAT | EVENT_CALLBACK_TYPE.TIMELINE_MARKER);

        _eventInstance.start();
        Debug.Log("Started FMOD event: " + fmodEvent);
    }

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
            var props = (TIMELINE_MARKER_PROPERTIES)Marshal.PtrToStructure(
                paramPtr, typeof(TIMELINE_MARKER_PROPERTIES));
            trigger._pendingMarkerName = props.name;
        }

        return FMOD.RESULT.OK;
    }

    private volatile bool _pendingBeat = false;
    private volatile string _pendingMarkerName = null;

    void Update()
    {
        if (_pendingBeat)
        {
            _pendingBeat = false;
            OnBeat?.Invoke();
        }

        if (_pendingMarkerName != null)
        {
            string markerName = _pendingMarkerName;
            _pendingMarkerName = null;

            OnMarker?.Invoke(markerName);

            // Backwards compat for BirdMarkerSync
            if (markerName == birdMarkerName)
                OnBirdMarker?.Invoke();
        }
    }
}