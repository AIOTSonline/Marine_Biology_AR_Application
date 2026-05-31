using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class ARSceneNavigator : MonoBehaviour
{
    public void GoToARPlacementScene()
    {
        StartCoroutine(GoToARPlacementSceneRoutine());
    }

    private IEnumerator GoToARPlacementSceneRoutine()
    {
        AppRuntimeUI.ShowStatus("Checking camera permission...", 0f);
        yield return PermissionManager.RequestCameraPermission();

        if (!PermissionManager.HasCameraPermission())
        {
            AppRuntimeUI.ShowStatus("Camera permission is required for AR placement.", 3f);
            Debug.LogWarning("Camera permission was denied. Cannot open AR placement.");
            yield break;
        }

        AppRuntimeUI.ShowStatus("Opening AR placement...", 2f);
        SceneManager.LoadScene("ARPlacementScene");
    }
}
