using UnityEngine;
using UnityEngine.Video;
using System.Collections;

/// <summary>
/// Add this component to VideoPlayers that flash black on first use.
/// It preloads a dummy frame on start to avoid the black flash.
/// </summary>
public class VideoPlayerPreloader : MonoBehaviour
{
    private VideoPlayer videoPlayer;

    private void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer != null)
        {
            StartCoroutine(PreloadFirstFrame());
        }
    }

    private IEnumerator PreloadFirstFrame()
    {
        // Prepare the video player to avoid black flash
        if (videoPlayer.clip != null)
        {
            videoPlayer.Prepare();

            // Wait for preparation
            while (!videoPlayer.isPrepared)
            {
                yield return null;
            }

            // Seek to first frame
            videoPlayer.frame = 0;
        }
    }
}