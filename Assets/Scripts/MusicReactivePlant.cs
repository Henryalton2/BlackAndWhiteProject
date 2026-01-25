using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections;
using System.Collections.Generic;

public class MusicReactivePlant : MonoBehaviour
{
    public enum Instrument { Violin1, Violin2, Celeste, Bass }
    public Instrument instrument;

    [Header("Visual Settings")]
    public float minScale = 1f;
    public float maxScale = 2f;
    public float smoothSpeed = 3f;

    [Header("Proximity Settings")]
    public Transform player;
    public float maxDistance = 15f;

    [Header("Audio Settings")]
    public int maxSimultaneousTrees = 5;
    public float syncInterval = 0.2f;
    public float pitchVariation = 0.01f;

    private ForestMusicManager musicManager;
    private EventInstance treeInstance;
    private Vector3 targetScale;

    private static Dictionary<Instrument, List<MusicReactivePlant>> activeTrees = new Dictionary<Instrument, List<MusicReactivePlant>>();
    private float distanceToPlayer = float.MaxValue;
    private float syncTimer = 0f;

    void Start()
    {
        musicManager = FindObjectOfType<ForestMusicManager>();
        if (musicManager == null)
        {
            Debug.LogError("ForestMusicManager not found in scene!");
            return;
        }

        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

        // Register this tree
        if (!activeTrees.ContainsKey(instrument))
            activeTrees[instrument] = new List<MusicReactivePlant>();
        activeTrees[instrument].Add(this);

        // Initialize tree audio after FMOD banks are loaded
        StartCoroutine(InitTreeAudio());
    }

    private IEnumerator InitTreeAudio()
    {
        // Wait until banks are loaded
        while (!RuntimeManager.HasBankLoaded("Master"))
            yield return null;
        while (!RuntimeManager.HasBankLoaded("Music"))
            yield return null;

        // Get EventReference for this instrument
        EventReference eventRef = instrument switch
        {
            Instrument.Violin1 => musicManager.Violin1Event,
            Instrument.Violin2 => musicManager.Violin2Event,
            Instrument.Celeste => musicManager.CelesteEvent,
            Instrument.Bass => musicManager.BassEvent,
            _ => musicManager.Violin1Event
        };

        // Create the tree's 3D instance
        treeInstance = RuntimeManager.CreateInstance(eventRef);
        RuntimeManager.AttachInstanceToGameObject(treeInstance, transform, GetComponent<Rigidbody>());

        // Optional small pitch variation
        float pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        treeInstance.setPitch(pitch);

        treeInstance.start();
    }

    void Update()
    {
        if (player == null) return;

        // Update visuals every frame
        UpdateVisuals();

        // Update audio sync at intervals
        syncTimer += Time.deltaTime;
        if (syncTimer >= syncInterval)
        {
            UpdateDistanceToPlayer();
            SyncAudioToMaster();
            syncTimer = 0f;
        }
    }

    private void UpdateVisuals()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= maxDistance)
        {
            float level = 1f; // Replace with FMOD RMS/parameter if available
            float scale = Mathf.Lerp(minScale, maxScale, level);
            targetScale = new Vector3(scale, scale, scale);
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * smoothSpeed);
        }
    }

    private void UpdateDistanceToPlayer()
    {
        if (player != null)
            distanceToPlayer = Vector3.Distance(transform.position, player.position);
        else
            distanceToPlayer = float.MaxValue;
    }

    private void SyncAudioToMaster()
    {
        if (!activeTrees.ContainsKey(instrument)) return;

        EventInstance master = musicManager.GetMasterInstance(instrument);
        if (!master.isValid() || !treeInstance.isValid())
            return;

        // Sync tree timeline with master
        if (master.getTimelinePosition(out int position) == FMOD.RESULT.OK)
        {
            treeInstance.setTimelinePosition(position);
        }

        // Limit number of audible trees
        List<MusicReactivePlant> trees = activeTrees[instrument];
        trees.Sort((a, b) => a.distanceToPlayer.CompareTo(b.distanceToPlayer));

        for (int i = 0; i < trees.Count; i++)
        {
            bool audible = i < maxSimultaneousTrees && trees[i].distanceToPlayer <= maxDistance;
            if (trees[i].treeInstance.isValid())
            {
                trees[i].treeInstance.setPaused(!audible);
            }
        }
    }

    private void OnDestroy()
    {
        if (treeInstance.isValid())
        {
            treeInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            treeInstance.release();
        }

        if (activeTrees.ContainsKey(instrument))
            activeTrees[instrument].Remove(this);
    }
}
