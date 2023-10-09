﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class JenkinsAddressables
{
    private static AddressableAssetSettings _setting;
    public static AddressableAssetSettings setting
    {
        get
        {
            if (_setting == null)
            {
                _setting = UnityEditor.AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>("Assets/AddressableAssetsData/AddressableAssetSettings.asset");
            }
            return _setting;
        }
    }

    public static void AutoUpdate(bool isCancelFix = false)
    {
        string contentStateDataPath = ContentUpdateScript.GetContentStateDataPath(false);
        if (!string.IsNullOrEmpty(contentStateDataPath))
        {
            CheckForUpdateContent(contentStateDataPath, isCancelFix);
            BuildUpdate(contentStateDataPath);
        }
        else
        {
            Debug.LogError("没找到addressables_content_state.bin文件");
        }
    }

    public static void CheckForUpdateContent(string contentStateDataPath, bool isCancelFix)
    {
        List<AddressableAssetEntry> entrys = ContentUpdateScript.GatherModifiedEntries(setting, contentStateDataPath);

        if (entrys.Count == 0) return;
        StringBuilder sbuider = new StringBuilder();
        sbuider.AppendLine("Need Update Assets:");
        foreach (var _ in entrys)
        {
            sbuider.AppendLine(_.address);
        }
        Debug.Log(sbuider.ToString());

        //将被修改过的资源单独分组
        var groupName = string.Format("UpdateGroup_{0}", DateTime.Now.ToString("yyyyMMdd"));
        if (isCancelFix)
        {
            CreateContentUpdateGroup(setting, entrys, groupName);
        }
        else
        {
            ContentUpdateScript.CreateContentUpdateGroup(setting, entrys, groupName);
        }

    }

    public static void CreateContentUpdateGroup(AddressableAssetSettings settings, List<AddressableAssetEntry> items, string groupName)
    {
        var contentGroup = settings.CreateGroup(groupName, false, false, true, null);
        var schema = contentGroup.AddSchema<BundledAssetGroupSchema>();
        schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
        schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
        schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
        contentGroup.AddSchema<ContentUpdateGroupSchema>().StaticContent = false;
        settings.MoveEntries(items, contentGroup);
    }

    /// <summary>
    /// 打增量更新的包
    /// </summary>
    /// <param name="contentStateDataPathh"></param>
    public static void BuildUpdate(string contentStateDataPathh)
    {
        if (!string.IsNullOrEmpty(contentStateDataPathh))
        {
            AddressablesPlayerBuildResult result = ContentUpdateScript.BuildContentUpdate(setting, contentStateDataPathh);
            Debug.Log("BuildFinish path = " + setting.RemoteCatalogBuildPath.GetValue(setting));
        }
    }
}
