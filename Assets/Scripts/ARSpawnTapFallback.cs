using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public sealed class ARSpawnTapFallback : MonoBehaviour
{
    private static readonly TrackableType PlacementMask =
        TrackableType.PlaneWithinPolygon |
        TrackableType.PlaneWithinBounds |
        TrackableType.PlaneEstimated |
        TrackableType.FeaturePoint;

    private readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
    private ARRaycastManager raycastManager;
    private ARPlaneManager planeManager;
    private ObjectSpawner objectSpawner;
    private float nextPlacementTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded += (_, _) => EnsureForScene();
        EnsureForScene();
    }

    private static void EnsureForScene()
    {
        if (SceneManager.GetActiveScene().name != "AR_Spawn")
        {
            return;
        }

        if (FindFirstObjectByType<ARSpawnTapFallback>() != null)
        {
            return;
        }

        var host = new GameObject(nameof(ARSpawnTapFallback));
        host.AddComponent<ARSpawnTapFallback>();
    }

    private void Awake()
    {
        raycastManager = FindFirstObjectByType<ARRaycastManager>();
        planeManager = FindFirstObjectByType<ARPlaneManager>();
        objectSpawner = FindFirstObjectByType<ObjectSpawner>();
        DisableSampleSpawnTriggers();
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != "AR_Spawn")
        {
            Destroy(gameObject);
            return;
        }

        if (Time.unscaledTime < nextPlacementTime || !TryGetTapPosition(out var screenPosition))
        {
            return;
        }

        nextPlacementTime = Time.unscaledTime + 0.25f;

        if (IsBlockedByRealUI(screenPosition))
        {
            return;
        }

        if (raycastManager == null)
        {
            raycastManager = FindFirstObjectByType<ARRaycastManager>();
        }

        if (objectSpawner == null)
        {
            objectSpawner = FindFirstObjectByType<ObjectSpawner>();
        }

        if (raycastManager == null || objectSpawner == null)
        {
            AppRuntimeUI.ShowStatus("AR spawner is still loading.", 2f);
            return;
        }

        if (!raycastManager.Raycast(screenPosition, hits, PlacementMask))
        {
            AppRuntimeUI.ShowStatus("Move slowly and tap on a brighter scanned surface.", 2.5f);
            return;
        }

        var hit = PickBestHit(hits);
        var normal = GetSurfaceNormal(hit);
        if (objectSpawner.TrySpawnObject(hit.pose.position, normal))
        {
            AppRuntimeUI.ShowStatus("Placed.", 1.5f);
        }
        else
        {
            AppRuntimeUI.ShowStatus("Tap a visible scanned surface in front of you.", 2.5f);
        }

        hits.Clear();
    }

    private static void DisableSampleSpawnTriggers()
    {
        foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (behaviour != null && behaviour.GetType().Name == "ARInteractorSpawnTrigger")
            {
                behaviour.enabled = false;
            }
        }
    }

    private static bool TryGetTapPosition(out Vector2 position)
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            position = Input.GetTouch(0).position;
            return true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            position = Input.mousePosition;
            return true;
        }

        position = default;
        return false;
    }

    private bool IsBlockedByRealUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        uiHits.Clear();
        var pointer = new PointerEventData(EventSystem.current) { position = screenPosition };
        EventSystem.current.RaycastAll(pointer, uiHits);

        foreach (var result in uiHits)
        {
            if (result.gameObject == null)
            {
                continue;
            }

            if (result.gameObject.GetComponentInParent<Selectable>() != null)
            {
                return true;
            }

            var graphic = result.gameObject.GetComponent<Graphic>();
            if (graphic != null && graphic.raycastTarget && graphic.color.a > 0.1f)
            {
                return true;
            }
        }

        return false;
    }

    private ARRaycastHit PickBestHit(List<ARRaycastHit> hitResults)
    {
        foreach (var hit in hitResults)
        {
            if (hit.hitType == TrackableType.PlaneWithinPolygon)
            {
                return hit;
            }
        }

        foreach (var hit in hitResults)
        {
            if (hit.hitType == TrackableType.PlaneWithinBounds || hit.hitType == TrackableType.PlaneEstimated)
            {
                return hit;
            }
        }

        return hitResults[0];
    }

    private Vector3 GetSurfaceNormal(ARRaycastHit hit)
    {
        if (planeManager != null && (hit.hitType & TrackableType.Planes) != 0)
        {
            var plane = planeManager.GetPlane(hit.trackableId);
            if (plane != null)
            {
                return plane.normal;
            }
        }

        return Vector3.up;
    }
}
