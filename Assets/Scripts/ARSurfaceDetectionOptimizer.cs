using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public sealed class ARSurfaceDetectionOptimizer : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallForCurrentScene()
    {
        SceneManager.sceneLoaded += (_, _) => CreateRunner();
        CreateRunner();
    }

    private static void CreateRunner()
    {
        if (FindFirstObjectByType<ARSurfaceDetectionOptimizer>() != null)
        {
            return;
        }

        var runner = new GameObject(nameof(ARSurfaceDetectionOptimizer));
        runner.hideFlags = HideFlags.HideAndDontSave;
        runner.AddComponent<ARSurfaceDetectionOptimizer>();
    }

    private IEnumerator Start()
    {
        for (int i = 0; i < 30; i++)
        {
            Apply();
            yield return new WaitForSeconds(0.25f);
        }

        Destroy(gameObject);
    }

    private static void Apply()
    {
        foreach (var planeManager in FindObjectsByType<ARPlaneManager>(FindObjectsSortMode.None))
        {
            if (planeManager.enabled)
            {
                planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal | PlaneDetectionMode.Vertical;
            }
        }
    }
}
