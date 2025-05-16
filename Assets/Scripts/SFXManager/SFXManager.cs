using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [Header("Audio Source Pool")]
    [SerializeField] private int audioSourcePoolSize = 10;
    [SerializeField] private Transform audioSourceParent;
    [SerializeField] private float defaultVolume = 1.0f;

    [Header("Success Sounds")]
    [SerializeField] private AudioClip[] correctArrestSounds;
    [SerializeField] private float correctArrestVolume = 1.0f;

    [Header("Warning Sounds")]
    [SerializeField] private AudioClip[] wrongArrestSounds;
    [SerializeField] private float wrongArrestVolume = 0.8f;

    [Header("Drone Sounds")]
    [SerializeField] private AudioClip[] droneAlertSounds;
    [SerializeField] private AudioClip[] droneConfirmSounds;
    [SerializeField] private AudioClip[] droneDenySounds;
    [SerializeField] private AudioClip droneTargetPlayerSound;
    [SerializeField] private float droneVolume = 0.7f;

    [Header("UI Sounds")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip menuOpenSound;
    [SerializeField] private AudioClip menuCloseSound;
    [SerializeField] private float uiVolume = 0.5f;

    [Header("Round Sounds")]
    [SerializeField] private AudioClip roundStartSound;
    [SerializeField] private AudioClip roundEndSound;
    [SerializeField] private AudioClip gameOverSound;
    [SerializeField] private AudioClip employeeOfMonthSound;
    [SerializeField] private float roundVolume = 0.9f;

    // Pool of audio sources
    private List<AudioSource> audioSourcePool = new List<AudioSource>();
    private int nextSourceIndex = 0;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Create audio source parent if needed
        if (audioSourceParent == null)
        {
            audioSourceParent = transform;
        }

        // Initialize audio source pool
        InitializeAudioSourcePool();
    }

    private void InitializeAudioSourcePool()
    {
        for (int i = 0; i < audioSourcePoolSize; i++)
        {
            GameObject audioSourceObj = new GameObject($"AudioSource_{i}");
            audioSourceObj.transform.SetParent(audioSourceParent);

            AudioSource source = audioSourceObj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = defaultVolume;

            audioSourcePool.Add(source);
        }
    }

    private AudioSource GetNextAudioSource()
    {
        // Get next available audio source from pool using round-robin approach
        AudioSource source = audioSourcePool[nextSourceIndex];

        // Move to next index for next time
        nextSourceIndex = (nextSourceIndex + 1) % audioSourcePool.Count;

        return source;
    }

    private void PlaySound(AudioClip clip, float volume = 1.0f, float pitch = 1.0f)
    {
        if (clip == null) return;

        AudioSource source = GetNextAudioSource();
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.Play();
    }

    private AudioClip GetRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }

    // Public methods for each sound category

    #region Arrest Sounds

    public void PlayCorrectArrestSound()
    {
        PlaySound(GetRandomClip(correctArrestSounds), correctArrestVolume);
    }

    public void PlayWrongArrestSound()
    {
        PlaySound(GetRandomClip(wrongArrestSounds), wrongArrestVolume);
    }

    #endregion

    #region Drone Sounds

    public void PlayDroneAlertSound()
    {
        PlaySound(GetRandomClip(droneAlertSounds), droneVolume);
    }

    public void PlayDroneConfirmSound()
    {
        PlaySound(GetRandomClip(droneConfirmSounds), droneVolume);
    }

    public void PlayDroneDenySound()
    {
        PlaySound(GetRandomClip(droneDenySounds), droneVolume);
    }

    public void PlayDroneTargetPlayerSound()
    {
        PlaySound(droneTargetPlayerSound, droneVolume * 1.2f);
    }

    #endregion

    #region UI Sounds

    public void PlayButtonClickSound()
    {
        PlaySound(buttonClickSound, uiVolume);
    }

    public void PlayMenuOpenSound()
    {
        PlaySound(menuOpenSound, uiVolume);
    }

    public void PlayMenuCloseSound()
    {
        PlaySound(menuCloseSound, uiVolume);
    }

    #endregion

    #region Round Sounds

    public void PlayRoundStartSound()
    {
        PlaySound(roundStartSound, roundVolume);
    }

    public void PlayRoundEndSound()
    {
        PlaySound(roundEndSound, roundVolume);
    }

    public void PlayGameOverSound()
    {
        PlaySound(gameOverSound, roundVolume * 1.2f);
    }

    public void PlayEmployeeOfMonthSound()
    {
        PlaySound(employeeOfMonthSound, roundVolume);
    }

    #endregion

    #region Advanced Sound Control

    // Play sound with custom parameters
    public void PlaySoundWithParameters(AudioClip clip, float volume = 1.0f, float pitch = 1.0f, bool loop = false)
    {
        if (clip == null) return;

        AudioSource source = GetNextAudioSource();
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.loop = loop;
        source.Play();

        // If looping, we need to keep track to stop it later
        if (loop)
        {
            StartCoroutine(TrackLoopingSound(source));
        }
    }

    // Play sound at specific position (3D sound)
    public void PlaySoundAtPosition(AudioClip clip, Vector3 position, float volume = 1.0f, float maxDistance = 20f)
    {
        if (clip == null) return;

        AudioSource source = GetNextAudioSource();
        source.clip = clip;
        source.volume = volume;
        source.transform.position = position;

        // Set up as 3D sound
        source.spatialBlend = 1.0f; // 1.0 = fully 3D
        source.minDistance = 1.0f;
        source.maxDistance = maxDistance;

        source.Play();
    }

    private System.Collections.IEnumerator TrackLoopingSound(AudioSource source)
    {
        // This just adds the looping source to a list if needed for management
        // Not implementing full tracking now for simplicity
        yield return null;
    }

    public void StopAllSounds()
    {
        foreach (AudioSource source in audioSourcePool)
        {
            source.Stop();
        }
    }

    public void SetMasterVolume(float volume)
    {
        foreach (AudioSource source in audioSourcePool)
        {
            source.volume = volume;
        }
        defaultVolume = volume;
    }

    #endregion
}