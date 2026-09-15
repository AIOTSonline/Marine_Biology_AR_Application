using UnityEngine;
using UnityEngine.UI;
using ZXing;
using ZXing.QrCode;
using System;
using System.Collections;
using UnityEngine.SceneManagement;
using System.IO;

public class QRCodeScanner : MonoBehaviour
{
    [Header("UI References")]
    public RawImage cameraFeedImage;
    public Text feedbackText;

    [Header("Scan Settings")]
    public float scanInterval = 0.5f;

    private WebCamTexture webcamTexture;
    private BarcodeReader reader;
    private bool isScanning = true;
    private bool isAdjusted = false;
    private bool hasDecoded = false;

    void Start()
    {
        Debug.Log("[QR] Start called");

        reader = new BarcodeReader();
        reader.AutoRotate = true;
        reader.TryInverted = true;
        reader.Options.PossibleFormats = new System.Collections.Generic.List<BarcodeFormat>
        {
            BarcodeFormat.QR_CODE
        };

        webcamTexture = new WebCamTexture();
        cameraFeedImage.texture = webcamTexture;
        cameraFeedImage.material.mainTexture = webcamTexture;
        webcamTexture.Play();

        SetFeedback("Initializing camera...");
        StartCoroutine(ScanLoop());
    }

    void Update()
    {
        if (!isAdjusted && webcamTexture != null
            && webcamTexture.isPlaying && webcamTexture.width >= 100)
        {
            AdjustCameraFeedAspect();
            ApplyRotationCorrection();
            isAdjusted = true;
            SetFeedback("Ready — point at QR code");
            Debug.Log($"[QR] Camera ready: {webcamTexture.width}x{webcamTexture.height}" +
                      $" rotation={webcamTexture.videoRotationAngle}");
        }
    }

    IEnumerator ScanLoop()
    {
        // Wait for webcam
        Debug.Log("[QR] Waiting for webcam...");
        while (webcamTexture == null || !webcamTexture.isPlaying || webcamTexture.width < 100)
            yield return null;

        Debug.Log("[QR] Webcam ready, starting decode loop");
        int attempt = 0;

        while (isScanning && !hasDecoded)
        {
            yield return new WaitForSeconds(scanInterval);
            attempt++;
            Debug.Log($"[QR] Decode attempt #{attempt}");
            TryDecode();
        }
    }

    void TryDecode()
    {
        if (webcamTexture == null || !webcamTexture.isPlaying) return;

        try
        {
            Color32[] pixels = webcamTexture.GetPixels32();
            int w = webcamTexture.width;
            int h = webcamTexture.height;

            Debug.Log($"[QR] Decoding {w}x{h} ({pixels.Length} pixels)");

        
            Color32[] flipped = FlipVertical(pixels, w, h);

            // Swap dims if sensor is rotated 90/270
            float angle = webcamTexture.videoRotationAngle;
            bool swap = Mathf.Abs(angle) == 90 || Mathf.Abs(angle) == 270;
            int dw = swap ? h : w;
            int dh = swap ? w : h;

            var result = reader.Decode(flipped, dw, dh);

            if (result != null)
            {
                Debug.Log($"[QR] DECODED! Format={result.BarcodeFormat}" +
                          $" Text length={result.Text.Length}" +
                          $" Text preview={result.Text.Substring(0, Mathf.Min(100, result.Text.Length))}");
                hasDecoded = true;
                isScanning = false;
                OnQRDecoded(result.Text);
            }
            else
            {
                Debug.Log("[QR] Decode returned null — no QR found in frame");
                SetFeedback("Scanning... hold steady");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[QR] Exception during decode: {ex.GetType().Name}: {ex.Message}");
        }
    }

    void OnQRDecoded(string raw)
    {
        Debug.Log($"[QR] OnQRDecoded called, raw length={raw.Length}");
        SetFeedback("QR found! Parsing...");

        // Step 1: parse JSON
        EnvironmentData importedData = null;
        try
        {
            importedData = JsonUtility.FromJson<EnvironmentData>(raw);
            Debug.Log($"[QR] JSON parsed. environmentName='{importedData?.environmentName}'");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[QR] JSON parse exception: {ex.Message}");
            SetFeedback("Error: not valid JSON");
            isScanning = true;
            hasDecoded = false;
            return;
        }

        if (importedData == null)
        {
            Debug.LogError("[QR] importedData is null after parse");
            SetFeedback("Error: parse returned null");
            isScanning = true;
            hasDecoded = false;
            return;
        }

        if (string.IsNullOrEmpty(importedData.environmentName))
        {
            Debug.LogError("[QR] environmentName is empty — QR may not encode EnvironmentData");
            SetFeedback("Error: missing environment name");
            isScanning = true;
            hasDecoded = false;
            return;
        }

        // Step 2: save to disk
        string moduleName = importedData.environmentName;
        string jsonPath = ModuleSaveManager.GetModulePath(moduleName);
        string prettyJson = JsonUtility.ToJson(importedData, true);

        Debug.Log($"[QR] Saving to: {jsonPath}");
        try
        {
            File.WriteAllText(jsonPath, prettyJson);
            Debug.Log($"[QR] File saved successfully");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[QR] File write failed: {ex.Message}");
            SetFeedback("Error: could not save file");
            return;
        }

        // Step 3: set PlayerPrefs
        PlayerPrefs.SetString(moduleName, prettyJson);
        PlayerPrefs.SetString("SelectedModulePath", jsonPath);
        PlayerPrefs.SetString("SelectedEnvironmentKey", moduleName);
        PlayerPrefs.SetString("LastSavedEnvironment", moduleName);
        PlayerPrefs.DeleteKey("NewModuleName");
        PlayerPrefs.Save();

        Debug.Log($"[QR] PlayerPrefs saved. SelectedModulePath={jsonPath}");
        Debug.Log($"[QR] All done — returning to StartScreenScene in 2s");

        SetFeedback($"✓ Imported: {moduleName}");
        Invoke(nameof(ReturnToStart), 2f);
    }

    

    Color32[] FlipVertical(Color32[] src, int width, int height)
    {
        Color32[] out2 = new Color32[src.Length];
        for (int row = 0; row < height; row++)
            Array.Copy(src, row * width, out2, (height - 1 - row) * width, width);
        return out2;
    }

    void SetFeedback(string msg)
    {
        if (feedbackText != null) feedbackText.text = msg;
        Debug.Log($"[QR] Feedback: {msg}");
    }

    void ApplyRotationCorrection()
    {
        float angle = webcamTexture.videoRotationAngle;
        cameraFeedImage.rectTransform.localEulerAngles = new Vector3(0, 0, -angle);
        if (webcamTexture.videoVerticallyMirrored)
        {
            var s = cameraFeedImage.rectTransform.localScale;
            s.y *= -1;
            cameraFeedImage.rectTransform.localScale = s;
        }
    }

    void AdjustCameraFeedAspect()
    {
        float angle = webcamTexture.videoRotationAngle;
        bool rotated = Mathf.Abs(angle) == 90 || Mathf.Abs(angle) == 270;
        float vw = rotated ? webcamTexture.height : webcamTexture.width;
        float vh = rotated ? webcamTexture.width : webcamTexture.height;
        float vr = vw / vh;

        RectTransform rt = cameraFeedImage.GetComponent<RectTransform>();
        RectTransform prt = rt.parent.GetComponent<RectTransform>();
        float pw = prt.rect.width;
        float ph = prt.rect.height;
        float pr = pw / ph;

        rt.sizeDelta = vr > pr
            ? new Vector2(pw, pw / vr)
            : new Vector2(ph * vr, ph);
    }

    public void ReturnToStart()
    {
        StopAllCoroutines();
        if (webcamTexture != null && webcamTexture.isPlaying) webcamTexture.Stop();
        SceneManager.LoadScene("StartScreenScene");
    }

    void OnDestroy()
    {
        StopAllCoroutines();
        if (webcamTexture != null && webcamTexture.isPlaying) webcamTexture.Stop();
    }
}