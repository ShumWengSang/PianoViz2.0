using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
#if WINDOWS_UWP
using Windows.Media.Capture;
using Windows.System;
#endif
public class FollowCameraScript : MonoBehaviour
{
    [SerializeField, Range(0.0f, 100.0f), Tooltip("How quickly to interpolate the window towards its target position and rotation.")]
    private float windowFollowSpeed = 5.0f;
    private Transform cameraTransform;
    
#if WINDOWS_UWP
    private bool appCaptureIsCapturingVideo = false;
    private void AppCapture_CapturingChanged(AppCapture sender, object args) => appCaptureIsCapturingVideo = sender.IsCapturingVideo;
    private AppCapture appCapture;
    private float previousFieldOfView = -1.0f;
#endif // WINDOWS_UWP
    
    // Start is called before the first frame update
    void Start()
    {
        cameraTransform = Camera.main.transform;
        
#if WINDOWS_UWP
            appCapture = AppCapture.GetForCurrentView();
            if (appCapture != null)
            {
                appCaptureIsCapturingVideo = appCapture.IsCapturingVideo;
                appCapture.CapturingChanged += AppCapture_CapturingChanged;
            }
#endif // WINDOWS_UWP
    }

    // Update is called once per frame
    void LateUpdate()
    {
        float t = Time.deltaTime * windowFollowSpeed;
        transform.position = Vector3.Lerp(transform.position, CalculateWindowPosition(cameraTransform), t);
        transform.rotation = Quaternion.Slerp(transform.rotation, CalculateWindowRotation(cameraTransform), t);
    }

    Vector3 CalculateWindowPosition(Transform cameraTransform)
    {
        float windowDistance =
#if WINDOWS_UWP
            Mathf.Max(16.0f / (appCaptureIsCapturingVideo ? previousFieldOfView : previousFieldOfView = CameraCache.Main.fieldOfView), Mathf.Max(CameraCache.Main.nearClipPlane, 0.5f));
#else
            Mathf.Max(16.0f / CameraCache.Main.fieldOfView, Mathf.Max(CameraCache.Main.nearClipPlane, 0.5f));
#endif // WINDOWS_UWP
        return cameraTransform.position + (cameraTransform.forward * windowDistance);
    }
    
    Quaternion CalculateWindowRotation(Transform cameraTransform)
    {
        return cameraTransform.rotation;
    }
}
