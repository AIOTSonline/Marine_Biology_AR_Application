using UnityEngine;
using UnityEngine.Android;
using System.Collections;

public class PermissionManager : MonoBehaviour
{
    [SerializeField] private bool requestCameraOnStart = true;
    [SerializeField] private bool requestMicrophoneOnStart = true;

    private void Start()
    {
        StartCoroutine(RequestStartupPermissions());
    }

    private IEnumerator RequestStartupPermissions()
    {
#if UNITY_ANDROID
        if (requestCameraOnStart)
        {
            yield return RequestPermission(Permission.Camera);
        }

        if (requestMicrophoneOnStart)
        {
            yield return RequestPermission(Permission.Microphone);
        }
#elif UNITY_IOS
        if (requestCameraOnStart && !Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        }

        if (requestMicrophoneOnStart && !Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
        }
#else
        yield break;
#endif
    }

#if UNITY_ANDROID
    private static IEnumerator RequestPermission(string permission)
    {
        if (Permission.HasUserAuthorizedPermission(permission))
        {
            yield break;
        }

        bool callbackReceived = false;
        var callbacks = new PermissionCallbacks();
        callbacks.PermissionGranted += _ => callbackReceived = true;
        callbacks.PermissionDenied += _ => callbackReceived = true;
        callbacks.PermissionDeniedAndDontAskAgain += _ => callbackReceived = true;

        Permission.RequestUserPermission(permission, callbacks);
        yield return new WaitUntil(() => callbackReceived || Permission.HasUserAuthorizedPermission(permission));
    }
#endif

    public static bool HasCameraPermission()
    {
#if UNITY_ANDROID
        return Permission.HasUserAuthorizedPermission(Permission.Camera);
#elif UNITY_IOS
        return Application.HasUserAuthorization(UserAuthorization.WebCam);
#else
        return true;
#endif
    }

    public static IEnumerator RequestCameraPermission()
    {
#if UNITY_ANDROID
        yield return RequestPermission(Permission.Camera);
#elif UNITY_IOS
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        }
#else
        yield break;
#endif
    }

    public static bool HasMicrophonePermission()
    {
#if UNITY_ANDROID
        return Permission.HasUserAuthorizedPermission(Permission.Microphone);
#elif UNITY_IOS
        return Application.HasUserAuthorization(UserAuthorization.Microphone);
#else
        return true;
#endif
    }

    public static IEnumerator RequestMicrophonePermission()
    {
#if UNITY_ANDROID
        yield return RequestPermission(Permission.Microphone);
#elif UNITY_IOS
        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
        }
#else
        yield break;
#endif
    }
}
