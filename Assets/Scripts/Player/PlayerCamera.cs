using System;
using UnityEngine;

public struct CameraInput
{
    public Vector2 Look;
}

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private float sensitivity = 0.1f;
    [SerializeField] private float verticalPositionDampening = 10f;

    [Header("Camera Limits")]
    [SerializeField] private float minPitch = -89f;
    [SerializeField] private float maxPitch = 89f;

    private Vector3 _eulerAngles;
    private float _currentYPosition;
    private CameraShake _cameraShake;

    public void Initialize(Transform target)
    {
        transform.position = target.position;
        _currentYPosition = target.position.y;
        transform.eulerAngles = _eulerAngles = target.eulerAngles;

        // Get or add CameraShake component
        _cameraShake = GetComponent<CameraShake>();
        if (_cameraShake == null)
        {
            _cameraShake = gameObject.AddComponent<CameraShake>();
        }
    }

    public void UpdateRotation(CameraInput input)
    {
        _eulerAngles += new Vector3(-input.Look.y, input.Look.x) * sensitivity;

        // Clamp the pitch (X rotation) to prevent view inversion
        _eulerAngles.x = Mathf.Clamp(_eulerAngles.x, minPitch, maxPitch);

        // Apply base rotation
        transform.eulerAngles = _eulerAngles;

        // Apply additive shake rotation
        if (_cameraShake != null)
        {
            Vector3 shakeRotation = _cameraShake.GetShakeRotation();
            transform.localRotation *= Quaternion.Euler(shakeRotation);
        }
    }

    public void UpdatePosition(Transform target)
    {
        // Smoothly interpolate Y position
        _currentYPosition = Mathf.Lerp(
            _currentYPosition,
            target.position.y,
            Time.deltaTime * verticalPositionDampening
        );

        // Apply position with smoothed Y and instant X/Z
        transform.position = new Vector3(
            target.position.x,
            _currentYPosition,
            target.position.z
        );
    }

    public void AddCameraShake(float intensity)
    {
        if (_cameraShake != null)
        {
            _cameraShake.AddTrauma(intensity);
        }
    }
}