using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// <summary>
/// This class is used to manage the main sound source of the game that
/// plays the soundtrack and handles the different channels volume
/// </summary>
public class SoundManager : MonoBehaviour
{
    public AudioMixerGroup masterMixerGroup;
    public AudioMixerGroup soundtrackMixerGroup;
    public AudioMixerGroup sfxMixerGroup;

    /// <summary>
    /// Make sure that this gameObject is not destroyed between different scenes being loaded
    /// </summary>
    void Start()
    {
        DontDestroyOnLoad(this);
    }

    public void UpdateMasterSoundLevel(Slider slider)
    {
        masterMixerGroup.audioMixer.SetFloat("MasterVolume", Mathf.Log(slider.value / 100f) * 20);
    }

    public void UpdateSoundtrackSoundLevel(Slider slider)
    {
        soundtrackMixerGroup.audioMixer.SetFloat("SoundtrackVolume", Mathf.Log(slider.value / 100f) * 20);
    }

    public void UpdateSfxSoundLevel(Slider slider)
    {
        sfxMixerGroup.audioMixer.SetFloat("SFXVolume", Mathf.Log(slider.value / 100f) * 20);
    }

}
