using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;

/// <summary>
/// 
/// </summary>
public class AddressablesEditor
{
    private static string LINUX_CACHED_DIR = "CachedAddressableLink/Linux/aa";
    private static string LINUX_STREAMING_DIR = "StreamingAssets/aa";

    private static string OSX_CACHED_DIR = "CachedAddressableLink/OSX/aa";
    private static string OSX_STREAMING_DIR = "Contents/Resources/Data/StreamingAssets/aa";

    /// <summary>
    /// Configures all assets within Addressables folder to addressables group
    /// </summary>
    public static void RefreshAddressables() {
        AddressableAssetGroup group = AddressableAssetSettingsDefaultObject.Settings.DefaultGroup;
        string path = "Assets/Addressables";
        string[] guids = AssetDatabase.FindAssets("", new[] { path });

        List<AddressableAssetEntry> entriesAdded = new List<AddressableAssetEntry>();
        for (int i = 0; i < guids.Length; i++)
        {
            entriesAdded.Add(AddToAddressablesGroup(guids[i], group));
        }

        AddressableAssetSettingsDefaultObject.Settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, true);
    }

    
    [MenuItem("Tools/Addressables/Add Selected Folder to Group/Custom Objects")]
    public static void SetAddressablesAtFolderOnDefaultGroup() {
        SetAddressablesAtFolderOnGroup(AddressableAssetSettingsDefaultObject.Settings.FindGroup("Custom Objects"));
    }
    
    [MenuItem("Tools/Addressables/Add Selected Folder to Group/Default")]
    public static void SetAddressablesAtFolderOnCustomObjectsGroup() {
        SetAddressablesAtFolderOnGroup(AddressableAssetSettingsDefaultObject.Settings.DefaultGroup);
    }
    
    /// <summary>
    /// Adds all content within folder recursively to addressables group
    /// </summary>
    public static void SetAddressablesAtFolderOnGroup(AddressableAssetGroup group)
    {
        string path = GetSelectedFolder();

        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("No path selected for marking as addressables!");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("", new[] { path });

        var entriesAdded = new List<AddressableAssetEntry>();
        for (int i = 0; i < guids.Length; i++)
        {
            entriesAdded.Add(AddToAddressablesGroup(guids[i], group));
        }

        AddressableAssetSettingsDefaultObject.Settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, true);
    }

    private static AddressableAssetEntry AddToAddressablesGroup(string guid, AddressableAssetGroup group)
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

        AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
        entry.address = AssetDatabase.GUIDToAssetPath(guid);

        Debug.Log(AssetDatabase.GUIDToAssetPath(guid) + " was added to Addressables " + group.Name + " group!");

        return entry;
    }

    private static string GetSelectedFolder()
    {
        var path = "";
        var obj = Selection.activeObject;
        if (obj == null)
        {
            return string.Empty;
        }
        else
        {
            path = AssetDatabase.GetAssetPath(obj.GetInstanceID());
        }
        if (path.Length > 0)
        {
            if (Directory.Exists(path))
            {
                return path;
            }
        }

        return string.Empty;
    }

    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
#if UNITY_STANDALONE_LINUX
        string sourceDir = Path.Combine(Path.GetDirectoryName(Application.dataPath), LINUX_CACHED_DIR);
        string dataDir = Path.ChangeExtension(pathToBuiltProject, null) + "_Data/";
        string targetDir = Path.Combine(dataDir, LINUX_STREAMING_DIR);
#endif

#if UNITY_STANDALONE_OSX
        string sourceDir = Path.Combine(Path.GetDirectoryName(Application.dataPath), OSX_CACHED_DIR);
        string targetDir = Path.Combine(pathToBuiltProject, OSX_STREAMING_DIR);
#endif

#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
        // Do not copy default addressables if newly generated addressables were detected
        if (!Directory.Exists(targetDir))
        {
            CopyFilesRecursively(sourceDir, targetDir);
        }
#endif
    }

    private static void CopyFilesRecursively(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir))
            File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)));

        foreach (var directory in Directory.GetDirectories(sourceDir))
            CopyFilesRecursively(directory, Path.Combine(targetDir, Path.GetFileName(directory)));
    }
}