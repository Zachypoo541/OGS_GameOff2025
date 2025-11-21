using System;
using UnityEngine;

public struct CameraInput
{
    public Vector2 Look;
}

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private float sensitivity = 0.1f;
    [SerializeField] private float verticalPositionDampening = 10f; // Higher = faster, lower = smoother

    private Vector3 _eulerAngles;
    private float _currentYPosition;

    public void Initialize(Transform target)
    {
        transform.position = target.position;
        _currentYPosition = target.position.y;
        transform.eulerAngles = _eulerAngles = target.eulerAngles;
    }

    public void UpdateRotation(CameraInput input)
    {
        _eulerAngles += new Vector3(-input.Look.y, input.Look.x) * sensitivity;
        transform.eulerAngles = _eulerAngles;
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
}