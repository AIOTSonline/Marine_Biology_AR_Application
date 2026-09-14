using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.Events;

public class AppleUIAutomator : Editor
{
    [MenuItem("Tools/Automate Apple UI")]
    public static void AutomateUI()
    {
        AuthManager authManager = FindObjectOfType<AuthManager>(true);
        if (authManager == null)
        {
            Debug.LogError("Could not find AuthManager in the scene! Please open AuthScene first.");
            return;
        }

        // Handle SignIn Button
        GameObject signInGoogle = GameObject.Find("SignInwithGoogleBtn");
        if (signInGoogle != null)
        {
            CreateAppleButton(signInGoogle, "SignInwithAppleBtn", "Sign in With Apple", authManager);
        }
        else
        {
            Debug.LogWarning("Could not find SignInwithGoogleBtn. Skipping...");
        }

        // Handle SignUp Button
        GameObject signUpGoogle = GameObject.Find("SignUpwithGoogleBtn");
        if (signUpGoogle != null)
        {
            CreateAppleButton(signUpGoogle, "SignUpwithAppleBtn", "Sign up With Apple", authManager);
        }
        else
        {
            Debug.LogWarning("Could not find SignUpwithGoogleBtn. Skipping...");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("Apple UI Automation Complete!");
    }

    private static void CreateAppleButton(GameObject original, string newName, string newText, AuthManager authManager)
    {
        GameObject appleObj = Instantiate(original, original.transform.parent);
        appleObj.name = newName;

        RectTransform rect = appleObj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y - 70f);
        }

        Image img = appleObj.GetComponent<Image>();
        if (img != null)
        {
            img.color = Color.black;
        }

        TextMeshProUGUI tmp = appleObj.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = newText;
            tmp.color = Color.white;
        }

        Button btn = appleObj.GetComponent<Button>();
        if (btn != null)
        {
            while (btn.onClick.GetPersistentEventCount() > 0)
            {
                UnityEventTools.RemovePersistentListener(btn.onClick, 0);
            }
            UnityEventTools.AddVoidPersistentListener(btn.onClick, new UnityEngine.Events.UnityAction(authManager.AppleSignInUser));
        }
        
        Selection.activeGameObject = appleObj;
        EditorGUIUtility.PingObject(appleObj);
    }
}
