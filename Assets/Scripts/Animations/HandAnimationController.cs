using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System.Collections;

public class HandAnimationController : MonoBehaviour
{
    [Header("Video Players")]
    [SerializeField] private VideoPlayer rightHandPlayer;
    [SerializeField] private VideoPlayer leftHandPlayer;

    [Header("Left Hand Action Clips")]
    [SerializeField] private VideoClip leftHandGrab;
    [SerializeField] private VideoClip leftHandButton;
    [SerializeField] private VideoClip leftHandCounter;

    [Header("Left Hand Action Speeds")]
    [Range(0.1f, 3f)]
    [SerializeField] private float grabSpeed = 1f;
    [Range(0.1f, 3f)]
    [SerializeField] private float buttonSpeed = 1f;
    [Range(0.1f, 3f)]
    [SerializeField] private float counterSpeed = 1f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    private WaveformHandAnimations _currentWaveform;
    private Coroutine _rightHandCoroutine;
    private Coroutine _leftHandCoroutine;

    // *** NEW: For looping thrust audio ***
    private AudioSource _thrustLoopAudioSource;
    private Coroutine _thrustAudioCoroutine;

    // For controlling left hand visibility via alpha
    private CanvasGroup _leftHandCanvasGroup;
    private RawImage _leftHandRawImage;

    private enum RightHandState
    {
        Idle,
        Entering,
        Firing,
        Exiting
    }

    private RightHandState _rightHandState = RightHandState.Idle;

    public void Initialize()
    {
        if (enableDebugLogs)
            Debug.Log("[HandAnimController] Initializing...");

        if (rightHandPlayer != null)
        {
            rightHandPlayer.isLooping = false;
            rightHandPlayer.loopPointReached += OnRightHandVideoEnd;

            if (enableDebugLogs)
                Debug.Log($"[HandAnimController] Right hand player initialized. RenderMode: {rightHandPlayer.renderMode}");
        }

        if (leftHandPlayer != null)
        {
            leftHandPlayer.isLooping = false;

            // Get or add CanvasGroup for alpha control
            _leftHandCanvasGroup = leftHandPlayer.GetComponent<CanvasGroup>();
            if (_leftHandCanvasGroup == null)
            {
                _leftHandCanvasGroup = leftHandPlayer.gameObject.AddComponent<CanvasGroup>();
            }

            // Try to get RawImage component (if using UI RawImage for display)
            _leftHandRawImage = leftHandPlayer.GetComponent<RawImage>();

            // Hide left hand initially
            HideLeftHand();

            if (enableDebugLogs)
                Debug.Log("[HandAnimController] Left hand player initialized.");
        }

        // *** NEW: Create AudioSource for looping thrust sound ***
        _thrustLoopAudioSource = gameObject.AddComponent<AudioSource>();
        _thrustLoopAudioSource.loop = true;
        _thrustLoopAudioSource.playOnAwake = false;
        _thrustLoopAudioSource.spatialBlend = 0f; // 2D sound for player
    }

    private void OnDestroy()
    {
        if (rightHandPlayer != null)
            rightHandPlayer.loopPointReached -= OnRightHandVideoEnd;

        // *** NEW: Clean up thrust audio ***
        if (_thrustAudioCoroutine != null)
            StopCoroutine(_thrustAudioCoroutine);
    }

    #region Right Hand (Waveform) Control
    private void ShowRightHand()
    {
        // Get or add CanvasGroup for right hand player if needed
        CanvasGroup rightHandCanvasGroup = rightHandPlayer.GetComponent<CanvasGroup>();
        if (rightHandCanvasGroup == null)
        {
            rightHandCanvasGroup = rightHandPlayer.gameObject.AddComponent<CanvasGroup>();
        }

        rightHandCanvasGroup.alpha = 1f;

        if (enableDebugLogs)
            Debug.Log("[HandAnimController] Right hand shown (alpha = 1)");
    }

    public void SwitchWaveform(WaveformHandAnimations newWaveform)
    {
        if (newWaveform == null)
        {
            return;
        }

        WaveformHandAnimations oldWaveform = _currentWaveform;
        _currentWaveform = newWaveform;

        if (_rightHandCoroutine != null)
            StopCoroutine(_rightHandCoroutine);

        _rightHandCoroutine = StartCoroutine(TransitionToNewWaveform(oldWaveform, newWaveform));
    }

    private IEnumerator TransitionToNewWaveform(WaveformHandAnimations oldWaveform, WaveformHandAnimations newWaveform)
    {
        if (enableDebugLogs)
            Debug.Log("[HandAnimController] Starting transition to new waveform...");

        if (oldWaveform != null && oldWaveform.exit != null)
        {
            if (enableDebugLogs)
                Debug.Log($"[HandAnimController] Playing EXIT animation: {oldWaveform.exit.name}");

            _rightHandState = RightHandState.Exiting;
            PlayRightHandClip(oldWaveform.exit, false);

            float prepTimeout = 2f;
            float prepElapsed = 0f;
            while (!rightHandPlayer.isPrepared && prepElapsed < prepTimeout)
            {
                prepElapsed += Time.deltaTime;
                yield return null;
            }

            if (enableDebugLogs)
                Debug.Log($"[HandAnimController] Exit prepared. IsPrepared: {rightHandPlayer.isPrepared}, IsPlaying: {rightHandPlayer.isPlaying}");

            if (rightHandPlayer.isPrepared)
            {
                yield return null;

                float timeout = 10f;
                float elapsed = 0f;
                while (rightHandPlayer.isPlaying && elapsed < timeout)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (enableDebugLogs)
                {
                    Debug.Log("[HandAnimController] Exit animation finished.");
                }
            }
        }

        if (newWaveform.enter != null)
        {
            if (enableDebugLogs)
                Debug.Log($"[HandAnimController] Playing ENTER animation: {newWaveform.enter.name}");

            _rightHandState = RightHandState.Entering;
            PlayRightHandClip(newWaveform.enter, false);

            float prepTimeout = 2f;
            float prepElapsed = 0f;
            while (!rightHandPlayer.isPrepared && prepElapsed < prepTimeout)
            {
                prepElapsed += Time.deltaTime;
                yield return null;
            }

            if (enableDebugLogs)
                Debug.Log($"[HandAnimController] Enter prepared. IsPrepared: {rightHandPlayer.isPrepared}, IsPlaying: {rightHandPlayer.isPlaying}");

            if (rightHandPlayer.isPrepared)
            {
                yield return null;

                float timeout = 10f;
                float elapsed = 0f;
                while (rightHandPlayer.isPlaying && elapsed < timeout)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (elapsed >= timeout)
                {
                }
                else if (enableDebugLogs)
                {
                    Debug.Log("[HandAnimController] Enter animation finished.");
                }
            }
        }

        StartRightHandIdle();
    }

    public void PlayFireAnimation()
    {
        if (_currentWaveform == null)
        {
            return;
        }

        if (_currentWaveform.fire == null)
        {
            return;
        }

        if (enableDebugLogs)
            Debug.Log($"[HandAnimController] PlayFireAnimation called for {_currentWaveform.fire.name}");

        if (_rightHandCoroutine != null)
            StopCoroutine(_rightHandCoroutine);

        _rightHandCoroutine = StartCoroutine(PlayFireSequence());
    }

    private IEnumerator PlayFireSequence()
    {
        if (enableDebugLogs)
            Debug.Log($"[HandAnimController] Playing FIRE animation: {_currentWaveform.fire.name}");

        _rightHandState = RightHandState.Firing;
        PlayRightHandClip(_currentWaveform.fire, false);

        float prepTimeout = 2f;
        float prepElapsed = 0f;
        while (!rightHandPlayer.isPrepared && prepElapsed < prepTimeout)
        {
            prepElapsed += Time.deltaTime;
            yield return null;
        }

        if (enableDebugLogs)
            Debug.Log($"[HandAnimController] Fire prepared. IsPrepared: {rightHandPlayer.isPrepared}, IsPlaying: {rightHandPlayer.isPlaying}");

        if (rightHandPlayer.isPrepared)
        {
            yield return null;

            float timeout = 10f;
            float elapsed = 0f;
            while (rightHandPlayer.isPlaying && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (enableDebugLogs)
            {
                Debug.Log("[HandAnimController] Fire animation finished, returning to idle.");
            }
        }

        StartRightHandIdle();
    }

    private void StartRightHandIdle()
    {
        if (_currentWaveform != null && _currentWaveform.idle != null)
        {
            if (enableDebugLogs)
                Debug.Log($"[HandAnimController] Starting IDLE loop: {_currentWaveform.idle.name}");

            _rightHandState = RightHandState.Idle;
            PlayRightHandClip(_currentWaveform.idle, true);
        }
    }

    private void PlayRightHandClip(VideoClip clip, bool loop)
    {
        if (rightHandPlayer == null)
        {
            return;
        }

        if (clip == null)
        {
            return;
        }

        if (enableDebugLogs)
            Debug.Log($"[HandAnimController] Setting clip: {clip.name}, Loop: {loop}, Duration: {clip.length}s");

        if (rightHandPlayer.isPlaying)
        {
            rightHandPlayer.Stop();
        }

        rightHandPlayer.clip = clip;
        rightHandPlayer.isLooping = loop;
        rightHandPlayer.Play();

        if (enableDebugLogs)
        {
            Debug.Log($"[HandAnimController] Play() called for clip: {clip.name}");
        }
    }

    private void OnRightHandVideoEnd(VideoPlayer vp)
    {
        if (enableDebugLogs)
            Debug.Log($"[HandAnimController] Video ended. State: {_rightHandState}");

        if (_rightHandState == RightHandState.Idle && _currentWaveform != null)
        {
            if (enableDebugLogs)
                Debug.Log("[HandAnimController] Restarting idle loop from OnVideoEnd");
            StartRightHandIdle();
        }
    }

    public void SetInitialWaveform(WaveformHandAnimations waveform)
    {
        if (waveform == null)
        {
            return;
        }

        _currentWaveform = waveform;

        // Show the right hand when setting initial waveform
        ShowRightHand();

        if (_rightHandCoroutine != null)
            StopCoroutine(_rightHandCoroutine);

        _rightHandCoroutine = StartCoroutine(PlayInitialSequence());
    }

    private IEnumerator PlayInitialSequence()
    {
        if (enableDebugLogs)
            Debug.Log("[HandAnimController] Starting initial sequence...");

        if (_currentWaveform.enter != null)
        {
            if (enableDebugLogs)
                Debug.Log($"[HandAnimController] Playing INITIAL ENTER: {_currentWaveform.enter.name}");

            _rightHandState = RightHandState.Entering;
            PlayRightHandClip(_currentWaveform.enter, false);

            float prepTimeout = 2f;
            float prepElapsed = 0f;
            while (!rightHandPlayer.isPrepared && prepElapsed < prepTimeout)
            {
                prepElapsed += Time.deltaTime;
                yield return null;
            }

            if (enableDebugLogs)
                Debug.Log($"[HandAnimController] Initial enter prepared. IsPrepared: {rightHandPlayer.isPrepared}, IsPlaying: {rightHandPlayer.isPlaying}");

            if (rightHandPlayer.isPrepared)
            {
                yield return null;

                float timeout = 10f;
                float elapsed = 0f;
                while (rightHandPlayer.isPlaying && elapsed < timeout)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (enableDebugLogs)
                {
                    Debug.Log("[HandAnimController] Initial enter finished.");
                }
            }
        }

        StartRightHandIdle();
    }

    #endregion

    #region Left Hand (Action) Control

    // Methods to show/hide left hand using alpha
    private void ShowLeftHand()
    {
        if (_leftHandCanvasGroup != null)
        {
            _leftHandCanvasGroup.alpha = 1f;
            if (enableDebugLogs)
                Debug.Log("[HandAnimController] Left hand shown (alpha = 1)");
        }
    }

    private void HideLeftHand()
    {
        if (_leftHandCanvasGroup != null)
        {
            _leftHandCanvasGroup.alpha = 0f;
            if (enableDebugLogs)
                Debug.Log("[HandAnimController] Left hand hidden (alpha = 0)");
        }
    }

    // *** NEW: Play left hand action with audio support ***
    public void PlayLeftHandAction(VideoClip actionClip, bool returnToIdle = true, float playbackSpeed = 1f, WaveformData waveformData = null)
    {
        if (leftHandPlayer == null || actionClip == null)
        {
            return;
        }

        if (_leftHandCoroutine != null)
            StopCoroutine(_leftHandCoroutine);

        _leftHandCoroutine = StartCoroutine(PlayLeftHandActionSequence(actionClip, returnToIdle, playbackSpeed, waveformData));
    }

    private IEnumerator PlayLeftHandActionSequence(VideoClip clip, bool returnToIdle, float playbackSpeed, WaveformData waveformData)
    {
        if (enableDebugLogs)
            Debug.Log($"[HandAnimController] Playing left hand action: {clip.name}, Duration: {clip.length}s, Speed: {playbackSpeed}x");

        // Show left hand before playing
        ShowLeftHand();

        leftHandPlayer.clip = clip;
        leftHandPlayer.isLooping = false;
        leftHandPlayer.playbackSpeed = playbackSpeed;

        // Prepare and play
        leftHandPlayer.Prepare();

        // Wait for preparation
        float prepTimeout = 2f;
        float prepElapsed = 0f;
        while (!leftHandPlayer.isPrepared && prepElapsed < prepTimeout)
        {
            prepElapsed += Time.deltaTime;
            yield return null;
        }

        if (!leftHandPlayer.isPrepared)
        {
            HideLeftHand();
            yield break;
        }

        leftHandPlayer.Play();

        // *** NEW: Play self-cast sound with delay ***
        if (waveformData != null && waveformData.selfCastSound != null)
        {
            if (waveformData.selfCastSoundDelay > 0f)
            {
                yield return new WaitForSeconds(waveformData.selfCastSoundDelay);
            }

            SoundFXManager.instance.PlayPlayerSound(
                waveformData.selfCastSound,
                waveformData.selfCastSoundVolume,
                waveformData.selfCastSoundPitchRange.x,
                waveformData.selfCastSoundPitchRange.y
            );
        }

        if (enableDebugLogs)
            Debug.Log($"[HandAnimController] Left hand video playing. IsPlaying: {leftHandPlayer.isPlaying}");

        // Wait for the video to finish
        float timeout = 10f;
        float elapsed = 0f;
        while (leftHandPlayer.isPlaying && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (enableDebugLogs)
        {
            Debug.Log($"[HandAnimController] Left hand action finished: {clip.name}");
        }

        // Reset playback speed to normal
        leftHandPlayer.playbackSpeed = 1f;

        // Hide left hand after playing
        if (returnToIdle)
        {
            HideLeftHand();
        }
    }

    // *** NEW: For Square wave thrust with enter/loop/exit audio ***
    public void StartLeftHandLoop(VideoClip loopClip, float playbackSpeed = 1f, WaveformData waveformData = null)
    {
        if (leftHandPlayer == null || loopClip == null)
        {
            return;
        }

        if (_leftHandCoroutine != null)
            StopCoroutine(_leftHandCoroutine);

        if (_thrustAudioCoroutine != null)
            StopCoroutine(_thrustAudioCoroutine);

        if (enableDebugLogs)
            Debug.Log($"[HandAnimController] Starting left hand loop: {loopClip.name}, Speed: {playbackSpeed}x");

        // Show left hand for looping
        ShowLeftHand();

        if (leftHandPlayer.isPlaying)
        {
            leftHandPlayer.Stop();
        }

        leftHandPlayer.clip = loopClip;
        leftHandPlayer.isLooping = true;
        leftHandPlayer.playbackSpeed = playbackSpeed;
        leftHandPlayer.Play();

        // *** NEW: Start thrust audio sequence ***
        if (waveformData != null)
        {
            _thrustAudioCoroutine = StartCoroutine(PlayThrustAudioSequence(waveformData));
        }
    }

    // *** NEW: Thrust audio sequence (start -> loop -> end) ***
    private IEnumerator PlayThrustAudioSequence(WaveformData waveformData)
    {
        // Play start sound
        if (waveformData.thrustStartSound != null)
        {
            SoundFXManager.instance.PlayPlayerSound(
                waveformData.thrustStartSound,
                waveformData.thrustStartVolume
            );

            // Wait for start sound to finish before starting loop
            yield return new WaitForSeconds(waveformData.thrustStartSound.length);
        }

        // Start loop sound
        if (waveformData.thrustLoopSound != null && _thrustLoopAudioSource != null)
        {
            _thrustLoopAudioSource.clip = waveformData.thrustLoopSound;
            _thrustLoopAudioSource.volume = waveformData.thrustLoopVolume;
            _thrustLoopAudioSource.Play();

            if (enableDebugLogs)
                Debug.Log("[HandAnimController] Thrust loop sound started");
        }
    }

    // *** MODIFIED: Stop left hand loop with exit audio ***
    public void StopLeftHandLoop(WaveformData waveformData = null)
    {
        if (enableDebugLogs)
            Debug.Log("[HandAnimController] Stopping left hand loop");

        if (leftHandPlayer != null && leftHandPlayer.isPlaying)
        {
            leftHandPlayer.Stop();
            leftHandPlayer.playbackSpeed = 1f;
        }

        // *** NEW: Stop thrust audio and play end sound ***
        if (_thrustAudioCoroutine != null)
        {
            StopCoroutine(_thrustAudioCoroutine);
            _thrustAudioCoroutine = null;
        }

        // Stop loop sound
        if (_thrustLoopAudioSource != null && _thrustLoopAudioSource.isPlaying)
        {
            _thrustLoopAudioSource.Stop();

            if (enableDebugLogs)
                Debug.Log("[HandAnimController] Thrust loop sound stopped");
        }

        // Play end sound
        if (waveformData != null && waveformData.thrustEndSound != null)
        {
            SoundFXManager.instance.PlayPlayerSound(
                waveformData.thrustEndSound,
                waveformData.thrustEndVolume
            );
        }
    }

    public void PlayGrabAction()
    {
        PlayLeftHandAction(leftHandGrab, true, grabSpeed);
    }

    public void PlayButtonAction()
    {
        PlayLeftHandAction(leftHandButton, true, buttonSpeed);
    }

    public void PlayCounterAction()
    {
        PlayLeftHandAction(leftHandCounter, true, counterSpeed);
    }

    #endregion
}