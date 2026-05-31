using UnityEngine;
using System.Collections;

public class ARActivator : MonoBehaviour
{
    public GameObject arSession;
    public GameObject arSessionOrigin;
    public GameObject uiCanvas;
    public Camera uiCamera;

    public void ActivateAR()
    {
        StartCoroutine(ActivateARRoutine());
    }

    private IEnumerator ActivateARRoutine()
    {
        AppRuntimeUI.ShowStatus("Checking camera permission...", 0f);
        yield return PermissionManager.RequestCameraPermission();

        if (!PermissionManager.HasCameraPermission())
        {
            AppRuntimeUI.ShowStatus("Camera permission is required for AR.", 3f);
            Debug.LogWarning("Camera permission was denied. AR cannot start.");
            yield break;
        }

        if (arSession != null)
        {
            arSession.SetActive(true);
        }

        if (arSessionOrigin != null)
        {
            arSessionOrigin.SetActive(true);
        }

        if (uiCanvas != null)
        {
            uiCanvas.SetActive(false);
        }

        if (uiCamera != null)
        {
            uiCamera.gameObject.SetActive(false);
        }

        AppRuntimeUI.ShowStatus("Move your phone slowly to scan the surface.", 3f);
    }
}
