using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class UIManager : MonoBehaviour
{
    private bool isLoadingScene;

    public void LoadARScene()
    {
        LoadSceneAfterCameraPermission("AR_Spawn");
    }

    public void LoadFreeExploreEcosystem()
    {
        if (isLoadingScene)
        {
            return;
        }

        isLoadingScene = true;
        StartCoroutine(LoadFreeExploreAfterPermissions());
    }

    public void LoadModuleWise()
    {
        LoadSceneAfterCameraPermission("ModuleWise");
    }

    public void LoadCustomCreateScene()
    {
        SceneManager.LoadScene("StartScreenScene");
    }

    public void LoadHumanInteractionScene()
    {
        LoadSceneAfterCameraPermission("Pose Landmark Detection");
    }

    private void LoadSceneAfterCameraPermission(string sceneName)
    {
        if (isLoadingScene)
        {
            return;
        }

        isLoadingScene = true;
        StartCoroutine(LoadSceneAfterCameraPermissionRoutine(sceneName));
    }

    private IEnumerator LoadSceneAfterCameraPermissionRoutine(string sceneName)
    {
        AppRuntimeUI.ShowStatus("Checking camera permission...", 0f);
        yield return PermissionManager.RequestCameraPermission();

        if (!PermissionManager.HasCameraPermission())
        {
            AppRuntimeUI.ShowStatus("Camera permission is required for this mode.", 3f);
            Debug.LogWarning($"Camera permission was denied. Cannot open {sceneName}.");
            isLoadingScene = false;
            yield break;
        }

        AppRuntimeUI.ShowStatus("Opening camera...", 2f);
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator LoadFreeExploreAfterPermissions()
    {
        AppRuntimeUI.ShowStatus("Checking camera and microphone permissions...", 0f);
        yield return PermissionManager.RequestCameraPermission();
        yield return PermissionManager.RequestMicrophonePermission();

        if (!PermissionManager.HasCameraPermission())
        {
            AppRuntimeUI.ShowStatus("Camera permission is required for Free Explore.", 3f);
            Debug.LogWarning("Camera permission was denied. Cannot open FreeExplore.");
            isLoadingScene = false;
            yield break;
        }

        AppRuntimeUI.ShowStatus("Opening Free Explore...", 2f);
        SceneManager.LoadScene("FreeExplore");
    }
}
