using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class WaterPlaneSpawner1 : MonoBehaviour
{
    [SerializeField] private GameObject waterPlanePrefab;
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Transform fixedWorldContainer;
    [SerializeField] private FreeExpGoalManager goalManager;
    [SerializeField] private CrossPlatformTTS globalTTS;

    private ARRaycastManager _arRaycastManager;
    private ARPlaneManager _arPlaneManager;
    private ARAnchorManager _arAnchorManager;

    private WaterPlaneMover _moverInstance;
    private InputAction _touchAction;
    private bool _planePlaced;
    private bool _scanGoalTriggered = false;
    private readonly List<ARRaycastHit> _hits = new List<ARRaycastHit>();
    private static readonly TrackableType PlacementSurfaceMask =
        TrackableType.PlaneWithinPolygon |
        TrackableType.PlaneWithinBounds |
        TrackableType.PlaneEstimated |
        TrackableType.FeaturePoint;

    private void Awake()
    {
        _arRaycastManager = GetComponent<ARRaycastManager>();
        _arPlaneManager = GetComponent<ARPlaneManager>();
        _arAnchorManager = GetComponent<ARAnchorManager>();

        if (_arRaycastManager == null || _arPlaneManager == null)
        {
            Debug.LogError("Missing required AR component (ARRaycastManager or ARPlaneManager).");
            enabled = false; // Disable script to prevent errors
            return;
        }

        _touchAction = inputActions?.FindAction("Touch");
        if (_touchAction == null)
        {
            Debug.LogWarning("Touch action not found in InputActionAsset.");
        }
    }

    private void OnEnable()
    {
        _touchAction?.Enable();
    }

    private void OnDisable()
    {
        _touchAction?.Disable();
    }

    void Update()
    {
        if (_planePlaced)
            return;

        // Handle Scan goal completion only if goalManager exists
        if (!_scanGoalTriggered && goalManager != null && goalManager.GetCurrentGoalType() == FreeExpGoalManager.FreeExplorerGoals.Scan)
        {
            if (_arPlaneManager.trackables.count > 0)
            {
                _scanGoalTriggered = true;
                goalManager.CompleteCurrentGoal();
            }
        }

        if (TryGetTapPosition(out Vector2 touchPosition))
        {
            if (IsPointerOverUI())
            {
                return;
            }

            if (_arRaycastManager.Raycast(touchPosition, _hits, PlacementSurfaceMask))
            {
                var hit = PickBestHit(_hits);
                Pose hitPose = StabilizePlacementPose(hit);
                ARPlane hitPlane = IsPlaneHit(hit.hitType) ? _arPlaneManager.GetPlane(hit.trackableId) : null;
                Transform placementParent = fixedWorldContainer;

                if (hitPlane != null && _arAnchorManager != null)
                {
                    ARAnchor anchor = _arAnchorManager.AttachAnchor(hitPlane, hitPose);
                    if (anchor != null && placementParent == null)
                    {
                        placementParent = anchor.transform;
                    }
                }

                if (waterPlanePrefab == null)
                {
                    Debug.LogError("Water plane prefab is not assigned.");
                    AppRuntimeUI.ShowStatus("Placement object is missing. Please reopen this module.", 3f);
                    _hits.Clear();
                    return;
                }

                GameObject spawnedPlane = Instantiate(waterPlanePrefab, hitPose.position, hitPose.rotation, placementParent);
                _moverInstance = spawnedPlane.GetComponent<WaterPlaneMover>();
                AppRuntimeUI.ShowStatus("Placed. You can now explore.", 2f);

                MarineBuddy buddy = spawnedPlane.GetComponentInChildren<MarineBuddy>(true);
                if (buddy != null)
                {
                    if (goalManager != null)
                    {
                        goalManager.SetMarineBuddy(buddy);
                    }
                    if (globalTTS != null)
                    {
                        buddy.SetTTSManager(globalTTS);
                    }
                }

                _planePlaced = true;

                _arPlaneManager.enabled = false;
                foreach (var plane in _arPlaneManager.trackables)
                    plane.gameObject.SetActive(false);

                if (goalManager != null && goalManager.GetCurrentGoalType() == FreeExpGoalManager.FreeExplorerGoals.Spawn)
                {
                    goalManager.CompleteCurrentGoal();
                }
            }
            else
            {
                AppRuntimeUI.ShowStatus("Move slowly and tap on a brighter scanned surface.", 2.5f);
            }

            _hits.Clear();
        }
    }

    private bool TryGetTapPosition(out Vector2 position)
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began)
        {
            position = Input.GetTouch(0).position;
            return true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            position = Input.mousePosition;
            return true;
        }

        if (_touchAction != null && _touchAction.WasPerformedThisFrame())
        {
            position = _touchAction.ReadValue<Vector2>();
            return position.x > 1f || position.y > 1f;
        }

        position = default;
        return false;
    }

    private static bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        if (Input.touchCount > 0)
        {
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        }

        return EventSystem.current.IsPointerOverGameObject();
    }

    private static ARRaycastHit PickBestHit(List<ARRaycastHit> hits)
    {
        foreach (var hit in hits)
        {
            if (hit.hitType == TrackableType.PlaneWithinPolygon)
            {
                return hit;
            }
        }

        foreach (var hit in hits)
        {
            if (hit.hitType == TrackableType.PlaneWithinBounds || hit.hitType == TrackableType.PlaneEstimated)
            {
                return hit;
            }
        }

        return hits[0];
    }

    private static Pose StabilizePlacementPose(ARRaycastHit hit)
    {
        Pose pose = hit.pose;
        if (!IsPlaneHit(hit.hitType))
        {
            pose.rotation = Quaternion.identity;
        }

        return pose;
    }

    private static bool IsPlaneHit(TrackableType hitType)
    {
        return (hitType & TrackableType.Planes) != 0;
    }
}
