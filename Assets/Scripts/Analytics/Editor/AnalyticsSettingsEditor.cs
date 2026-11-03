using UnityEditor;
using UnityEngine;
using KajoGames.CopyPaste.Analytics;

namespace KajoGames.CopyPaste.Analytics.Editor
{
    [CustomEditor(typeof(AnalyticsSettings))]
    public class AnalyticsSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Analytics Framework Global Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Centralized configuration for event submission, debug logging, sanitization, and pre-init queueing.", MessageType.Info);

            EditorGUILayout.Space(4);
            DrawDefaultInspector();

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("GameAnalytics SDK Links", EditorStyles.boldLabel);
            if (GUILayout.Button("Select GameAnalytics Settings", GUILayout.Height(26)))
            {
                Selection.activeObject = Resources.Load("GameAnalytics/Settings");
            }
        }
    }
}
