using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Automatically loads the appropriate music track when a scene loads.
/// Attach this to your MusicManager GameObject.
/// </summary>
public class SceneBasedMusicLoader : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the MusicStemManager")]
    public MusicStemManager musicManager;

    [Header("Scene to Track Mapping")]
    [Tooltip("Map scene names to track indices or names")]
    public SceneMusicMapping[] sceneMappings;

    [Header("Settings")]
    [Tooltip("Use crossfade when changing tracks")]
    public bool useCrossfade = true;

    [Tooltip("Crossfade duration in seconds")]
    [Range(0.5f, 5f)]
    public float crossfadeDuration = 2f;

    [System.Serializable]
    public class SceneMusicMapping
    {
        [Tooltip("Name of the scene (must match exactly)")]
        public string sceneName;

        [Tooltip("Index of the track to play (or -1 to use track name)")]
        public int trackIndex = 0;

        [Tooltip("Or specify track by name (if trackIndex is -1)")]
        public string trackName = "";
    }

    private string currentSceneName = "";

    private void Awake()
    {
        // Auto-find MusicStemManager if not assigned
        if (musicManager == null)
        {
            musicManager = GetComponent<MusicStemManager>();
            if (musicManager == null)
            {
                Debug.LogError("SceneBasedMusicLoader: No MusicStemManager found!");
                enabled = false;
                return;
            }
        }

        // Subscribe to scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        // Load music for current scene
        LoadMusicForCurrentScene();
    }

    private void OnDestroy()
    {
        // Unsubscribe from scene loaded event
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        LoadMusicForScene(scene.name);
    }

    /// <summary>
    /// Load music for the current scene
    /// </summary>
    public void LoadMusicForCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        LoadMusicForScene(sceneName);
    }

    /// <summary>
    /// Load music for a specific scene
    /// </summary>
    private void LoadMusicForScene(string sceneName)
    {
        // Check if we're already playing this scene's music
        if (sceneName == currentSceneName && musicManager.IsPlaying())
        {
            return; // Already playing correct music
        }

        // Find mapping for this scene
        foreach (var mapping in sceneMappings)
        {
            if (mapping.sceneName == sceneName)
            {
                // Determine track to play
                if (mapping.trackIndex >= 0)
                {
                    // Play by index
                    if (useCrossfade && currentSceneName != "")
                    {
                        musicManager.CrossfadeToTrack(mapping.trackIndex, crossfadeDuration);
                    }
                    else
                    {
                        musicManager.PlayTrack(mapping.trackIndex);
                    }
                }
                else if (!string.IsNullOrEmpty(mapping.trackName))
                {
                    // Play by name
                    // Note: CrossfadeToTrack only works with indices, so we find the index first
                    int trackIndex = FindTrackIndexByName(mapping.trackName);
                    if (trackIndex >= 0)
                    {
                        if (useCrossfade && currentSceneName != "")
                        {
                            musicManager.CrossfadeToTrack(trackIndex, crossfadeDuration);
                        }
                        else
                        {
                            musicManager.PlayTrack(trackIndex);
                        }
                    }
                    else
                    {
                        Debug.LogError($"SceneBasedMusicLoader: Track '{mapping.trackName}' not found!");
                    }
                }

                currentSceneName = sceneName;
                Debug.Log($"SceneBasedMusicLoader: Loaded music for scene '{sceneName}'");
                return;
            }
        }

        Debug.LogWarning($"SceneBasedMusicLoader: No music mapping found for scene '{sceneName}'");
    }

    /// <summary>
    /// Find a track index by name
    /// </summary>
    private int FindTrackIndexByName(string trackName)
    {
        for (int i = 0; i < musicManager.musicTracks.Length; i++)
        {
            if (musicManager.musicTracks[i].trackName == trackName)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Manually switch to a track for a specific scene
    /// </summary>
    public void SwitchToSceneMusic(string sceneName)
    {
        LoadMusicForScene(sceneName);
    }
}