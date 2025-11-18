using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Manages music playback with separate stems (bass, mid, high) for audio-reactive effects.
/// Plays a full mix for the player to hear, while analyzing separate stems for better effect control.
/// </summary>
public class MusicStemManager : MonoBehaviour
{
    [System.Serializable]
    public class MusicTrack
    {
        [Header("Track Info")]
        public string trackName = "Level 1 Music";

        [Header("Audio Clips")]
        [Tooltip("Full mix that the player hears")]
        public AudioClip fullMixClip;

        [Tooltip("Isolated bass frequencies (kick, bass guitar, etc.)")]
        public AudioClip bassStemClip;

        [Tooltip("Isolated mid frequencies (vocals, guitars, etc.)")]
        public AudioClip midStemClip;

        [Tooltip("Isolated high frequencies (cymbals, hi-hats, etc.)")]
        public AudioClip highStemClip;

        [Header("Settings")]
        [Range(0f, 1f)]
        public float volume = 0.8f;

        public bool loop = true;
    }

    [Header("Music Tracks")]
    [Tooltip("List of all music tracks for different levels/scenes")]
    public MusicTrack[] musicTracks;

    [Header("Audio Mixers")]
    [Tooltip("Audio mixer for the music the player hears")]
    public AudioMixerGroup musicMixerGroup;

    [Tooltip("Audio mixer for muted analysis sources (create a separate mixer with -80dB volume)")]
    public AudioMixerGroup mutedAnalysisMixerGroup;

    [Header("Current Track")]
    [Tooltip("Index of the currently playing track (or -1 for none)")]
    public int currentTrackIndex = -1;

    [Header("Auto-Play")]
    [Tooltip("Automatically play a track on Start")]
    public bool autoPlay = false;

    [Tooltip("Track index to auto-play (if autoPlay is enabled)")]
    public int autoPlayTrackIndex = 0;

    // Audio sources
    private AudioSource fullMixSource;
    private AudioSource bassAnalysisSource;
    private AudioSource midAnalysisSource;
    private AudioSource highAnalysisSource;

    // Reference to edge detection controller
    private AudioReactiveEdgeDetection audioReactive;

    // Sync check
    private float syncCheckInterval = 0.1f; // Check every 100ms instead of 500ms
    private float nextSyncCheck = 0f;
    private const float SYNC_THRESHOLD = 0.02f; // 20ms tolerance (much tighter!)

    // Track if we've done initial sync
    private bool hasInitialSync = false;

    private void Awake()
    {
        // Create audio sources
        SetupAudioSources();
    }

    private void Start()
    {
        // Find AudioReactiveEdgeDetection component
        audioReactive = GetComponent<AudioReactiveEdgeDetection>();
        if (audioReactive == null)
        {
            Debug.LogWarning("MusicStemManager: No AudioReactiveEdgeDetection found on this GameObject. Add one if you want audio-reactive effects.");
        }

        // Auto-play if enabled
        if (autoPlay && musicTracks.Length > 0)
        {
            PlayTrack(autoPlayTrackIndex);
        }
    }

    private void Update()
    {
        // Force initial sync on first frame after playback starts
        if (fullMixSource.isPlaying && !hasInitialSync)
        {
            ForceInitialSync();
            hasInitialSync = true;
        }

        // Periodically check sync between sources
        if (Time.time >= nextSyncCheck && fullMixSource.isPlaying)
        {
            CheckSync();
            nextSyncCheck = Time.time + syncCheckInterval;
        }

        // Reset sync flag when music stops
        if (!fullMixSource.isPlaying)
        {
            hasInitialSync = false;
        }
    }

    /// <summary>
    /// Setup all audio sources
    /// </summary>
    private void SetupAudioSources()
    {
        // Full mix source (player hears this)
        fullMixSource = gameObject.AddComponent<AudioSource>();
        fullMixSource.playOnAwake = false;
        fullMixSource.outputAudioMixerGroup = musicMixerGroup;

        // Bass analysis source (routed to muted mixer, for spectrum analysis only)
        GameObject bassObj = new GameObject("Bass Analysis");
        bassObj.transform.SetParent(transform);
        bassAnalysisSource = bassObj.AddComponent<AudioSource>();
        bassAnalysisSource.playOnAwake = false;
        bassAnalysisSource.volume = 1f; // Must be > 0 for GetSpectrumData
        bassAnalysisSource.outputAudioMixerGroup = mutedAnalysisMixerGroup; // Route to muted mixer

        // Mid analysis source
        GameObject midObj = new GameObject("Mid Analysis");
        midObj.transform.SetParent(transform);
        midAnalysisSource = midObj.AddComponent<AudioSource>();
        midAnalysisSource.playOnAwake = false;
        midAnalysisSource.volume = 1f;
        midAnalysisSource.outputAudioMixerGroup = mutedAnalysisMixerGroup;

        // High analysis source
        GameObject highObj = new GameObject("High Analysis");
        highObj.transform.SetParent(transform);
        highAnalysisSource = highObj.AddComponent<AudioSource>();
        highAnalysisSource.playOnAwake = false;
        highAnalysisSource.volume = 1f;
        highAnalysisSource.outputAudioMixerGroup = mutedAnalysisMixerGroup;

        Debug.Log("MusicStemManager: Audio sources created successfully");
    }

    /// <summary>
    /// Play a specific track by index
    /// </summary>
    public void PlayTrack(int trackIndex)
    {
        if (trackIndex < 0 || trackIndex >= musicTracks.Length)
        {
            Debug.LogError($"MusicStemManager: Invalid track index {trackIndex}. Valid range: 0-{musicTracks.Length - 1}");
            return;
        }

        MusicTrack track = musicTracks[trackIndex];

        // Validate clips
        if (track.fullMixClip == null)
        {
            Debug.LogError($"MusicStemManager: Track '{track.trackName}' has no full mix clip!");
            return;
        }

        if (track.bassStemClip == null || track.midStemClip == null || track.highStemClip == null)
        {
            Debug.LogWarning($"MusicStemManager: Track '{track.trackName}' is missing some stem clips. Audio reactivity may not work properly.");
        }

        // Stop current track if playing
        StopCurrentTrack();

        // Setup full mix
        fullMixSource.clip = track.fullMixClip;
        fullMixSource.volume = track.volume;
        fullMixSource.loop = track.loop;

        // Setup stems
        if (track.bassStemClip != null)
        {
            bassAnalysisSource.clip = track.bassStemClip;
            bassAnalysisSource.loop = track.loop;
        }

        if (track.midStemClip != null)
        {
            midAnalysisSource.clip = track.midStemClip;
            midAnalysisSource.loop = track.loop;
        }

        if (track.highStemClip != null)
        {
            highAnalysisSource.clip = track.highStemClip;
            highAnalysisSource.loop = track.loop;
        }

        // Use PlayScheduled for sample-accurate synchronization
        // Get the next DSP time (Digital Signal Processing time - very accurate)
        double startTime = AudioSettings.dspTime + 0.1; // Start 100ms from now

        // Schedule all sources to start at exactly the same DSP time
        fullMixSource.PlayScheduled(startTime);
        if (track.bassStemClip != null) bassAnalysisSource.PlayScheduled(startTime);
        if (track.midStemClip != null) midAnalysisSource.PlayScheduled(startTime);
        if (track.highStemClip != null) highAnalysisSource.PlayScheduled(startTime);

        currentTrackIndex = trackIndex;
        hasInitialSync = false; // Will force sync check on next frame

        Debug.Log($"MusicStemManager: Now playing '{track.trackName}' (scheduled for sample-accurate sync)");
    }

    /// <summary>
    /// Play a track by name
    /// </summary>
    public void PlayTrackByName(string trackName)
    {
        for (int i = 0; i < musicTracks.Length; i++)
        {
            if (musicTracks[i].trackName == trackName)
            {
                PlayTrack(i);
                return;
            }
        }

        Debug.LogError($"MusicStemManager: Track '{trackName}' not found!");
    }

    /// <summary>
    /// Stop the currently playing track
    /// </summary>
    public void StopCurrentTrack()
    {
        fullMixSource.Stop();
        bassAnalysisSource.Stop();
        midAnalysisSource.Stop();
        highAnalysisSource.Stop();

        currentTrackIndex = -1;
    }

    /// <summary>
    /// Pause the currently playing track
    /// </summary>
    public void PauseCurrentTrack()
    {
        fullMixSource.Pause();
        bassAnalysisSource.Pause();
        midAnalysisSource.Pause();
        highAnalysisSource.Pause();
    }

    /// <summary>
    /// Resume the paused track
    /// </summary>
    public void ResumeCurrentTrack()
    {
        fullMixSource.UnPause();
        bassAnalysisSource.UnPause();
        midAnalysisSource.UnPause();
        highAnalysisSource.UnPause();
    }

    /// <summary>
    /// Check if all sources are still in sync, resync if needed
    /// </summary>
    private void CheckSync()
    {
        if (!fullMixSource.isPlaying) return;

        float masterTime = fullMixSource.time;

        // Check bass
        if (bassAnalysisSource.clip != null && bassAnalysisSource.isPlaying)
        {
            float drift = Mathf.Abs(bassAnalysisSource.time - masterTime);
            if (drift > SYNC_THRESHOLD)
            {
                bassAnalysisSource.time = masterTime;
                if (drift > 0.05f) // Only log significant drift
                {
                    Debug.LogWarning($"MusicStemManager: Bass stem resynced (drift: {drift * 1000:F1}ms)");
                }
            }
        }

        // Check mid
        if (midAnalysisSource.clip != null && midAnalysisSource.isPlaying)
        {
            float drift = Mathf.Abs(midAnalysisSource.time - masterTime);
            if (drift > SYNC_THRESHOLD)
            {
                midAnalysisSource.time = masterTime;
                if (drift > 0.05f)
                {
                    Debug.LogWarning($"MusicStemManager: Mid stem resynced (drift: {drift * 1000:F1}ms)");
                }
            }
        }

        // Check high
        if (highAnalysisSource.clip != null && highAnalysisSource.isPlaying)
        {
            float drift = Mathf.Abs(highAnalysisSource.time - masterTime);
            if (drift > SYNC_THRESHOLD)
            {
                highAnalysisSource.time = masterTime;
                if (drift > 0.05f)
                {
                    Debug.LogWarning($"MusicStemManager: High stem resynced (drift: {drift * 1000:F1}ms)");
                }
            }
        }
    }

    /// <summary>
    /// Force all stems to sync to master time on first frame
    /// </summary>
    private void ForceInitialSync()
    {
        if (!fullMixSource.isPlaying) return;

        float masterTime = fullMixSource.time;

        if (bassAnalysisSource.clip != null && bassAnalysisSource.isPlaying)
        {
            bassAnalysisSource.time = masterTime;
        }

        if (midAnalysisSource.clip != null && midAnalysisSource.isPlaying)
        {
            midAnalysisSource.time = masterTime;
        }

        if (highAnalysisSource.clip != null && highAnalysisSource.isPlaying)
        {
            highAnalysisSource.time = masterTime;
        }

        Debug.Log("MusicStemManager: Initial sync completed");
    }

    /// <summary>
    /// Set the volume of the full mix (what the player hears)
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        fullMixSource.volume = Mathf.Clamp01(volume);
    }

    /// <summary>
    /// Get the bass analysis audio source
    /// </summary>
    public AudioSource GetBassAnalysisSource()
    {
        return bassAnalysisSource;
    }

    /// <summary>
    /// Get the mid analysis audio source
    /// </summary>
    public AudioSource GetMidAnalysisSource()
    {
        return midAnalysisSource;
    }

    /// <summary>
    /// Get the high analysis audio source
    /// </summary>
    public AudioSource GetHighAnalysisSource()
    {
        return highAnalysisSource;
    }

    /// <summary>
    /// Get the full mix audio source
    /// </summary>
    public AudioSource GetFullMixSource()
    {
        return fullMixSource;
    }

    /// <summary>
    /// Check if a track is currently playing
    /// </summary>
    public bool IsPlaying()
    {
        return fullMixSource.isPlaying;
    }

    /// <summary>
    /// Get the name of the currently playing track
    /// </summary>
    public string GetCurrentTrackName()
    {
        if (currentTrackIndex >= 0 && currentTrackIndex < musicTracks.Length)
        {
            return musicTracks[currentTrackIndex].trackName;
        }
        return "None";
    }

    /// <summary>
    /// Crossfade to a different track
    /// </summary>
    public void CrossfadeToTrack(int trackIndex, float fadeTime = 1f)
    {
        StartCoroutine(CrossfadeCoroutine(trackIndex, fadeTime));
    }

    private System.Collections.IEnumerator CrossfadeCoroutine(int newTrackIndex, float fadeTime)
    {
        if (newTrackIndex < 0 || newTrackIndex >= musicTracks.Length)
        {
            Debug.LogError($"MusicStemManager: Invalid track index {newTrackIndex}");
            yield break;
        }

        float startVolume = fullMixSource.volume;
        float elapsed = 0f;

        // Fade out current track
        while (elapsed < fadeTime / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (fadeTime / 2f);
            fullMixSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        // Switch tracks
        PlayTrack(newTrackIndex);
        fullMixSource.volume = 0f;

        // Fade in new track
        elapsed = 0f;
        float targetVolume = musicTracks[newTrackIndex].volume;
        while (elapsed < fadeTime / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (fadeTime / 2f);
            fullMixSource.volume = Mathf.Lerp(0f, targetVolume, t);
            yield return null;
        }

        fullMixSource.volume = targetVolume;
    }
}