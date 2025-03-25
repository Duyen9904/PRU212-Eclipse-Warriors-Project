using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("Cinemachine References")]
    public CinemachineVirtualCamera virtualCamera;
    public CinemachineConfiner2D confiner;

    [Header("Camera Settings")]
    public float defaultZoom = 5f;
    public float minZoom = 3f;
    public float maxZoom = 8f;
    public float zoomSpeed = 1f;

    [Header("Camera Effects")]
    public float shakeIntensity = 1f;
    public float shakeDuration = 0.2f;

    private float currentZoom;
    private CinemachineBasicMultiChannelPerlin virtualCameraNoise;

    private void Awake()
    {
        if (virtualCamera != null)
        {
            virtualCameraNoise = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            currentZoom = defaultZoom;
            SetZoom(currentZoom);
        }
    }

    private void Update()
    {
        // Example input for zoom (can be modified based on your input system)
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (scrollInput != 0)
        {
            // Adjust zoom based on input
            AdjustZoom(scrollInput * zoomSpeed);
        }
    }

    // Set camera zoom level
    public void SetZoom(float zoomLevel)
    {
        currentZoom = Mathf.Clamp(zoomLevel, minZoom, maxZoom);

        if (virtualCamera != null)
        {
            virtualCamera.m_Lens.OrthographicSize = currentZoom;
        }
    }

    // Adjust zoom by a relative amount
    public void AdjustZoom(float zoomChange)
    {
        SetZoom(currentZoom - zoomChange); // Negative because scrolling up should zoom in (reduce size)
    }

    // Trigger a camera shake effect
    public IEnumerator ShakeCamera(float intensity = -1, float duration = -1)
    {
        float useIntensity = intensity > 0 ? intensity : shakeIntensity;
        float useDuration = duration > 0 ? duration : shakeDuration;

        if (virtualCameraNoise != null)
        {
            virtualCameraNoise.AmplitudeGain = useIntensity; // Corrected property name
            yield return new WaitForSeconds(useDuration);
            virtualCameraNoise.AmplitudeGain = 0f; // Corrected property name
        }
    }

    // Set camera target (for changing the follow target)
    public void SetCameraTarget(Transform target)
    {
        if (virtualCamera != null)
        {
            virtualCamera.Follow = target;
        }
    }

    // Set or update the camera bounds (for the confiner)
    public void UpdateCameraBounds(Collider2D bounds)
    {
        if (confiner != null)
        {
            confiner.BoundingShape2D = bounds;
            confiner.InvalidateCache();
        }
    }
}