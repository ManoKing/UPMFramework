using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 需要搭建一个白名单Jenkins，自动添加管理白名单。用于灰度测试
/// </summary>
public class DownloadConfigManager : MonoBehaviour
{
    private string hashUrl;
    private string configList;
    private string configPath;
    private string showVersion;
    public int loadSum = 0;
    public int loadIndex = 0;
    public void Init()
    {
        // 获取到当前游戏的版本号，根据版本号拼成下载链接
        TextAsset curPackageText = Resources.Load<TextAsset>("Config/CurrentPackageConfig");
        JSONNode curConfig = JSONNode.Parse(curPackageText.text);
        showVersion = curConfig["showVersion"];

#if MJ_DEBUG
        hashUrl = $"https://xxxxxxx/ConfigDebug/{showVersion}/catalog.txt";
        configList = $"https://xxxxxxx/ConfigDebug/{showVersion}/configList.txt";
        configPath = $"https://xxxxxxx/ConfigDebug/{showVersion}/Config/";
#else
        hashUrl = $"https://yyyyyyy/Config/{showVersion}/catalog.txt";
        configList = $"https://yyyyyyy/Config/{showVersion}/configList.txt";
        configPath = $"https://yyyyyyy/Config/{showVersion}/Config/";
#endif
        StartCoroutine(IsURLExists());
    }

    /// <summary>
    /// 对比Hash,判断是否需要热更
    /// </summary>
    /// <returns></returns>
    private IEnumerator DownloadHash()
    {
        UnityWebRequest request = UnityWebRequest.Get(hashUrl);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // 下载成功，可以通过request.downloadHandler获取下载的文件内容
            byte[] downloadedData = request.downloadHandler.data;

            // 在这里处理下载的资源文件
            string downloadedText = System.Text.Encoding.UTF8.GetString(downloadedData);
            // 打印文本内容
            Debug.Log("Downloaded text: " + downloadedText);
            TextAsset catalogAsset;
            // 判断持久化路径是否包含
            string filePath = Application.persistentDataPath + $"/{showVersion}/" + "catalog.txt";
            if (File.Exists(filePath))
            {
                string fileContent = File.ReadAllText(filePath);
                catalogAsset = new TextAsset(fileContent);
            }
            else
            {
                catalogAsset = Resources.Load<TextAsset>("Hash/catalog");
            }

            if (catalogAsset != null)
            {
                string catalogContent = catalogAsset.text;
                if (catalogContent != downloadedText)
                {
                    Debug.Log("Resources text: " + catalogContent);
                    StartCoroutine(DownloadConfigList(()=> {
                        // 保存修改后的内容到新的文件
                        string saveDicPath = Application.persistentDataPath + $"/{showVersion}";
                        string savePath = saveDicPath + "/" + "catalog.txt";
                        // 创建文件夹
                        if (!Directory.Exists(saveDicPath))
                        {
                            Directory.CreateDirectory(saveDicPath);
                        }
                        File.WriteAllText(savePath, downloadedText);
                    }));
                }
                else
                {
                    Debug.Log("Not Update Config" + catalogContent);
                }
            }
            else
            {
                Debug.LogError("Failed to load catalog at path ");
            }

        }
        else
        {
            // 下载失败，可以通过request.error获取错误信息
            Debug.LogError("Failed to config hash : " + request.error);
        }

        request.Dispose();
    }

    private IEnumerator DownloadConfigList(Action ConfigLoadSuccess)
    {
        UnityWebRequest request = UnityWebRequest.Get(configList);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // 下载成功，可以通过request.downloadHandler获取下载的文件内容
            byte[] downloadedData = request.downloadHandler.data;
            // 在这里处理下载的资源文件
            string downloadedText = System.Text.Encoding.UTF8.GetString(downloadedData);
            JSONNode curConfig = JSONNode.Parse(downloadedText);
            loadSum = curConfig["configList"].Count;
            for (int j = 0; j < curConfig["configList"].Count; j++)
            {
                Debug.Log("Downloaded config list: " + curConfig["configList"][j]);
                var configName = curConfig["configList"][j];
                StartCoroutine(DownloadResource(configPath, configName, ConfigLoadSuccess));
            }
        }
        else
        {
            // 下载失败，可以通过request.error获取错误信息
            Debug.LogError("Failed to download config list: " + request.error);
        }

        request.Dispose();
    }

    /// <summary>
    /// 下载配置表
    /// </summary>
    /// <returns></returns>
    private IEnumerator DownloadResource(string configPath, string configName, Action ConfigLoadSuccess)
    {
        UnityWebRequest request = UnityWebRequest.Get(configPath + configName);
        yield return request.SendWebRequest();
        //request.SendWebRequest();

        //while (!request.isDone)
        //{
        //    float progress = request.downloadProgress;
        //    Debug.LogError("Download progress: " + (progress * 100f) + "%");

        //    yield return null;
        //}
        if (request.result == UnityWebRequest.Result.Success)
        {
            ++loadIndex;
            byte[] downloadedData = request.downloadHandler.data;
            // 保存修改后的内容到新的文件
            string saveDicPath = Application.persistentDataPath + $"/{showVersion}";
            string savePath = saveDicPath + "/" + configName;
            // 创建文件夹
            if (!Directory.Exists(saveDicPath))
            {
                Directory.CreateDirectory(saveDicPath);
            }
            Debug.Log("config success: " + configName);
            if (configName.Contains("bytes"))
            {
                File.WriteAllBytes(savePath, downloadedData);
            }
            else
            {
                string downloadedText = System.Text.Encoding.UTF8.GetString(downloadedData);
                File.WriteAllText(savePath, downloadedText);
            }
            if (loadIndex >= loadSum) // 下载完成才会更新标识
            {
                Debug.Log("下载完成更新标识");
                ConfigLoadSuccess();
                loadSum = 0;
                loadIndex = 0;
            }
        }
        else
        {
            Debug.LogError("Failed to download config: " + request.error);
        }

        request.Dispose();
    }

    private IEnumerator IsURLExists()
    {
        UnityWebRequest webRequest = UnityWebRequest.Head(hashUrl);
        yield return webRequest.SendWebRequest();

        if (webRequest.responseCode == 200)
        {
            // 获取配置表是否需要下载，本地保存一个key，与远端的key对比，如果需要下载就覆盖这个key
            StartCoroutine(DownloadHash());
        }
        else
        {
            Debug.Log("URL does not exist");
        }
    }

    /// <summary>
    /// 获取需要下载的资源大小
    /// </summary>
    /// <returns></returns>
    private IEnumerator GetResourceSize(string resourceUrl)
    {
        UnityWebRequest request = UnityWebRequest.Head(resourceUrl);
        request.SendWebRequest();

        while (!request.isDone)
        {
            yield return null;
        }

        if (request.result == UnityWebRequest.Result.Success)
        {
            string contentLength = request.GetResponseHeader("Content-Length");
            long size = long.Parse(contentLength);
            Debug.LogError("Resource size: " + (size / (1024f * 1024f)).ToString("N2") + " MB");
        }
        else
        {
            Debug.LogError("Failed to retrieve resource size: " + request.error);
        }

        request.Dispose();
    }
   
}
