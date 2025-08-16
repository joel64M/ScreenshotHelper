using System;
using UnityEngine;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ScreenshotHelper : Singleton<ScreenshotHelper>
{
#if UNITY_EDITOR
    private const float DELAY_BETWEEN_SCREENSHOTS = 0.1f;

    public enum DeviceOrientation
    {
        Portrait,
        Landscape
    }

    [SerializeField] private DeviceOrientation m_orientation = DeviceOrientation.Portrait;

    [Header("Keyboard Shortcut {S} to Take Screenshots")]
    [SerializeField] private bool m_useKeyboardShortcutKey = false;
    [SerializeField] private string m_savePath;

    private UnityEngine.Vector2Int m_iphonePortrait = new UnityEngine.Vector2Int(1320, 2868);
    private UnityEngine.Vector2Int m_iphoneLandscape = new UnityEngine.Vector2Int(2868, 1320);
    private UnityEngine.Vector2Int m_ipadPortrait = new UnityEngine.Vector2Int(2048, 2732);
    private UnityEngine.Vector2Int m_ipadLandscape = new UnityEngine.Vector2Int(2732, 2048);

    [Header("Resolution Indices (Dont Touch)")]
    [SerializeField] private int m_iphonePortraitIndex = 0;
    [SerializeField] private int m_iphoneLandscapeIndex = 0;
    [SerializeField] private int m_ipadPortraitIndex = 0;
    [SerializeField] private int m_ipadLandscapeIndex = 0;

    private int m_count = 0;

    // Input System support
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    private UnityEngine.InputSystem.InputAction m_screenshotAction;
#endif

    private void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (m_screenshotAction == null)
        {
            m_screenshotAction = new UnityEngine.InputSystem.InputAction(type: UnityEngine.InputSystem.InputActionType.Button, binding: "<Keyboard>/s");
            m_screenshotAction.Enable();
        }
#endif
    }

    private void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (m_screenshotAction != null)
        {
            m_screenshotAction.Disable();
            m_screenshotAction.Dispose();
            m_screenshotAction = null;
        }
#endif
    }

    private void Update()
    {
        if (!m_useKeyboardShortcutKey)
            return;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (m_screenshotAction != null && m_screenshotAction.WasPressedThisFrame())
        {
            TakeScreenshots();
        }
#else
        if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.S))
        {
            TakeScreenshots();
        }
#endif
    }

    public void TakeScreenshots()
    {
        StartCoroutine(TakeScreenshotsAtResolutions());
    }

    private System.Collections.IEnumerator TakeScreenshotsAtResolutions()
    {
        string folderPath = string.IsNullOrEmpty(m_savePath) ? System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop) : m_savePath;
        // Ensure folderPath ends with a separator
        if (!folderPath.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
            folderPath += System.IO.Path.DirectorySeparatorChar;
        System.IO.Directory.CreateDirectory(folderPath);

        int ipadResIndex = m_orientation == DeviceOrientation.Portrait ? m_ipadPortraitIndex : m_ipadLandscapeIndex;
        int iphoneResIndex = m_orientation == DeviceOrientation.Portrait ? m_iphonePortraitIndex : m_iphoneLandscapeIndex;

        // Get version from PlayerSettings
        string version = UnityEditor.PlayerSettings.bundleVersion;
        string gameName = UnityEditor.PlayerSettings.productName;
        int currentResIndex = GameViewResolutionUtility.GetCurrentMenuResolutionChoice();

        // Take iPad screenshot
        GameViewResolutionUtility.SetResolution(ipadResIndex);
        yield return new UnityEngine.WaitForSeconds(DELAY_BETWEEN_SCREENSHOTS);
        string fileNameIpad = $"{m_orientation}-Screenshot-iPad-{gameName}-v{version}-{m_count}.png";
        string filePathIpad = folderPath + fileNameIpad;
        UnityEngine.ScreenCapture.CaptureScreenshot(filePathIpad);
        yield return new UnityEngine.WaitForSeconds(DELAY_BETWEEN_SCREENSHOTS);

        // Take iPhone screenshot
        GameViewResolutionUtility.SetResolution(iphoneResIndex);
        yield return new UnityEngine.WaitForSeconds(DELAY_BETWEEN_SCREENSHOTS);
        string fileNameIphone = $"{m_orientation}-Screenshot-iPhone-{gameName}-v{version}-{m_count}.png";
        string filePathIphone = folderPath + fileNameIphone;
        UnityEngine.ScreenCapture.CaptureScreenshot(filePathIphone);
        yield return new UnityEngine.WaitForSeconds(DELAY_BETWEEN_SCREENSHOTS);

        m_count++;
        // Restore original resolution
        GameViewResolutionUtility.SetResolution(currentResIndex);
    }

    public void SyncResolutions()
    {
        m_ipadPortraitIndex = GameViewResolutionUtility.AddResolution(m_ipadPortrait.x, m_ipadPortrait.y, "iPad Portrait");
        m_ipadLandscapeIndex = GameViewResolutionUtility.AddResolution(m_ipadLandscape.x, m_ipadLandscape.y, "iPad Landscape");
        m_iphonePortraitIndex = GameViewResolutionUtility.AddResolution(m_iphonePortrait.x, m_iphonePortrait.y, "iPhone Portrait");
        m_iphoneLandscapeIndex = GameViewResolutionUtility.AddResolution(m_iphoneLandscape.x, m_iphoneLandscape.y, "iPhone Landscape");
    }

    public void SelectSaveFolder()
    {
        string selected = UnityEditor.EditorUtility.OpenFolderPanel("Select Save Folder", System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), "");
        if (!string.IsNullOrEmpty(selected))
        {
            m_savePath = selected;
            UnityEditor.EditorUtility.SetDirty(this); // Mark as dirty to save change
        }
    }

    public bool AreIndexesSynced()
    {
        return m_ipadPortraitIndex != 0 && m_ipadLandscapeIndex != 0 &&
               m_iphonePortraitIndex != 0 && m_iphoneLandscapeIndex != 0;
    }

    [UnityEditor.CustomEditor(typeof(ScreenshotHelper))]
    private class ScreenshotHelperEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            UnityEngine.GUILayout.Space(10);

            ScreenshotHelper cs = (ScreenshotHelper)target;

            UnityEngine.GUIStyle bigButtonStyle = new UnityEngine.GUIStyle(UnityEngine.GUI.skin.button);
            bigButtonStyle.fixedHeight = 40;

            // Make button red if m_savePath is empty or null
            bool isSavePathEmpty = string.IsNullOrEmpty(cs.m_savePath);
            Color prevColor = UnityEngine.GUI.backgroundColor;

            bool areIndexesSynced = cs.AreIndexesSynced();
            if (isSavePathEmpty || !areIndexesSynced)
                UnityEngine.GUI.backgroundColor = Color.red;

            if (UnityEngine.GUILayout.Button("Take Screenshots", bigButtonStyle))
            {
                cs.TakeScreenshots();
            }

            if (isSavePathEmpty || !areIndexesSynced)
                UnityEngine.GUI.backgroundColor = prevColor;

            UnityEngine.GUILayout.Space(10);

            if (UnityEngine.GUILayout.Button("Select Save Folder"))
            {
                cs.SelectSaveFolder();
            }

            if (UnityEngine.GUILayout.Button("Sync Resolutions"))
            {
                cs.SyncResolutions();
                UnityEditor.EditorUtility.SetDirty(cs);
            }

            // Show warning labels
            if (isSavePathEmpty)
            {
                EditorGUILayout.HelpBox("Please set a save path before taking screenshots.", MessageType.Warning);
            }
            if (!areIndexesSynced)
            {
                EditorGUILayout.HelpBox("Please sync resolutions before taking screenshots.", MessageType.Warning);
            }
        }
    }
#endif
}
