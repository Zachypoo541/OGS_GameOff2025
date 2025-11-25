using UnityEngine;
using UnityEngine.Video;
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

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    private WaveformHandAnimations _currentWaveform;
    private Coroutine _rightHandCoroutine;
    private Coroutine _leftHandCoroutine;

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
        else
        {
            Debug.LogError("[HandAnimController] Right hand player is NULL!");
        }

        if (leftHandPlayer != null)
        {
            leftHandPlayer.isLooping = false;
            if (enableDebugLogs)
                Debug.Log("[HandAnimController] Left hand player initialized.");
        }
        else
        {
            Debug.LogWarning("[HandAnimController] Left hand player is NULL!");
        }
    }

    private void OnDestroy()
    {
        if (rightHandPlayer != null)
            rightHandPlayer.loopPointReached -= OnRightHandVideoEnd;
    }

    #region Right Hand (Waveform) Control

    public void SwitchWaveform(WaveformHandAnimations newWaveform)
    {
        Debug.Log($"[HandAnimController] === SwitchWaveform called ===");
        Debug.Log($"[HandAnimController] New waveform is null? {newWaveform == null}");

        if (newWaveform == null)
        {
            Debug.LogError("[HandAnimController] Trying to switch to NULL waveform!");
            return;
        }

        Debug.Log($"[HandAnimController] New Enter clip: {(newWaveform.enter != null ? newWaveform.enter.name : "NULL")}");
        Debug.Log($"[HandAnimController] New Idle clip: {(newWaveform.idle != null ? newWaveform.idle.name : "NULL")}");
        Debug.Log($"[HandAnimController] New Fire clip: {(newWaveform.fire != null ? newWaveform.fire.name : "NULL")}");
        Debug.Log($"[HandAnimController] New Exit clip: {(newWaveform.exit != null ? newWaveform.exit.name : "NULL")}");

        if (_rightHandCoroutine != null)
            StopCoroutine(_rightHandCoroutine);

        _rightHandCoroutine = StartCoroutine(TransitionToNewWaveform(newWaveform));
    }

    private IEnumerator TransitionToNewWaveform(WaveformHandAnimations newWaveform)
    {
        if (enableDebugLogs)
            Debug.Log("[HandAnimController] Starting transition to new waveform...");

        // Play exit animation if we have a current waveform
        if (_currentWaveform != null && _currentWaveform.exit != null)
        {
            if (enableDebugLogs)
                Debug.Log($"[HandAnimController] Playing EXIT animation: {_currentWaveform.exit.name}");

            _rightHandState = RightHandState.Exiting;
            PlayRightHandClip(_currentWaveform.exit, false);

            // Wait for video to be prepared and start playing
            float prepTimeout = 2f;
            float prepElapsed = 0f;
            while (!rightHandPlayer.isPrepared && prepElapsed < prepTimeout)
            {
                prepElapsed += Time.deltaTime;
                yield return null;
            }

            if (enableDebugLogs)
                Debug.Log($"[HandAnimController] Exit prepared. IsPrepared: {rightHandPlayer.isPrepared}, IsPlaying: {rightHandPlayer.isPlaying}");

            // Now wait for it to finish playing
            if (rightHandPlayer.isPrepared)
            {
                // Wait one more frame for isPlaying to update
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
                    Debug.LogWarning("[HandAnimController] Exit animation timed out!");
                }
                else if (enableDebugLogs)
                {
                    Debug.Log("[HandAnimController] Exit animation finished.");
                }
            }
        }

        // Switch to new waveform
        _currentWaveform = newWaveform;

        // Play enter animation
        if (_currentWaveform.enter != null)
        {
            if (enableDebugLogs)
                Debug.Log($"[HandAnimController] Playing ENTER animation: {_currentWaveform.enter.name}");

            _rightHandState = RightHandState.Entering;
            PlayRightHandClip(_currentWaveform.enter, false);

            // Wait for video to be prepared and start playing
            float prepTimeout = 2f;
            float prepElapsed = 0f;
            while (!rightHandPlayer.isPrepared && prepElapsed < prepTimeout)
            {
                prepElapsed += Time.deltaTime;
                yield return null;
            }

            if (enableDebugLogs)
                Debug.Log($"[HandAnimController] Enter prepared. IsPrepared: {rightHandPlayer.isPrepared}, IsPlaying: {rightHandPlayer.isPlaying}");

            // Now wait for it to finish playing
            if (rightHandPlayer.isPrepared)
            {
                // Wait one more frame for isPlaying to update
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
                    Debug.LogWarning("[HandAnimController] Enter animation timed out!");
                }
                else if (enableDebugLogs)
                {
                    Debug.Log("[HandAnimController] Enter animation finished.");
                }
            }
        }

        // Start idle loop
        StartRightHandIdle();
    }

    public void PlayFireAnimation()
    {
        if (_currentWaveform == null)
        {
            Debug.LogWarning("[HandAnimController] Cannot fire - no current waveform!");
            return;
        }

        if (_currentWaveform.fire == null)
        {
            Debug.LogWarning("[HandAnimController] Cannot fire - no fire animation assigned!");
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

        // Wait for video to be prepared and start playing
        float prepTimeout = 2f;
        float prepElapsed = 0f;
        while (!rightHandPlayer.isPrepared && prepElapsed < prepTimeout)
        {
            prepElapsed += Time.deltaTime;
            yield return null;
        }

        if (enableDebugLogs)
            Debug.Log($"[HandAnimController] Fire prepared. IsPrepared: {rightHandPlayer.isPrepared}, IsPlaying: {rightHandPlayer.isPlaying}");

        // Now wait for it to finish playing
        if (rightHandPlayer.isPrepared)
        {
            // Wait one more frame for isPlaying to update
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
                Debug.LogWarning("[HandAnimController] Fire animation timed out!");
            }
            else if (enableDebugLogs)
            {
                Debug.Log("[HandAnimController] Fire animation finished, returning to idle.");
            }
        }

        // Return to idle
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
        else
        {
            Debug.LogWarning($"[HandAnimController] Cannot start idle - waveform or idle clip is null! Waveform: {_currentWaveform != null}, Idle: {(_currentWaveform != null ? _currentWaveform.idle != null : false)}");
        }
    }

    private void PlayRightHandClip(VideoClip clip, bool loop)
    {
        if (rightHandPlayer == null)
        {
            Debug.LogError("[HandAnimController] Cannot play clip - rightHandPlayer is NULL!");
            return;
        }

        if (clip == null)
        {
            Debug.LogError("[HandAnimController] Cannot play clip - clip is NULL!");
            return;
        }

        if (enableDebugLogs)
            Debug.Log($"[HandAnimController] Setting clip: {clip.name}, Loop: {loop}, Duration: {clip.length}s");

        // Stop any current playback
        if (rightHandPlayer.isPlaying)
        {
            rightHandPlayer.Stop();
        }

        // Set the clip
        rightHandPlayer.clip = clip;
        rightHandPlayer.isLooping = loop;

        // Important: We need to call Play() which will prepare and play
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

        // Safety check for idle loop
        if (_rightHandState == RightHandState.Idle && _currentWaveform != null)
        {
            if (enableDebugLogs)
                Debug.Log("[HandAnimController] Restarting idle loop from OnVideoEnd");
            StartRightHandIdle();
        }
    }

    public void SetInitialWaveform(WaveformHandAnimations waveform)
    {
        Debug.Log($"[HandAnimController] === SetInitialWaveform called ===");
        Debug.Log($"[HandAnimController] Waveform is null? {waveform == null}");

        if (waveform == null)
        {
            Debug.LogError("[HandAnimController] Trying to set NULL initial waveform!");
            return;
        }

        Debug.Log($"[HandAnimController] Enter clip: {(waveform.enter != null ? waveform.enter.name : "NULL")}");
        Debug.Log($"[HandAnimController] Idle clip: {(waveform.idle != null ? waveform.idle.name : "NULL")}");
        Debug.Log($"[HandAnimController] Fire clip: {(waveform.fire != null ? waveform.fire.name : "NULL")}");
        Debug.Log($"[HandAnimController] Exit clip: {(waveform.exit != null ? waveform.exit.name : "NULL")}");

        _currentWaveform = waveform;

        if (_rightHandCoroutine != null)
            StopCoroutine(_rightHandCoroutine);

        _rightHandCoroutine = StartCoroutine(PlayInitialSequence());
    }

    private IEnumerator PlayInitialSequence()
    {
        if (enableDebugLogs)
            Debug.Log("[HandAnimController] Starting initial sequence...");

        // Play enter animation
        if (_currentWaveform.enter != null)
        {
            if (enableDebugLogs)
                Debug.Log($"[HandAnimController] Playing INITIAL ENTER: {_currentWaveform.enter.name}");

            _rightHandState = RightHandState.Entering;
            PlayRightHandClip(_currentWaveform.enter, false);

            // Wait for video to be prepared and start playing
            float prepTimeout = 2f;
            float prepElapsed = 0f;
            while (!rightHandPlayer.isPrepared && prepElapsed < prepTimeout)
            {
                prepElapsed += Time.deltaTime;
                yield return null;
            }

            if (enableDebugLogs)
                Debug.Log($"[HandAnimController] Initial enter prepared. IsPrepared: {rightHandPlayer.isPrepared}, IsPlaying: {rightHandPlayer.isPlaying}");

            // Now wait for it to finish playing
            if (rightHandPlayer.isPrepared)
            {
                // Wait one more frame for isPlaying to update
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
                    Debug.LogWarning("[HandAnimController] Initial enter timed out!");
                }
                else if (enableDebugLogs)
                {
                    Debug.Log("[HandAnimController] Initial enter finished.");
                }
            }
        }

        // Start idle loop
        StartRightHandIdle();
    }

    #endregion

    #region Left Hand (Action) Control

    public void PlayLeftHandAction(VideoClip actionClip, bool returnToIdle = true)
    {
        if (leftHandPlayer == null || actionClip == null)
            return;

        if (_leftHandCoroutine != null)
            StopCoroutine(_leftHandCoroutine);

        _leftHandCoroutine = StartCoroutine(PlayLeftHandActionSequence(actionClip, returnToIdle));
    }

    private IEnumerator PlayLeftHandActionSequence(VideoClip clip, bool returnToIdle)
    {
        leftHandPlayer.clip = clip;
        leftHandPlayer.isLooping = false;
        leftHandPlayer.Play();

        yield return new WaitUntil(() => !leftHandPlayer.isPlaying);

        if (returnToIdle)
        {
            // You can set a default left hand idle here if you have one
        }
    }

    public void PlayGrabAction()
    {
        PlayLeftHandAction(leftHandGrab);
    }

    public void PlayButtonAction()
    {
        PlayLeftHandAction(leftHandButton);
    }

    public void PlayCounterAction()
    {
        PlayLeftHandAction(leftHandCounter);
    }

    #endregion
}