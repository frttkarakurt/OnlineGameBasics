using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gerçek (non-fake) ilerleme göstergesi:
/// - SceneManager.LoadSceneAsync (allowSceneActivation = false)
/// - Ayrıca inspector'dan verilen Resources dosyalarını LoadAsync ile yükler (Resources folder)
/// - Tüm bu Async işlerin ilerlemelerini ağırlıklı ortalama ile birleştirir ve UI'ı günceller.
/// - Server onayı (TargetAllowSceneActivation) gelene kadar scene activation beklenir.
/// 
/// NOT: Eğer Addressables kullanıyorsan Resources yerine Addressables.LoadAssetAsync kullanılabilir.
/// </summary>
public class LoadingScreenManager : MonoBehaviour
{
    public static LoadingScreenManager Instance;

    [Header("UI refs")]
    public GameObject loadingCanvas; // başlangıçta inactive
    public TMP_Text messageText;
    public Slider progressBar;
    public TMP_Text percentText;

    [Header("Preload settings (optional)")]
    [Tooltip("Resources klasöründeki asset path'leri. örn: 'BigTexture' -> Assets/Resources/BigTexture.png")]
    public string[] assetsToPreload; // real assets placed inside Assets/Resources/

    [Header("Weights (how much each group contributes to final progress)")]
    [Range(0f,1f)]
    public float sceneWeight = 0.7f;     // sahnenin toplam yükleme içindeki payı
    [Range(0f,1f)]
    public float assetsWeight = 0.3f;    // resources yüklerinin payı (sceneWeight + assetsWeight should be ~1)

    // internal
    private AsyncOperation sceneOp;
    private List<ResourceRequest> resourceRequests = new List<ResourceRequest>();
    private bool isLoading = false;
    private string targetSceneName;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        if (loadingCanvas != null) loadingCanvas.SetActive(false);
    }

    /// <summary>
    /// Başlat: client kendi sahne yüklemelerini ve resource yüklemelerini başlatır.
    /// assetsToPreload inspector'da doldurulmuşsa onlar da yüklenir (gerçek iş).
    /// </summary>
    public void StartLoadingScene(string sceneName)
    {
        if (isLoading) return;

        isLoading = true;
        targetSceneName = sceneName;

        if (loadingCanvas != null) loadingCanvas.SetActive(true);
        if (messageText != null) messageText.text = $"Sahne yükleniyor: {sceneName}";
        if (progressBar != null) progressBar.value = 0f;
        if (percentText != null) percentText.text = "0%";

        // 1) Başlangıç: sahne async, activation false
        sceneOp = SceneManager.LoadSceneAsync(sceneName);
        sceneOp.allowSceneActivation = false;

        // 2) Başlat resources yükleri (gerçek assetler). Eğer hiç yoksa liste boş.
        resourceRequests.Clear();
        if (assetsToPreload != null && assetsToPreload.Length > 0)
        {
            foreach (var p in assetsToPreload)
            {
                if (string.IsNullOrEmpty(p)) continue;
                // gerçek yükleme: büyük bir texture/model koyarsan gerçekten süre alır
                var rr = Resources.LoadAsync<Object>(p);
                resourceRequests.Add(rr);
            }
        }

        StopAllCoroutines();
        StartCoroutine(UpdateCombinedProgress());
    }

    private IEnumerator UpdateCombinedProgress()
    {
        // Durum: sceneOp.progress -> 0..0.9 (activation öncesi). ResourceRequest.progress -> 0..1.
        while (true)
        {
            // scene part normalized: treat 0..0.9 as 0..1
            float sceneProgress = 0f;
            if (sceneOp != null)
                sceneProgress = Mathf.Clamp01(sceneOp.progress / 0.9f); // 0..1

            // assets part: average progress across all resource requests
            float assetsProgress = 1f;
            if (resourceRequests.Count > 0)
            {
                float sum = 0f;
                for (int i = 0; i < resourceRequests.Count; i++)
                    sum += resourceRequests[i].progress; // each 0..1
                assetsProgress = Mathf.Clamp01(sum / resourceRequests.Count);
            }

            // combined weighted progress
            float combined = Mathf.Clamp01(sceneProgress * sceneWeight + assetsProgress * assetsWeight);

            if (progressBar != null) progressBar.value = combined;
            if (percentText != null) percentText.text = Mathf.RoundToInt(combined * 100f) + "%";

            // tüm işler hazırsa (resources done ve scene reached 0.9), break BUT don't activate yet — wait server
            bool sceneReady = (sceneOp != null && sceneOp.progress >= 0.9f);
            bool assetsReady = true;
            foreach (var rr in resourceRequests)
            {
                if (!rr.isDone) { assetsReady = false; break; }
            }

            // If everything done, update UI to near-100 (but do not call allowSceneActivation here; server controls activation)
            if (sceneReady && assetsReady)
            {
                // set to full visually (but scene activation still blocked until server says so)
                if (progressBar != null) progressBar.value = 1f;
                if (percentText != null) percentText.text = "100%";
                // keep coroutine running to avoid exiting until server calls ActivateLoadedScene.
            }

            // Keep looping until server calls ActivateLoadedScene, which will set sceneOp.allowSceneActivation = true
            // If sceneOp is done (activation finished), break and hide
            if (sceneOp != null && sceneOp.isDone && (resourceRequests.Count == 0 || AllResourcesDone()))
                break;

            yield return null;
        }

        // Scene activated and assets loaded - small delay for UX
        yield return new WaitForSecondsRealtime(0.05f);
        Hide();
        isLoading = false;
    }

    private bool AllResourcesDone()
    {
        foreach (var rr in resourceRequests) if (!rr.isDone) return false;
        return true;
    }

    /// <summary>
    /// Server onayı geldiğinde client bu method'u çağırır: scene activation'ı true yap.
    /// </summary>
    public void ActivateLoadedScene()
    {
        if (sceneOp == null)
        {
            Hide();
            return;
        }

        if (!sceneOp.allowSceneActivation)
            sceneOp.allowSceneActivation = true;
    }

    public void Hide()
    {
        if (loadingCanvas != null) loadingCanvas.SetActive(false);
        if (progressBar != null) progressBar.value = 0f;
        if (percentText != null) percentText.text = "0%";
        resourceRequests.Clear();
        sceneOp = null;
        isLoading = false;
    }
}