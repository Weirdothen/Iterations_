using UnityEditor;
using UnityEngine;
using KajoGames.CopyPaste.Analytics;

namespace KajoGames.CopyPaste.Analytics.Editor
{
    /// <summary>
    /// Editor menu utilities under Tools > Analytics for easy access and workflow shortcuts.
    /// </summary>
    public static class AnalyticsMenuItems
    {
        [MenuItem("Tools/Analytics/Select Framework Settings", false, 10)]
        public static void SelectFrameworkSettings()
        {
            AnalyticsSettings settings = Resources.Load<AnalyticsSettings>(AnalyticsSettings.ResourcePath);
            if (settings == null)
            {
                // Ensure directory exists
                string dir = "Assets/Resources/Analytics";
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                if (!AssetDatabase.IsValidFolder(dir))
                {
                    AssetDatabase.CreateFolder("Assets/Resources", "Analytics");
                }

                settings = ScriptableObject.CreateInstance<AnalyticsSettings>();
                AssetDatabase.CreateAsset(settings, $"{dir}/AnalyticsSettings.asset");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        [MenuItem("Tools/Analytics/Select GameAnalytics Settings", false, 11)]
        public static void SelectGASettings()
        {
            Object gaSettings = Resources.Load("GameAnalytics/Settings");
            if (gaSettings != null)
            {
                Selection.activeObject = gaSettings;
                EditorGUIUtility.PingObject(gaSettings);
            }
            else
            {
                Debug.LogWarning("[Analytics] GameAnalytics/Settings.asset could not be found in Resources.");
            }
        }

        [MenuItem("Tools/Analytics/Send Diagnostic Test Event", false, 30)]
        public static void SendTestEvent()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Analytics] Test events can only be sent while Play Mode is active.");
                return;
            }

            GameAnalyticsManager.Event("diagnostic", "editor_test_event", 1f);
            Debug.Log("[Analytics] Diagnostic test event dispatched.");
        }
    }
}
