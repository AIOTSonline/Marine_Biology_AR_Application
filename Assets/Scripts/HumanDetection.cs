using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;

public class HumanDetectionUI : MonoBehaviour
{
    [Header("AR Human Detection")]
    public ARHumanBodyManager humanBodyManager;

    [Header("UI Elements")]
    public Text statusText;

    private bool wasDetected = false;

    void Start()
    {
        if (statusText != null)
        {
            statusText.text = "Looking for a person";
            statusText.color = new Color(1f, 0.72f, 0.24f);
        }
    }

    void Update()
    {
        if (humanBodyManager == null || statusText == null)
            return;

        // Check if any human bodies are currently tracked
        bool isDetected = false;
        foreach (var body in humanBodyManager.trackables)
        {
            if (body.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
            {
                isDetected = true;
                break;
            }
        }

        if (isDetected != wasDetected)
        {
            wasDetected = isDetected;

            statusText.text = isDetected ? "Person detected" : "Looking for a person";
            statusText.color = isDetected ? new Color(0.25f, 1f, 0.45f) : new Color(1f, 0.72f, 0.24f);
        }
    }
}
