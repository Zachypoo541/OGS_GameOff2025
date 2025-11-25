using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [Header("Shake Settings")]
    [SerializeField] private float trauma = 0f;
    [SerializeField] private float traumaDecay = 1f;
    [SerializeField] private float maxShakeRotation = 5f;
    [SerializeField] private float shakeFrequency = 25f;

    private Vector3 _shakeRotation;
    private float _seed;

    void Start()
    {
        _seed = Random.Range(0f, 1000f);
    }

    void LateUpdate()
    {
        if (trauma > 0f)
        {
            // Decay trauma over time
            trauma = Mathf.Max(0f, trauma - traumaDecay * Time.deltaTime);

            // Calculate shake amount (squared for smoother falloff)
            float shakeAmount = trauma * trauma;

            // Generate Perlin noise based shake
            float time = Time.time * shakeFrequency;

            float rotX = (Mathf.PerlinNoise(_seed, time) - 0.5f) * 2f * maxShakeRotation * shakeAmount;
            float rotY = (Mathf.PerlinNoise(_seed + 1f, time) - 0.5f) * 2f * maxShakeRotation * shakeAmount;
            float rotZ = (Mathf.PerlinNoise(_seed + 2f, time) - 0.5f) * 2f * maxShakeRotation * shakeAmount;

            _shakeRotation = new Vector3(rotX, rotY, rotZ);
        }
        else
        {
            _shakeRotation = Vector3.zero;
        }
    }

    public Vector3 GetShakeRotation()
    {
        return _shakeRotation;
    }

    public void AddTrauma(float amount)
    {
        trauma = Mathf.Clamp01(trauma + amount);
    }

    public void SetTrauma(float amount)
    {
        trauma = Mathf.Clamp01(amount);
    }
}