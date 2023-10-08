using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.UI;
public class Main : MonoBehaviour
{
    // 下载进度提醒
    public Text loadText;
    public Slider loadSlider;
    // 打开登录界面后再关闭，防止花屏
    public Canvas loadCanvas;

    // 下载Size弹框
    public GameObject tipsObj;
    public Button cancelBtn;
    public Button confirmBtn;
    public Text tipsText;

    // 预加载资源标签
#if UNITY_IOS
    private string downkey = "ND";
#else
    private string downkey = "Pre";
#endif

    // 动态切换URL
    private string newUrl;
    void Start()
    {
        DontDestroyOnLoad(loadCanvas.gameObject);
        DontDestroyOnLoad(gameObject);


#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        StartGame();
#else
        StartCoroutine(DownloadWhitelisted());
#endif
    }

    /// <summary>
    /// 防止花屏，进入游戏后销毁下载界面
    /// </summary>
    public void DestroySelf()
    {
        if (loadCanvas != null)
        {
            Destroy(loadCanvas.gameObject);
        }
    }

    void StartGame()
    {
#if UNITY_ANDROID || UNITY_EDITOR || UNITY_STANDALONE_WIN
        gameObject.AddComponent<LoadDll>();
#endif
#if UNITY_IOS // IOS不热更代码，直接进入游戏
        Addressables.InstantiateAsync("UIPrefabs/App");
#endif
    }

    /// <summary>
    /// 热更新，加载流程
    /// </summary>
    /// <returns></returns>
    IEnumerator UpdateAddressablesContent()
    {
        Debug.Log("UpdateAddressablesContent");
        var initHandle = Addressables.InitializeAsync();
        yield return initHandle;
        var handler = Addressables.CheckForCatalogUpdates(false);
        yield return handler;

        List<string> catalogs = handler.Result;
        if (catalogs != null)
        {
            Debug.Log($"need update catalog:{catalogs.Count}");
            foreach (var catalog in catalogs)
            {
                Debug.Log("catalog__:" + catalog);
            }

            if (catalogs.Count > 0)
            {
                // 更新Catalog
                var updateHandle = Addressables.UpdateCatalogs(catalogs, false);
                yield return updateHandle;
                var locators = updateHandle.Result;
                foreach (var locator in locators)
                {
                    foreach (var key in locator.Keys)
                    {
                        Debug.Log($"update : {key}");
                    }
                }
            }
        }
        // 获取下载大小
        var sizeHandle = Addressables.GetDownloadSizeAsync(downkey);
        yield return sizeHandle;
        long totalDownloadSize = sizeHandle.Result;
        Debug.Log("NEED downLoad size:" + totalDownloadSize);
        if (totalDownloadSize > 0)
        {
#if UNITY_ANDROID || UNITY_EDITOR
            StartCoroutine(UpdateAddressablesDownload(totalDownloadSize, handler));
#endif
#if UNITY_IOS
            // IOS需要弹框提醒下载资源大小
            tipsObj.SetActive(true);
            tipsText.text = "需要下载游戏资源，本次下载资源大小为" + (totalDownloadSize / (1024f * 1024f)).ToString("#0.000") + "MB";
            cancelBtn.onClick.AddListener(()=> { Application.Quit(); });
            confirmBtn.onClick.AddListener(()=> { StartCoroutine(UpdateAddressablesDownload(totalDownloadSize, handler)); 
                tipsObj.SetActive(false); });
#endif
        }
        else
        {
            StartCoroutine(StartGameAndReleaseHandler(handler));
        }

    }

    /// <summary>
    /// 预加载资源，下载
    /// </summary>
    /// <param name="totalDownloadSize"></param>
    /// <returns></returns>
    IEnumerator UpdateAddressablesDownload(long totalDownloadSize, AsyncOperationHandle<List<string>> handler)
    {
        var downloadHandle = Addressables.DownloadDependenciesAsync(downkey, false);
        while (downloadHandle.Status == AsyncOperationStatus.None)
        {
            float percent = downloadHandle.PercentComplete;

            var status = downloadHandle.GetDownloadStatus();
            float progress = status.Percent;
            Debug.Log($"已经下载：{(int)(totalDownloadSize * percent)}/{totalDownloadSize}");
            Debug.Log($"{progress * 100:0.0}" + "%");
            loadText.text = $"正在为您下载和校验配置，请耐心等待：" + $"{progress * 100:0.0}" + "%  已下载" + (totalDownloadSize / (1024f * 1024f) * percent).ToString("#0.000") + "MB";
            loadSlider.gameObject.SetActive(true);
            loadSlider.value = progress;
            yield return null;
        }

        if (downloadHandle.IsDone)
        {
            Debug.Log("已经下载完成！！！");
        }
        if (downloadHandle.Status == AsyncOperationStatus.Succeeded)
        {
            Addressables.Release(downloadHandle);
        }
        StartCoroutine(StartGameAndReleaseHandler(handler));
    }

    /// <summary>
    /// 进入游戏
    /// </summary>
    /// <param name="handler"></param>
    /// <returns></returns>
    IEnumerator StartGameAndReleaseHandler(AsyncOperationHandle<List<string>> handler)
    {
        Debug.Log("已经下载完成，准备进入游戏");
        Addressables.Release(handler);
        Debug.Log("释放hander成功");
        yield return null;
        StartGame();
    }

    /// <summary>
    /// 下载白名单：设计的核心是下载不影响进入游戏流程，下载不到就用Bundle包里面的资源
    /// </summary>
    /// <returns></returns>
    private IEnumerator DownloadWhitelisted()
    {

#if MJ_DEBUG // 测试环境
        var hashUrl = "https://xxxxx/whitelistedDeviceId.txt";
#else // 正式环境
        var hashUrl = "https://yyyyy/whitelistedDeviceId.txt";
#endif

        UnityWebRequest request = UnityWebRequest.Get(hashUrl);
        request.timeout = Mathf.FloorToInt(10f); // 如果下载失败，防止卡住太久
        //request.redirectLimit = 3;
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // 下载成功，可以通过request.downloadHandler获取下载的文件内容
            byte[] downloadedData = request.downloadHandler.data;

            // 在这里处理下载的资源文件
            string downloadedText = System.Text.Encoding.UTF8.GetString(downloadedData);
            Debug.Log(downloadedText);
            JSONNode curConfig = JSONNode.Parse(downloadedText);
            var isAllFix = curConfig["isAllFix"].ToBool();
            List<string> whitelistedDeviceId = new List<string>();
            for (int j = 0; j < curConfig["whitelistedDeviceId"].Count; j++)
            {
                whitelistedDeviceId.Add(curConfig["whitelistedDeviceId"][j]);
            }
#if UNITY_IOS
            newUrl = curConfig["pathCDN"].ToString() + "iOS/";
#elif UNITY_ANDROID
            newUrl = curConfig["pathCDN"].ToString() + "Android/";
#endif
            // 资源热更白名单
            if (isAllFix)
            {
                Debug.Log("全部热更");
                UpdateAndStartGame();
            }
            else
            {
                // 获取设备id
                string deviceId = SystemInfo.deviceUniqueIdentifier;
                Debug.Log("deviceId:" + deviceId);
                if (whitelistedDeviceId.Contains(deviceId))
                {
                    Debug.Log("白名单热更");
                    UpdateAndStartGame();
                }
                else
                {
                    Debug.Log("非热更");
                    StartGame();
                }
            }
        }
        else
        {
            // 下载失败，可以通过request.error获取错误信息
            Debug.LogError("Download whitelisted failed to download resource: " + request.error);

            StartGame();
        }

        request.Dispose();
    }

    private void UpdateAndStartGame()
    {
        // 修改CDN路径
        // Addressables.InternalIdTransformFunc = MyCustomTransform;
        // 更新CND配置表
        gameObject.AddComponent<DownloadConfigManager>().Init();
        StartCoroutine(UpdateAddressablesContent());
    }

    /// <summary>
    /// 动态修改CDN地址
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    private string MyCustomTransform(IResourceLocation location)
    {
        if (location.ResourceType == typeof(IAssetBundleResource) && location.InternalId.StartsWith("http"))
        {
            string filename = location.InternalId.Substring(location.InternalId.LastIndexOf("/") + 1);
            Debug.Log("动态修改CDN地址:" + newUrl + filename);
            return newUrl + filename;
        }
        return location.InternalId;
    }

}
