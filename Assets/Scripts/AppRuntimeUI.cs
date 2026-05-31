using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public sealed class AppRuntimeUI : MonoBehaviour
{
    private static AppRuntimeUI instance;
    private static readonly Dictionary<string, string> BackTargets = new Dictionary<string, string>
    {
        { "AR_Spawn", "StartScene" },
        { "FreeExplore", "StartScene" },
        { "ModuleWise", "StartScene" },
        { "Pose Landmark Detection", "StartScene" },
        { "StartScreenScene", "StartScene" },
        { "QRScannerScene", "StartScreenScene" },
        { "CreateProjectScene", "StartScreenScene" },
        { "PlacedActorListScene", "StartScreenScene" },
        { "BehaviorScene", "PlacedActorListScene" },
        { "AddScriptsScene", "BehaviorScene" },
        { "ARPlacementScene", "PlacedActorListScene" },
        { "TestScene2", "CreateProjectScene" }
    };

    private Canvas canvas;
    private GameObject backButtonRoot;
    private GameObject statusRoot;
    private TextMeshProUGUI statusText;
    private string currentSceneName;
    private float statusUntilTime;
    private float nextScanStatusCheckTime;
    private readonly List<ARRaycastHit> scanHits = new List<ARRaycastHit>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
        SceneManager.sceneLoaded += (_, _) => EnsureInstance().ConfigureForScene();
    }

    public static void ShowStatus(string message, float seconds = 3f)
    {
        var ui = EnsureInstance();
        ui.SetStatus(message, seconds);
    }

    public static void HideStatus()
    {
        if (instance != null)
        {
            instance.statusRoot.SetActive(false);
        }
    }

    private static AppRuntimeUI EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        var root = new GameObject(nameof(AppRuntimeUI));
        DontDestroyOnLoad(root);
        instance = root.AddComponent<AppRuntimeUI>();
        instance.BuildUI();
        instance.ConfigureForScene();
        return instance;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GoBack();
        }

        if (statusRoot.activeSelf && statusUntilTime > 0f && Time.unscaledTime >= statusUntilTime)
        {
            statusRoot.SetActive(false);
        }

        UpdateARScanStatus();
    }

    private void BuildUI()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem");
            DontDestroyOnLoad(eventSystem);
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        var canvasObject = new GameObject("App Runtime Canvas");
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();

        backButtonRoot = CreateBackButton(canvasObject.transform);
        statusRoot = CreateStatusPanel(canvasObject.transform);
    }

    private GameObject CreateBackButton(Transform parent)
    {
        var root = new GameObject("Back Button");
        root.transform.SetParent(parent, false);
        var rect = root.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(18f, -18f);
        rect.sizeDelta = new Vector2(64f, 64f);

        var image = root.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.62f);

        var button = root.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(GoBack);

        var label = new GameObject("Label");
        label.transform.SetParent(root.transform, false);
        var labelRect = label.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var text = label.AddComponent<TextMeshProUGUI>();
        text.text = "<";
        text.fontSize = 38f;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        return root;
    }

    private GameObject CreateStatusPanel(Transform parent)
    {
        var root = new GameObject("Status Panel");
        root.transform.SetParent(parent, false);
        var rect = root.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 40f);
        rect.sizeDelta = new Vector2(720f, 78f);

        var image = root.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.62f);

        var label = new GameObject("Status Text");
        label.transform.SetParent(root.transform, false);
        var labelRect = label.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(24f, 8f);
        labelRect.offsetMax = new Vector2(-24f, -8f);

        statusText = label.AddComponent<TextMeshProUGUI>();
        statusText.fontSize = 24f;
        statusText.color = Color.white;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.enableWordWrapping = true;
        statusText.raycastTarget = false;

        root.SetActive(false);
        return root;
    }

    private void ConfigureForScene()
    {
        currentSceneName = SceneManager.GetActiveScene().name;
        backButtonRoot.SetActive(currentSceneName != "StartScene" && !SceneHasVisibleBackButton());

        if (IsCameraScene(currentSceneName))
        {
            SetStatus("Opening camera...", 2.5f);
        }
        else
        {
            statusRoot.SetActive(false);
        }
    }

    private void SetStatus(string message, float seconds)
    {
        statusText.text = message;
        statusUntilTime = seconds > 0f ? Time.unscaledTime + seconds : 0f;
        statusRoot.SetActive(true);
    }

    private void UpdateARScanStatus()
    {
        if (!IsARScanScene(currentSceneName) || statusRoot.activeSelf)
        {
            return;
        }

        if (Time.unscaledTime < nextScanStatusCheckTime)
        {
            return;
        }

        nextScanStatusCheckTime = Time.unscaledTime + 1f;

        foreach (var planeManager in FindObjectsByType<ARPlaneManager>(FindObjectsSortMode.None))
        {
            if (planeManager.enabled && planeManager.trackables.count > 0)
            {
                return;
            }
        }

        foreach (var raycastManager in FindObjectsByType<ARRaycastManager>(FindObjectsSortMode.None))
        {
            var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (raycastManager.enabled && raycastManager.Raycast(center, scanHits, TrackableType.Planes | TrackableType.FeaturePoint))
            {
                scanHits.Clear();
                return;
            }

            scanHits.Clear();
        }

        SetStatus("Move your phone slowly to scan the surface.", 2f);
    }

    private void GoBack()
    {
        if (TryCloseTopOverlay())
        {
            return;
        }

        StopARSessionIfPresent();

        if (BackTargets.TryGetValue(currentSceneName, out var targetScene))
        {
            SceneManager.LoadScene(targetScene);
            return;
        }

        if (currentSceneName != "StartScene")
        {
            SceneManager.LoadScene("StartScene");
        }
    }

    private static bool TryCloseTopOverlay()
    {
        foreach (var canvasTag in FindObjectsByType<UICanvasTag>(FindObjectsSortMode.None))
        {
            if (canvasTag.gameObject.activeSelf)
            {
                canvasTag.gameObject.SetActive(false);
                return true;
            }
        }

        return false;
    }

    private static bool SceneHasVisibleBackButton()
    {
        foreach (var button in FindObjectsByType<Button>(FindObjectsSortMode.None))
        {
            if (!button.isActiveAndEnabled)
            {
                continue;
            }

            Transform current = button.transform;
            while (current != null)
            {
                if (current.name.ToLowerInvariant().Contains("back"))
                {
                    return true;
                }

                current = current.parent;
            }
        }

        return false;
    }

    private static void StopARSessionIfPresent()
    {
        foreach (var arSession in FindObjectsByType<ARSession>(FindObjectsSortMode.None))
        {
            arSession.Reset();
        }
    }

    private static bool IsCameraScene(string sceneName)
    {
        return sceneName == "AR_Spawn" ||
            sceneName == "FreeExplore" ||
            sceneName == "ModuleWise" ||
            sceneName == "ARPlacementScene" ||
            sceneName == "QRScannerScene" ||
            sceneName == "Pose Landmark Detection";
    }

    private static bool IsARScanScene(string sceneName)
    {
        return sceneName == "AR_Spawn" ||
            sceneName == "FreeExplore" ||
            sceneName == "ModuleWise" ||
            sceneName == "ARPlacementScene";
    }
}
