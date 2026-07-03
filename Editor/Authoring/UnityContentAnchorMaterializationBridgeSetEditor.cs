using Immersive.Framework.ContentAnchor;
using Immersive.Framework.Editor.Editor.Validation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    [CustomEditor(typeof(UnityContentAnchorMaterializationBridgeSet))]
    [CanEditMultipleObjects]
    internal sealed class UnityContentAnchorMaterializationBridgeSetEditor : UnityEditor.Editor
    {
        private SerializedProperty _bridges;
        private SerializedProperty _reason;
        private SerializedProperty _logResults;

        private void OnEnable()
        {
            _bridges = serializedObject.FindProperty("bridges");
            _reason = serializedObject.FindProperty("reason");
            _logResults = serializedObject.FindProperty("logResults");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Unity Content Anchor Materialization Bridge Set", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Explicit authored set for batching Content Anchor materialization bridges. It preflights the whole set before materialization side effects and only runs when called directly.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.PropertyField(_bridges, new GUIContent("Bridges", "Explicit list of bridges. No automatic scene discovery or lifecycle subscription is performed."), true);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Diagnostics", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_reason, new GUIContent("Reason", "Default diagnostic reason for explicit bridge set calls."));
            EditorGUILayout.PropertyField(_logResults, new GUIContent("Log Results", "Logs explicit materialize/release results from this component."));

            DrawValidationReport();
            DrawDiagnosticsSnapshot();

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "Guardrails: this set does not create/destroy objects itself, subscribe to Route/Activity lifecycle, use Addressables, use pooling, spawn actors, join players, or bind camera/audio/save/gameplay consumers.",
                MessageType.None);

            serializedObject.ApplyModifiedProperties();
        }


        private void DrawDiagnosticsSnapshot()
        {
            if (serializedObject.isEditingMultipleObjects)
            {
                return;
            }

            var bridgeSet = target as UnityContentAnchorMaterializationBridgeSet;
            if (bridgeSet == null)
            {
                return;
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Runtime Diagnostics Snapshot", EditorStyles.boldLabel);
            var snapshot = bridgeSet.CreateDiagnosticsSnapshot();
            EditorGUILayout.LabelField("Bridge Count", snapshot.BridgeCount.ToString());
            EditorGUILayout.LabelField("Authoring Status", snapshot.AuthoringStatus.ToString());
            EditorGUILayout.LabelField("Registry Entries", snapshot.RegistryEntries.ToString());
            EditorGUILayout.LabelField("Registry Active", snapshot.RegistryActive.ToString());
            EditorGUILayout.LabelField("Physical Release Requests", snapshot.PhysicalReleaseRequests.ToString());
            EditorGUILayout.LabelField("Content Handles", snapshot.ContentHandleCount.ToString());
            EditorGUILayout.LabelField("Last Materialize All", snapshot.LastMaterializeAllStatus.ToString());
            EditorGUILayout.LabelField("Last Release All", snapshot.LastReleaseAllStatus.ToString());
            EditorGUILayout.HelpBox(
                "Snapshot is query-only. It does not materialize, release, bind, place or mutate Unity objects.",
                MessageType.None);
        }

        private void DrawValidationReport()
        {
            if (serializedObject.isEditingMultipleObjects)
            {
                EditorGUILayout.HelpBox("Multiple Content Anchor materialization bridge sets selected. Use Project Settings authoring validation to check loaded scenes.", MessageType.Info);
                return;
            }

            var bridgeSet = target as UnityContentAnchorMaterializationBridgeSet;
            if (bridgeSet == null)
            {
                return;
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Authoring Validation", EditorStyles.boldLabel);
            var report = FrameworkAuthoringValidator.ValidateUnityContentAnchorMaterializationBridgeSet(bridgeSet);
            FrameworkAuthoringValidationGui.DrawSummary(report);
            FrameworkAuthoringValidationGui.DrawIssues(report, false);
        }
    }
}
