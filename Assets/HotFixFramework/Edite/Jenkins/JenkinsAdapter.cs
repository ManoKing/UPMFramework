using UnityEditor;
using System.IO;
using Debug = UnityEngine.Debug;
using System;
using UnityEditor.AddressableAssets.Settings;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.AddressableAssets.Build;

public class JenkinsAdapter : Editor
{
    // 发布平台
    private static BuildTarget buildTarget;
    // android包目标路径
    private static string androidPath = "./build/android/";
    // ios目标路径
    private static string iosPath = "./build/ios/";
    // Jenkins参数
    private static string[] args;
    public static string svnBuildTxtPath = Application.dataPath + "/Resources_moved/Config/SVNInfo/svnBuildTxt.txt";
    public static void Build()
    {
        buildTarget = GetBuildTargetArgument("Platform");
        // 获取打包类型
        bool isFix = false;
        bool isFirst = false;
        bool isCancelFix = false;
        string publishType = GetStringArgument("publishType");
        switch (publishType)
        {
            case "HotFixResources": // 热更资源
                isFix = true;
                break;
            case "ChannelPack": // 渠道包
                break;
            case "LocalPack": // 本地包，移除热更
                isCancelFix = true;
                break;
            case "FirstPack": // 首包
                isFirst = true;
                break;
            default:
                break;
        }

        // 版本回滚
        string versionRollback = GetStringArgument("versionRollback");
        Debug.Log("版本回滚.." + versionRollback);
        if (!string.IsNullOrEmpty(versionRollback))
        {
            Debug.Log("版本回滚..." + versionRollback);
            AddTimeStampAndDeleteFile();
            string srcFileName = "./ServerData/" + versionRollback;
            string destFileName = "./ServerData/Android.zip";
            if (File.Exists(srcFileName))
            {
                File.Move(srcFileName, destFileName);
            }
            return;
        }

        // 切换资源加载URL
        string settingProfile = GetStringArgument("settingProfile");
        switch (settingProfile)
        {
            case "Online":
                Debug.LogError("URL:Online");
                JenkinsAddressables.setting.activeProfileId = JenkinsAddressables.setting.profileSettings.GetProfileId("Online");
                break;
            case "Dev":
                Debug.LogError("URL:Dev");
                JenkinsAddressables.setting.activeProfileId = JenkinsAddressables.setting.profileSettings.GetProfileId("Dev");
                break;
            default:
                break;
        }
        EditorUtility.SetDirty(JenkinsAddressables.setting);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.LogError("URL Name2:" + JenkinsAddressables.setting.activeProfileId);

        // 取消热更流程
        Debug.LogError("jenkinsAdapter ================= cancelFix:" + isCancelFix);
        if (isCancelFix)
        {
            TurnOffFixFeature();
        }

        // 宏
        var symbols = GetStringArgument("Symbols");
        var SVN_REVISION = GetStringArgument("SVN_REVISION");
        var BUILD_NUMBER = GetStringArgument("BUILD_NUMBER");

        // 记录热更版本号
        if (symbols.Contains("MJ_DEBUG"))
        {
            File.WriteAllText(svnBuildTxtPath, "\nsvn:" + (SVN_REVISION == null ? "1" : SVN_REVISION + "\nbuildNumber: " + (BUILD_NUMBER == null ? "1" : BUILD_NUMBER)));
        }
        else
        {
            if (publishType == "HotFixResources")
            {
                File.WriteAllText(svnBuildTxtPath, "\nbuildNumber: " + (BUILD_NUMBER == null ? "1" : BUILD_NUMBER));
            }
        }


        // 热更流程通过isFix字段判断
        Debug.LogError("jenkinsAdapter ================= isFix:" + isFix);
        if (isFix) // 需要热更
        {
            AddTimeStampAndDeleteFile();
            // 执行 Check for Content Update Restrictions
            // 执行Update a Previous Build
            JenkinsAddressables.AutoUpdate(isCancelFix);
            return;
        }


        // 取消热更流程
        Debug.LogError("jenkinsAdapter ================= isCancelFix:" + isCancelFix);
        if (isCancelFix)
        {
            // 切换场景
            SetNotFixScene();
        }


#if UNITY_IOS
    // IOS打开资源更新              
    // AddressableToolHelper.SetNotFixScene();
#endif
        // isFirst首包流程
        AddTimeStampAndDeleteFile();
        Debug.LogError("jenkinsAdapter ================= isFirst:" + isFirst);

        if (isFirst) // 首包
        {
            BuildAllContent();
        }
        else
        {
            JenkinsAddressables.AutoUpdate(isCancelFix);
        }
        // 执行打包
        string packName = "xxx.xxx.xxx";
        List<string> levels = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {

            if (!scene.enabled) continue;

#if UNITY_EDITOR_OSX
            string path =  Path.Combine(Application.dataPath.Replace("Assets", ""), scene.path).Replace("\\", "/");
#else
            string path = Path.Combine(Application.dataPath.Replace("Assets", ""), scene.path).Replace("/", "\\");

#endif

            Debug.LogError("=================jenkins scene path:" + scene.path);
            if (File.Exists(path))
            {
                Debug.LogError("=================jenkins add scene Combine path:" + path);
                levels.Add(scene.path);
            }
            else
            {
                Debug.LogError("=================jenkins no scene Combine path:" + path);
                continue;
            }

        }
        string targetPath = androidPath;
        string suffix = ".apk";
        if (buildTarget == BuildTarget.iOS)
        {
            targetPath = iosPath;
            suffix = "";
        }

        BuildPipeline.BuildPlayer(levels.ToArray(), targetPath + packName + suffix, buildTarget,
        EditorUserBuildSettings.development == true ? BuildOptions.Development : BuildOptions.None);

        AssetDatabase.Refresh();
    }
    public static void BuildAllContent()
    {
        Debug.Log("全量打Ab");
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
        bool success = string.IsNullOrEmpty(result.Error);

        if (!success)
        {
            Debug.LogError("Addressables build error encountered: " + result.Error);
        }
    }

    /// <summary>
    /// 获取Jenkins参数
    /// </summary>
    /// <param name="argName"></param>
    /// <returns></returns>
    public static string GetStringArgument(string argName)
    {
        foreach (string arg in args)
        {
            if (arg.StartsWith(argName))
            {

                var result = arg.Replace(argName + "-", "");
                Debug.Log("\n==========" + result);
                return result;
            }
        }
        return null;
    }

    /// <summary>
    /// 转换打包平台
    /// </summary>
    /// <param name="argName"></param>
    /// <returns></returns>
    private static BuildTarget GetBuildTargetArgument(string argName)
    {
        string platform = GetStringArgument(argName);
        if (!string.IsNullOrEmpty(platform))
        {
            if (platform.Equals("android"))
                return BuildTarget.Android;

            else if (platform.Equals("ios"))
                return BuildTarget.iOS;

            else if (platform.Equals("Win"))
                return BuildTarget.StandaloneWindows;

        }
        return BuildTarget.NoTarget;
    }

    /// <summary>
    /// 添加时间戳，用于回归资源
    /// </summary>
    public static void AddTimeStampAndDeleteFile()
    {
        var timeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
        string srcFileName = "./ServerData/Android.zip";
        string destFileName = "./ServerData/Android_" + timeStamp + ".zip";
#if UNITY_IOS
        srcFileName = "./ServerData/iOS.zip";
        destFileName = "./ServerData/iOS_" + timeStamp + ".zip";
#endif
        if (File.Exists(srcFileName))
        {
            File.Move(srcFileName, destFileName);
        }
        string targetDirPath = "./ServerData/Android";
#if UNITY_IOS
        targetDirPath = "./ServerData/iOS";
#endif
        DeleteDirectory(targetDirPath);

    }

    /// <summary>
    /// 递归方法删除目录及文件
    /// </summary>
    /// <param name="path">路径</param> 

    public static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            DirectoryInfo info = new DirectoryInfo(path);
            DeleteFileByDirectory(info);
        }
    }

    private static void DeleteFileByDirectory(DirectoryInfo info)
    {
        foreach (DirectoryInfo newInfo in info.GetDirectories())
        {
            DeleteFileByDirectory(newInfo);
        }
        foreach (FileInfo newInfo in info.GetFiles())
        {
            newInfo.Attributes = newInfo.Attributes & ~(FileAttributes.Archive | FileAttributes.ReadOnly | FileAttributes.Hidden);
            newInfo.Delete();
        }
        info.Attributes = info.Attributes & ~(FileAttributes.Archive | FileAttributes.ReadOnly | FileAttributes.Hidden);
        info.Delete();
    }

    public static void TurnOffFixFeature()
    {
        Debug.Log("========= TurnOffFixFeature");
        // 关闭热更
        //AddressableToolHelper.setting.BuildRemoteCatalog = false;
        // 切换场景
        SetNotFixScene();
        // 资源放到本地
        //AddressableToolHelper.SettingGroupsLocalLoadType();
    }
    public static void SetNotFixScene()
    {
        List<EditorBuildSettingsScene> editorBuildSettingsScenes = new List<EditorBuildSettingsScene>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes) // 切换场景
        {
            if (scene != null)
            {
                if (!string.IsNullOrEmpty(scene.path))
                {
                    if (scene.path.Contains("InitScene"))
                    {
                        scene.enabled = true;
                        Debug.Log("======打开InitScene=======");
                    }
                    if (scene.path.Contains("HUpdateScene"))
                    {
                        scene.enabled = false;
                        Debug.Log("======关闭HUpdateScene=======");
                    }
                    else
                    {
                        editorBuildSettingsScenes.Add(scene);
                    }

                }
            }
        }
        Debug.Log("======场景切换成功=======");
        EditorBuildSettings.scenes = editorBuildSettingsScenes.ToArray();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
