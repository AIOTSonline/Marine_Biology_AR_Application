using UnityEngine;
using System.Collections;

public class ModuleSelector : MonoBehaviour
{
    public GameObject arSessionOrigin;
    public GameObject arSession;
    public GameObject uiCanvas;
    public Camera arCamera;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (arSessionOrigin != null)
        {
            arSessionOrigin.SetActive(false);
        }
        
        if (arSession != null)
        {
            arSession.SetActive(false);
        }

        if (uiCanvas != null)
        {
            uiCanvas.SetActive(true);
        }
        
    }

    public void EnableAR()
    {
        StartCoroutine(EnableARRoutine());
    }

    private IEnumerator EnableARRoutine()
    {
        AppRuntimeUI.ShowStatus("Checking camera permission...", 0f);
        yield return PermissionManager.RequestCameraPermission();

        if (!PermissionManager.HasCameraPermission())
        {
            AppRuntimeUI.ShowStatus("Camera permission is required for this module.", 3f);
            Debug.LogWarning("Camera permission was denied. Module AR view cannot start.");
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

        // Hide UI Canvas
        if (uiCanvas != null)
        {
            uiCanvas.SetActive(false);
        }

        if (arCamera != null)
        {
            arCamera.enabled = true;
        }

        AppRuntimeUI.ShowStatus("Move your phone slowly to scan the surface.", 3f);
    }
}
