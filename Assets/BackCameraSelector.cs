using Mediapipe.Unity;
using Mediapipe.Unity.Sample;
using System.Collections;
using UnityEngine;

public class BackCameraSelector : MonoBehaviour
{
    IEnumerator Start()
    {
        AppRuntimeUI.ShowStatus("Preparing human interaction camera...", 0f);
        yield return PermissionManager.RequestCameraPermission();

        if (!PermissionManager.HasCameraPermission())
        {
            AppRuntimeUI.ShowStatus("Camera permission is required for Human Interaction.", 3f);
            Debug.LogWarning("Camera permission was denied. MediaPipe camera source cannot start.");
            yield break;
        }

        // Wait until Bootstrap created the ImageSource
        AppRuntimeUI.ShowStatus("Loading human interaction model...", 0f);
        yield return new WaitUntil(() => ImageSourceProvider.ImageSource != null);
        var src = ImageSourceProvider.ImageSource;

        // Pick the first non-front-facing device (fallback index 0)
        var devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            AppRuntimeUI.ShowStatus("No camera was found on this device.", 3f);
            Debug.LogWarning("No camera devices were found.");
            yield break;
        }

        int backIdx = -1;
        for (int i = 0; i < devices.Length; i++)
            if (!devices[i].isFrontFacing) { backIdx = i; break; }
        if (backIdx < 0) backIdx = 0;

        // If already running, stop it first
        if (src.isPrepared)
        {
            src.Stop();  // no yield, just call it
        }

        // Select the back camera and play
        src.SelectSource(backIdx);
        yield return src.Play();  // Play() does return IEnumerator
        AppRuntimeUI.ShowStatus("Stand in view of the camera for detection.", 4f);
    }
}
