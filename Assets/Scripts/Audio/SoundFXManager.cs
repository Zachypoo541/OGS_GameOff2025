using UnityEngine;

public class SoundFXManager : MonoBehaviour
{
    public static SoundFXManager instance;

    [SerializeField] private AudioSource soundFXObject; // For 3D spatial sounds
    [SerializeField] private AudioSource player2DSource; // For non-spatial player sounds

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    // 3D spatial sound (existing method)
    public void PlaySoundFXClip(AudioClip audioClip, Transform spawnTransform, float volume)
    {
        if (audioClip == null) return;

        AudioSource audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);
        audioSource.clip = audioClip;
        audioSource.volume = volume;
        audioSource.Play();
        float clipLength = audioSource.clip.length;
        Destroy(audioSource.gameObject, clipLength);
    }

    // Overload with pitch variation for 3D sounds
    public void PlaySoundFXClip(AudioClip audioClip, Transform spawnTransform, float volume, float pitch)
    {
        if (audioClip == null) return;

        AudioSource audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);
        audioSource.clip = audioClip;
        audioSource.volume = volume;
        audioSource.pitch = pitch;
        audioSource.Play();
        float clipLength = audioSource.clip.length;
        Destroy(audioSource.gameObject, clipLength);
    }

    // 2D player sound
    public void PlayPlayerSound(AudioClip audioClip, float volume = 1f, float pitch = 1f)
    {
        if (player2DSource == null || audioClip == null) return;

        player2DSource.pitch = pitch;
        player2DSource.PlayOneShot(audioClip, volume);
    }

    // Random pitch variation helper
    public void PlayPlayerSound(AudioClip audioClip, float volume, float minPitch, float maxPitch)
    {
        float randomPitch = Random.Range(minPitch, maxPitch);
        PlayPlayerSound(audioClip, volume, randomPitch);
    }

    // 3D sound with random pitch
    public void PlaySoundFXClip(AudioClip audioClip, Transform spawnTransform, float volume, float minPitch, float maxPitch)
    {
        float randomPitch = Random.Range(minPitch, maxPitch);
        PlaySoundFXClip(audioClip, spawnTransform, volume, randomPitch);
    }
}