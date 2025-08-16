#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;

public static class GameViewResolutionUtility
{
    /// <summary>
    /// Sets the game view resolution.
    /// </summary>
    public static void SetResolution(int index)
    {
        Type gameView = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        PropertyInfo selectedSizeIndex = gameView.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        EditorWindow window = EditorWindow.GetWindow(gameView);
        selectedSizeIndex.SetValue(window, index, null);
    }

    /// <summary>
    /// Adds a custom resolution.
    /// </summary>
    public static int AddResolution(int width, int height, string label)
    {
        Type gameViewSize = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");
        Type gameViewSizes = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
        Type gameViewSizeType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType");
        Type generic = typeof(ScriptableSingleton<>).MakeGenericType(gameViewSizes);
        MethodInfo getGroup = gameViewSizes.GetMethod("GetGroup");
        object instance = generic.GetProperty("instance").GetValue(null, null);
        // Get the current active GameViewSizeGroupType
        PropertyInfo currentGroupTypeProp = instance.GetType().GetProperty("currentGroupType");
        int groupType = (int)currentGroupTypeProp.GetValue(instance, null);
        object group = getGroup.Invoke(instance, new object[] { groupType });
        string groupTypeName = Enum.GetName(typeof(GameViewSizeGroupType), groupType) ?? groupType.ToString();
        MethodInfo getBuiltinCount = group.GetType().GetMethod("GetBuiltinCount");
        MethodInfo getCustomCount = group.GetType().GetMethod("GetCustomCount");
        MethodInfo getGameViewSize = group.GetType().GetMethod("GetGameViewSize");
        int builtinCount = (int)getBuiltinCount.Invoke(group, null);
        int customCount = (int)getCustomCount.Invoke(group, null);
        int total = builtinCount + customCount;
        for (int i = 0; i < total; i++)
        {
            var size = getGameViewSize.Invoke(group, new object[] { i });
            var baseTextProp = size.GetType().GetProperty("baseText");
            var widthProp = size.GetType().GetProperty("width");
            var heightProp = size.GetType().GetProperty("height");
            string name = (string)baseTextProp.GetValue(size, null);
            int w = (int)widthProp.GetValue(size, null);
            int h = (int)heightProp.GetValue(size, null);
            if (w == width && h == height)
            {
                Debug.Log($"[GameViewResolutionUtility] Skipped duplicate: {label} ({width}x{height}) in group {groupTypeName}, returning index {i}");
                return i;
            }
        }
        Type[] types = new Type[] { gameViewSizeType, typeof(int), typeof(int), typeof(string) };
        ConstructorInfo constructorInfo = gameViewSize.GetConstructor(types);
        object entry = constructorInfo.Invoke(new object[] { 1, width, height, label });
        MethodInfo addCustomSize = getGroup.ReturnType.GetMethod("AddCustomSize");
        addCustomSize.Invoke(group, new object[] { entry });
        // The new index is at the end
        int newIndex = builtinCount + customCount;
        Debug.Log($"[GameViewResolutionUtility] Added: {label} ({width}x{height}) at index {newIndex} in group {groupTypeName}");
        return newIndex;
    }

    /// <summary>
    /// Removes a custom resolution.
    /// Currently not used
    /// </summary>
    public static void RemoveResolution(int index)
    {
        Type gameViewSizes = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
        Type generic = typeof(ScriptableSingleton<>).MakeGenericType(gameViewSizes);
        MethodInfo getGroup = gameViewSizes.GetMethod("GetGroup");
        object instance = generic.GetProperty("instance").GetValue(null, null);
        object group = getGroup.Invoke(instance, new object[] { (int)GameViewSizeGroupType.Standalone });
        MethodInfo removeCustomSize = getGroup.ReturnType.GetMethod("RemoveCustomSize");
        removeCustomSize.Invoke(group, new object[] { index });
    }

    /// <summary>
    /// Gets the total count of game view resolutions.
    /// </summary>
    public static int GetCount()
    {
        Type gameViewSizes = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
        Type generic = typeof(ScriptableSingleton<>).MakeGenericType(gameViewSizes);
        MethodInfo getGroup = gameViewSizes.GetMethod("GetGroup");
        object instance = generic.GetProperty("instance").GetValue(null, null);
        PropertyInfo currentGroupType = instance.GetType().GetProperty("currentGroupType");
        GameViewSizeGroupType groupType = (GameViewSizeGroupType)(int)currentGroupType.GetValue(instance, null);
        object group = getGroup.Invoke(instance, new object[] { (int)groupType });
        MethodInfo getBuiltinCount = group.GetType().GetMethod("GetBuiltinCount");
        MethodInfo getCustomCount = group.GetType().GetMethod("GetCustomCount");
        return (int)getBuiltinCount.Invoke(group, null) + (int)getCustomCount.Invoke(group, null);
    }

    /// <summary>
    /// Gets the current menu resolution choice.
    /// </summary>
    public static int GetCurrentMenuResolutionChoice()
    {
        Type gameView = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        PropertyInfo selectedSizeIndex = gameView.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return (int)selectedSizeIndex.GetValue(EditorWindow.GetWindow(gameView));
    }
}
#endif
