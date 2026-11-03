using UnityEditor;
using UnityEngine;
using KajoGames.CopyPaste.Analytics;

namespace KajoGames.CopyPaste.Analytics.Editor
{
    [CustomEditor(typeof(AnalyticsTracker))]
    public class AnalyticsTrackerEditor : UnityEditor.Editor
    {
        private SerializedProperty triggerOnProp;
        private SerializedProperty triggerOnceProp;
        private SerializedProperty targetTagProp;
        private SerializedProperty categoryProp;
        private SerializedProperty actionProp;
        private SerializedProperty labelProp;
        private SerializedProperty includeValueProp;
        private SerializedProperty numericValueProp;
        private SerializedProperty customFieldsProp;

        private void OnEnable()
        {
            triggerOnProp = serializedObject.FindProperty("triggerOn");
            triggerOnceProp = serializedObject.FindProperty("triggerOnce");
            targetTagProp = serializedObject.FindProperty("targetTag");
            categoryProp = serializedObject.FindProperty("category");
            actionProp = serializedObject.FindProperty("action");
            labelProp = serializedObject.FindProperty("label");
            includeValueProp = serializedObject.FindProperty("includeValue");
            numericValueProp = serializedObject.FindProperty("numericValue");
            customFieldsProp = serializedObject.FindProperty("customFields");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Analytics Event Tracker", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Dispatches analytics events directly from the Inspector. Wire to Button.onClick or trigger via physics / lifecycle events without writing code.", MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Trigger Setup", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(triggerOnProp);
            EditorGUILayout.PropertyField(triggerOnceProp);

            int triggerModeIndex = triggerOnProp.enumValueIndex;
            // If physics trigger or collision
            if (triggerModeIndex >= (int)AnalyticsTracker.TriggerMode.OnTriggerEnter2D)
            {
                EditorGUILayout.PropertyField(targetTagProp);
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Event Payload", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(categoryProp);
            EditorGUILayout.PropertyField(actionProp);
            EditorGUILayout.PropertyField(labelProp);

            // Validation Preview
            string actionStr = actionProp.stringValue;
            if (string.IsNullOrEmpty(actionStr))
            {
                EditorGUILayout.HelpBox("Action name cannot be empty.", MessageType.Error);
            }
            else if (actionStr.Length > AnalyticsEventData.MaxSegmentLength)
            {
                EditorGUILayout.HelpBox($"Action '{actionStr}' exceeds maximum length of {AnalyticsEventData.MaxSegmentLength} characters. It will be clamped at runtime.", MessageType.Warning);
            }

            string labelStr = labelProp.stringValue;
            if (!string.IsNullOrEmpty(labelStr) && labelStr.Length > AnalyticsEventData.MaxSegmentLength)
            {
                EditorGUILayout.HelpBox($"Label '{labelStr}' exceeds maximum length of {AnalyticsEventData.MaxSegmentLength} characters. It will be clamped at runtime.", MessageType.Warning);
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Telemetry Value", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(includeValueProp);
            if (includeValueProp.boolValue)
            {
                EditorGUILayout.PropertyField(numericValueProp);
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.PropertyField(customFieldsProp, true);

            EditorGUILayout.Space(8);
            if (Application.isPlaying)
            {
                if (GUILayout.Button("Test Fire Event in Play Mode", GUILayout.Height(28)))
                {
                    AnalyticsTracker tracker = (AnalyticsTracker)target;
                    tracker.Trigger();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
