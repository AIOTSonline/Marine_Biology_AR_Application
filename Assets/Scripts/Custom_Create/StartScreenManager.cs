using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections;

public class StartScreenManager : MonoBehaviour
{
    public Transform moduleGridContainer;       // Assign in Inspector (Scroll View Content)
    public GameObject moduleButtonPrefab;       // Assign prefab with icon + name + button
    public Button createModuleButton;           // Assign "+" button in Inspector
    public Button scanQRButton;                 // Assign "Scan QR to Import" button in Inspector

    void Start()
    {
        Debug.Log("Saved Modules Folder: " + Application.persistentDataPath);

        // Create new module
        if (createModuleButton != null)
        {
            createModuleButton.onClick.AddListener(() =>
            {
                SceneManager.LoadScene("CreateProjectScene");
            });
        }

        // Scan QR to Import
        if (scanQRButton != null)
        {
            scanQRButton.onClick.AddListener(() =>
            {
                StartCoroutine(OpenQRScannerScene());
            });
        }

        // Load saved modules
        string[] modulePaths = ModuleSaveManager.GetAllModulePaths();

        foreach (string path in modulePaths)
        {
            string json = ModuleSaveManager.LoadModule(path);
            EnvironmentData data = JsonUtility.FromJson<EnvironmentData>(json);
            string moduleName = data.environmentName;

            GameObject btn = Instantiate(moduleButtonPrefab, moduleGridContainer);
            Text nameText = btn.transform.Find("ModuleNameText").GetComponent<Text>();
            Button loadButton = btn.GetComponent<Button>();

            nameText.text = moduleName;

            loadButton.onClick.AddListener(() =>
            {
                PlayerPrefs.SetString("LastSavedEnvironment", moduleName);
                PlayerPrefs.SetString("SelectedModulePath", path);
                PlayerPrefs.Save();
                SceneManager.LoadScene("PlacedActorListScene");
            });
        }
    }

    private IEnumerator OpenQRScannerScene()
    {
        AppRuntimeUI.ShowStatus("Checking camera permission...", 0f);
        yield return PermissionManager.RequestCameraPermission();

        if (!PermissionManager.HasCameraPermission())
        {
            AppRuntimeUI.ShowStatus("Camera permission is required to scan QR codes.", 3f);
            Debug.LogWarning("Camera permission was denied. Cannot open QR scanner.");
            yield break;
        }

        AppRuntimeUI.ShowStatus("Opening QR scanner...", 2f);
        SceneManager.LoadScene("QRScannerScene");
    }
}
