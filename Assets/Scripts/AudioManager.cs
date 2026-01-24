using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] EventReference FootstepEvent;
    [SerializeField] float rate;
    [SerializeField] GameObject player;
    [SerializeField] PlayerMovement controller;

    float time;
    public void PlayFootstep()
    {
        RuntimeManager.PlayOneShotAttached(FootstepEvent, player);
    }
    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;
        if(controller.isWalking)
        {
            if(time>=rate)
            {
                PlayFootstep();
                time = 0;
            }
        }
    }
}
