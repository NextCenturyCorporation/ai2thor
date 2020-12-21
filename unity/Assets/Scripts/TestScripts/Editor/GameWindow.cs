#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;

/// <summary>
/// Class for setting the gamewindow resolution within the editor
/// </summary>
public static class GameWindow
{
    static object _gameViewSizesInstance;
    static MethodInfo _getGroup;

    static GameWindow()
    {
        var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
        var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var instanceProp = singleType.GetProperty("instance");
        _getGroup = sizesType.GetMethod("GetGroup");
        _gameViewSizesInstance = instanceProp.GetValue(null, null);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public static bool ResolutionExists(int width, int height)
    {
        return FindResolutionIndex(width, height) != -1;
    }

    /// <summary>
    /// Adds a resolution to the standalone resolutions 
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public static void AddResolution(int width, int height)
    {
        // Do not add the resolution if it already exists
        if (ResolutionExists(width, height))
            return;

        // Add new custom resolution to the standalone group
        var group = GetGroup();
        var addCustomSize = _getGroup.ReturnType.GetMethod("AddCustomSize");
        var gameViewSizeType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");
        var gameViewSizeConstructor = gameViewSizeType.GetConstructor(new Type[] { typeof(int), typeof(int), typeof(int), typeof(string) });
        var newSize = gameViewSizeConstructor.Invoke(new object[] { 1, width, height, width + "x" + height });
        addCustomSize.Invoke(group, new object[] { newSize });
    }

    /// <summary>
    /// Sets the gameview window to the 
    /// </summary>
    /// <param name="index"></param>
    public static void SetResolution(int width, int height)
    {
        if (!ResolutionExists(width, height))
        {
            AddResolution(width, height);
        }

        var index = FindResolutionIndex(width, height);
        var gameViewWindowType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        var selectedSizeIndexProp = gameViewWindowType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var gameViewWindow = EditorWindow.GetWindow(gameViewWindowType);
        selectedSizeIndexProp.SetValue(gameViewWindow, index, null);
    }

    /// <summary>
    /// Searches for the index in the standalone group resolutions which matches the resolution provided.
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns>Resolution index in group, -1 if not found </returns>
    private static int FindResolutionIndex(int width, int height)
    {
        var group = GetGroup();
        var groupType = group.GetType();
        var getBuiltinCount = groupType.GetMethod("GetBuiltinCount");
        var getCustomCount = groupType.GetMethod("GetCustomCount");
        int sizesCount = (int)getBuiltinCount.Invoke(group, null) + (int)getCustomCount.Invoke(group, null);
        var getGameViewSize = groupType.GetMethod("GetGameViewSize");
        var gameViewSizeType = getGameViewSize.ReturnType;
        var widthProp = gameViewSizeType.GetProperty("width");
        var heightProp = gameViewSizeType.GetProperty("height");
        var indexValue = new object[1];

        for (int i = 0; i < sizesCount; i++)
        {
            indexValue[0] = i;
            var size = getGameViewSize.Invoke(group, indexValue);
            int sizeWidth = (int)widthProp.GetValue(size, null);
            int sizeHeight = (int)heightProp.GetValue(size, null);
            if (sizeWidth == width && sizeHeight == height)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Returns the standalone resolution group
    /// </summary>
    /// <returns></returns>
    private static object GetGroup()
    {
        return _getGroup.Invoke(_gameViewSizesInstance, new object[] { 0 });
    }
}
#endif