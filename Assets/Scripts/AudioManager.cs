using System.Collections;
using FMODUnity;
using FMOD.Studio;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Footsteps")]
    [SerializeField] private EventReference FootstepEvent;
    [SerializeField] private float rate = 0.5f;
    [SerializeField] private GameObject player;
    private PlayerMovement controller;

    [Header("Music for Tree Visuals")]
    [SerializeField] private EventReference MusicEvent;
    private EventInstance musicInstance;

    [Header("Atmosphere / Ambient Sounds")]
    [SerializeField] private EventReference AtmosphereEvent;
    private EventInstance atmosphereInstance;

    [Range(0f, 1f)]
    public float atmosphereVolume = 1f; // Slider for Atmosphere

    [Range(0f, 1f)]
    public float musicLevel = 0f; // Trees react to this

    private float time;

    void Start()
    {
        // --- PlayerMovement assignment ---
        if (player != null)
        {
            controller = player.GetComponent<PlayerMovement>();
            if (controller == null)
                Debug.LogWarning("PlayerMovement not found on player!");
        }

        // --- Start music instance ---
        if (MusicEvent.Guid != System.Guid.Empty)
        {
            musicInstance = RuntimeManager.CreateInstance(MusicEvent);
            musicInstance.start();
        }

        // --- Start atmosphere instance ---
        if (AtmosphereEvent.Guid != System.Guid.Empty)
        {
            atmosphereInstance = RuntimeManager.CreateInstance(AtmosphereEvent);

            // Attach to player if it's a 3D event
            if (player != null)
                RuntimeManager.AttachInstanceToGameObject(atmosphereInstance, player.transform, player.GetComponent<Rigidbody>());

            atmosphereInstance.start();

            // Set initial volume
            SetAtmosphereVolume(atmosphereVolume);
        }
    }

    void Update()
    {
        // --- Footsteps ---
        time += Time.deltaTime;
        if (controller != null && controller.isWalking)
        {
            if (time >= rate)
            {
                PlayFootstep();
                time = 0f;
            }
        }

        // --- Update musicLevel for trees (you can hook this to a parameter) ---
        UpdateMusicLevel();

        // --- Update atmosphere volume if slider changed ---
        if (atmosphereInstance.isValid())
        {
            SetAtmosphereVolume(atmosphereVolume);
        }
    }

    public void PlayFootstep()
    {
        if (player != null)
            RuntimeManager.PlayOneShotAttached(FootstepEvent, player);
    }

    private void UpdateMusicLevel()
    {
        // Example placeholder: set musicLevel manually or via FMOD parameter
        musicLevel = 1f;
    }

    public void SetAtmosphereVolume(float volume)
    {
        if (atmosphereInstance.isValid())
        {
            atmosphereInstance.setVolume(Mathf.Clamp01(volume));
        }
    }

    private void OnDestroy()
    {
        if (musicInstance.isValid())
        {
            musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            musicInstance.release();
        }

        if (atmosphereInstance.isValid())
        {
            atmosphereInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            atmosphereInstance.release();
        }
    }
}
