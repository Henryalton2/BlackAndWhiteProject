using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class ForestMusicManager : MonoBehaviour
{
    [Header("FMOD Events")]
    public EventReference Violin1Event;
    public EventReference Violin2Event;
    public EventReference CelesteEvent;
    public EventReference BassEvent;

    [HideInInspector] public EventInstance Violin1Master;
    [HideInInspector] public EventInstance Violin2Master;
    [HideInInspector] public EventInstance CelesteMaster;
    [HideInInspector] public EventInstance BassMaster;

    void Start()
    {
        // Create and start master instances (can be 3D or 2D)
        Violin1Master = RuntimeManager.CreateInstance(Violin1Event);
        Violin2Master = RuntimeManager.CreateInstance(Violin2Event);
        CelesteMaster = RuntimeManager.CreateInstance(CelesteEvent);
        BassMaster = RuntimeManager.CreateInstance(BassEvent);

        Violin1Master.start();
        Violin2Master.start();
        CelesteMaster.start();
        BassMaster.start();
    }

    /// <summary>
    /// Returns the master EventInstance for a given instrument.
    /// </summary>
    public EventInstance GetMasterInstance(MusicReactivePlant.Instrument instrument)
    {
        switch (instrument)
        {
            case MusicReactivePlant.Instrument.Violin1: return Violin1Master;
            case MusicReactivePlant.Instrument.Violin2: return Violin2Master;
            case MusicReactivePlant.Instrument.Celeste: return CelesteMaster;
            case MusicReactivePlant.Instrument.Bass: return BassMaster;
        }
        return Violin1Master;
    }

    private void OnDestroy()
    {
        // Stop and release all master instances
        Violin1Master.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        Violin2Master.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        CelesteMaster.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        BassMaster.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);

        Violin1Master.release();
        Violin2Master.release();
        CelesteMaster.release();
        BassMaster.release();
    }
}
