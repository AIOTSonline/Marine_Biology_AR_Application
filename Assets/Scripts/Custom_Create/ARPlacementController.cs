using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems;

public class ARPlacementController : MonoBehaviour
{
    [Header("AR References")]
    public ARRaycastManager raycastManager;
    public Camera arCamera;

    public ARPlaneManager planeManager;  // AR Plane Manager reference

    [Header("Environment Prefabs")]
    public ActorDatabase actorDatabase;

    [Header("UI")]
    public FloatingJoystick joystick;                  // Joystick logic reference
    public GameObject joystickUIRoot;                  // Joystick canvas root (assign in Inspector)

    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private bool placed = false;
    private static readonly TrackableType FastSurfaceMask =
        TrackableType.PlaneWithinPolygon |
        TrackableType.PlaneWithinBounds |
        TrackableType.PlaneEstimated |
        TrackableType.FeaturePoint;

    void Start()
    {
        if (joystickUIRoot != null)
            joystickUIRoot.SetActive(false);  // Hide joystick UI by default
    }

    void Update()
    {

        if (placed)
            return;

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceAt(Input.mousePosition);
        }
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began)
        {
            TryPlaceAt(Input.GetTouch(0).position);
        }
#endif
    }



    private void TryPlaceAt(Vector2 screenPosition)
    {
        if (IsPointerOverUI())
        {
            return;
        }

        if (raycastManager != null && raycastManager.Raycast(screenPosition, hits, FastSurfaceMask))
        {
            Pose pose = StabilizePlacementPose(PickBestHit(hits));
            if (PlaceSavedEnvironment(pose))
            {
                placed = true;
                AppRuntimeUI.ShowStatus("Module placed.", 2f);
            }
        }
        else
        {
            AppRuntimeUI.ShowStatus("Move slowly and tap on a brighter scanned surface.", 2.5f);
        }

        hits.Clear();
    }

    private bool PlaceSavedEnvironment(Pose pose)
    {
        string envKey = PlayerPrefs.GetString("SelectedEnvironmentKey");
        string json = PlayerPrefs.GetString(envKey);

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("No environment data found for key: " + envKey);
            AppRuntimeUI.ShowStatus("No saved module found. Create or select a module first.", 3f);
            return false;
        }

        EnvironmentData data = JsonUtility.FromJson<EnvironmentData>(json);
        if (data == null)
        {
            Debug.LogError("Failed to parse environment data.");
            AppRuntimeUI.ShowStatus("Could not load this saved module.", 3f);
            return false;
        }

        GameObject root = new GameObject(data.environmentName);
        root.transform.SetPositionAndRotation(pose.position, pose.rotation);

        if (planeManager != null)
        {
            planeManager.enabled = false; // Disable plane detection after placement
            
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(false); // Hide existing planes
            }
        }

        // Instantiate environment plane
        if (!string.IsNullOrEmpty(data.environmentPlanePrefabName))
        {
            GameObject planePrefab = actorDatabase.GetActorByName(data.environmentPlanePrefabName);
            if (planePrefab != null)
            {
                GameObject plane = Instantiate(planePrefab, root.transform);
                plane.transform.localPosition = Vector3.zero;
                plane.transform.localRotation = Quaternion.identity;
            }
        }

        bool mainPlayerFound = false;

        // Instantiate actors
        foreach (var actor in data.placedActors)
        {
            GameObject prefab = actorDatabase.GetActorByName(actor.prefabName);
            if (prefab != null)
            {
                GameObject go = Instantiate(prefab, root.transform);
                go.transform.localPosition = actor.localPosition;
                go.transform.localRotation = actor.localRotation;

                // Assign unique name and tag
                if (string.IsNullOrEmpty(actor.uniqueID))
                    actor.uniqueID = System.Guid.NewGuid().ToString();

                go.name = actor.uniqueID;
                go.tag = "Actor";

                // Attach ActorIdentity script and assign uniqueId
                ActorIdentity identity = go.AddComponent<ActorIdentity>();
                identity.uniqueId = actor.uniqueID;

                // Main player setup
                if (actor.isMainPlayer)
                {
                    var controller = go.AddComponent<MovementController>();
                    controller.joystick = joystick;
                    mainPlayerFound = true;
                    Debug.Log("Main player instantiated with movement.");
                }

                // Food Consumer behavior
                if (actor.addedScripts != null && actor.addedScripts.Contains("Food Consumption"))
                {
                    FoodConsumer foodConsumer = go.AddComponent<FoodConsumer>();
                    foodConsumer.foodTargetUniqueID = actor.foodTargetUniqueID;
                }
            }
        }

        if (joystickUIRoot != null)
            joystickUIRoot.SetActive(mainPlayerFound);

        Debug.Log("Environment placed successfully.");
        return true;
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

    private static ARRaycastHit PickBestHit(List<ARRaycastHit> hitResults)
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

    private static Pose StabilizePlacementPose(ARRaycastHit hit)
    {
        Pose pose = hit.pose;
        if ((hit.hitType & TrackableType.Planes) == 0)
        {
            pose.rotation = Quaternion.identity;
        }

        return pose;
    }
}
