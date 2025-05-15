using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{

    public static MusicManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource softSource;
    [SerializeField] private AudioSource mediumSource;
    [SerializeField] private AudioSource hardSource;

    [Header("Settings")]
    [SerializeField] private float defaultTransitionTime = 1.5f;
    [Range(0, 1)]
    [SerializeField] private float maxVolume = 1f;

    [Header("Songs")]
    [SerializeField] private MusicTrackSO song01;
    [SerializeField] private MusicTrackSO song02;

    // Intensity tracking
    private enum MusicIntensity { Soft, Medium, Hard }
    private MusicIntensity currentIntensity = MusicIntensity.Soft;
    private MusicTrackSO currentSong;

    // Transition coroutine reference
    private Coroutine fadeCoroutine;

    private void Awake()
    {

        // Implement singleton pattern for cross-scene persistence
        SingletonAwake();

        // Ensure we have all required audio sources
        if (softSource == null || mediumSource == null || hardSource == null)
        {
            Debug.LogError("MusicManager requires three AudioSource components!");
            enabled = false;
            return;
        }

        // Initialize all sources with volume 0
        softSource.volume = 0;
        mediumSource.volume = 0;
        hardSource.volume = 0;

        // Configure audio sources for looping
        softSource.loop = true;
        mediumSource.loop = true;
        hardSource.loop = true;
    }

    private void SingletonAwake()
    {
        // Implement singleton pattern for cross-scene persistence
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadTestTransitionScene()
    {
        // Load the test scene for transitions
        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("TestTransitionScene");
    }

    public void PlaySong01()
    {
        PlaySong(song01);
    }

    public void PlaySong02()
    {
        PlaySong(song02);
    }


    /// <summary>
    /// Plays a song at the default (soft) intensity level.
    /// </summary>
    /// <param name="song">The song data to play</param>
    public void PlaySong(MusicTrackSO song)
    {
        if (song == null)
        {
            Debug.LogWarning("Attempted to play a null MusicTrackSO!");
            return;
        }

        // Stop any active music
        StopAllMusic();

        // Store the current song
        currentSong = song;

        // Set clips for each intensity
        softSource.clip = song.softIntensityClip;
        mediumSource.clip = song.mediumIntensityClip;
        hardSource.clip = song.hardIntensityClip;

        // Start playing all sources at the same time for seamless transitions
        softSource.Play();
        mediumSource.Play();
        hardSource.Play();

        // Set initial volumes - default to soft intensity
        softSource.volume = maxVolume;
        mediumSource.volume = 0f;
        hardSource.volume = 0f;

        currentIntensity = MusicIntensity.Soft;
    }

    /// <summary>
    /// Stops all music playback.
    /// </summary>
    public void StopAllMusic()
    {
        // Stop any fade transition in progress
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        // Stop all audio sources
        softSource.Stop();
        mediumSource.Stop();
        hardSource.Stop();

        currentSong = null;
    }

    /// <summary>
    /// Set the music intensity to soft with optional transition time.
    /// </summary>
    /// <param name="transitionTime">Time in seconds for the transition</param>
    public void SetSoftIntensity(float transitionTime = -1f)
    {
        if (currentSong == null) return;

        if (transitionTime < 0)
            transitionTime = defaultTransitionTime;

        TransitionToIntensity(MusicIntensity.Soft, transitionTime);
    }

    /// <summary>
    /// Set the music intensity to medium with optional transition time.
    /// </summary>
    /// <param name="transitionTime">Time in seconds for the transition</param>
    public void SetMediumIntensity(float transitionTime = -1f)
    {
        if (currentSong == null) return;

        if (transitionTime < 0)
            transitionTime = defaultTransitionTime;

        TransitionToIntensity(MusicIntensity.Medium, transitionTime);
    }

    /// <summary>
    /// Set the music intensity to hard with optional transition time.
    /// </summary>
    /// <param name="transitionTime">Time in seconds for the transition</param>
    public void SetHardIntensity(float transitionTime = -1f)
    {
        if (currentSong == null) return;

        if (transitionTime < 0)
            transitionTime = defaultTransitionTime;

        TransitionToIntensity(MusicIntensity.Hard, transitionTime);
    }

    /// <summary>
    /// Handles the transition between intensity levels.
    /// </summary>
    private void TransitionToIntensity(MusicIntensity targetIntensity, float transitionTime)
    {
        // Skip if we're already at the target intensity
        if (currentIntensity == targetIntensity) return;

        // Stop any existing transition
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // Start the new transition
        fadeCoroutine = StartCoroutine(FadeIntensity(targetIntensity, transitionTime));
    }

    /// <summary>
    /// Coroutine to handle fading between intensity levels.
    /// </summary>
    private IEnumerator FadeIntensity(MusicIntensity targetIntensity, float transitionTime)
    {
        // Determine which source to fade in and which to fade out
        AudioSource sourceToFadeIn = GetSourceForIntensity(targetIntensity);
        AudioSource sourceToFadeOut = GetSourceForIntensity(currentIntensity);

        // Store initial volumes
        float startVolumeIn = sourceToFadeIn.volume;
        float startVolumeOut = sourceToFadeOut.volume;

        // Fade over the specified duration
        float elapsedTime = 0f;

        while (elapsedTime < transitionTime)
        {
            elapsedTime += Time.deltaTime;
            float fadeProgress = elapsedTime / transitionTime;

            // Use smoothstep for a more natural transition (maps the value to a curve that starts and ends slowly)
            float smoothT = fadeProgress * fadeProgress * (3f - 2f * fadeProgress);

            // Update volumes
            sourceToFadeIn.volume = Mathf.Lerp(startVolumeIn, maxVolume, smoothT);
            sourceToFadeOut.volume = Mathf.Lerp(startVolumeOut, 0f, smoothT);

            yield return null;
        }

        // Ensure final volumes are exact
        sourceToFadeIn.volume = maxVolume;
        sourceToFadeOut.volume = 0f;

        // Update current intensity
        currentIntensity = targetIntensity;
        fadeCoroutine = null;
    }

    /// <summary>
    /// Returns the AudioSource corresponding to the specified intensity.
    /// </summary>
    private AudioSource GetSourceForIntensity(MusicIntensity intensity)
    {
        switch (intensity)
        {
            case MusicIntensity.Soft:
                return softSource;
            case MusicIntensity.Medium:
                return mediumSource;
            case MusicIntensity.Hard:
                return hardSource;
            default:
                return softSource;
        }
    }
}
