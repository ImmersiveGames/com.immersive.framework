using Immersive.Framework.Editor.Common;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.PlayerParticipation
{
    [CustomEditor(typeof(SceneProvidedLocalPlayerAuthoring))]
    public sealed class SceneProvidedLocalPlayerAuthoringEditor : UnityEditor.Editor
    {
        private SerializedProperty _playerSlotProfile;
        private SerializedProperty _localPlayerHost;
        private SerializedProperty _actorProfile;
        private SerializedProperty _admissionTiming;
        private bool _showDebug;

        private void OnEnable()
        {
            _localPlayerHost = serializedObject.FindProperty("localPlayerHost");
            _playerSlotProfile = serializedObject.FindProperty("playerSlotProfile");
            _actorProfile = serializedObject.FindProperty("actorProfile");
            _admissionTiming = serializedObject.FindProperty("admissionTiming");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            SceneProvidedLocalPlayerAuthoring authoring =
                (SceneProvidedLocalPlayerAuthoring)target;

            EditorGUILayout.LabelField(
                new GUIContent(
                    "Scene-Provided Local Player",
                    "The consumer authors physical Player composition. The Framework validates it in the Editor and resolves/adopts it at runtime."),
                EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            DrawConfiguration();
            bool changed = EditorGUI.EndChangeCheck();
            bool modified = serializedObject.ApplyModifiedProperties();
            if (changed || modified)
            {
                Undo.RecordObject(authoring, "Invalidate Scene-Provided Local Player Configuration");
                authoring.EditorSetAuthoringResult(
                    SceneProvidedLocalPlayerAuthoringStatus.NotValidated,
                    "Scene-Provided Local Player configuration changed. Validate the authored composition.");
                EditorUtility.SetDirty(authoring);
                PrefabUtility.RecordPrefabInstancePropertyModifications(authoring);
            }

            DrawConfigurationStatus(authoring);
            DrawActions(authoring);
            if (Application.isPlaying)
            {
                DrawRuntimeStatus(authoring);
            }

            DrawDebug(authoring);
        }

        private void DrawConfiguration()
        {
            FrameworkAuthoringInspectorGui.Section("Player");
            EditorGUILayout.PropertyField(
                _playerSlotProfile,
                new GUIContent("Player Slot", "Exact configured Session Player Slot."));
            EditorGUILayout.PropertyField(
                _actorProfile,
                new GUIContent("Actor Profile", "Player Protagonist Profile and Presentation prefab authority."));

            FrameworkAuthoringInspectorGui.Section("Local Player Host");
            EditorGUILayout.PropertyField(
                _localPlayerHost,
                new GUIContent("Host", "Nearest ancestral Local Player Host that owns the authored hierarchy."));

            FrameworkAuthoringInspectorGui.Section("Initial Placement");
            EditorGUILayout.PropertyField(
                _admissionTiming,
                new GUIContent("Timing", "Activity lifecycle moment that requests admission."));
        }

        private static void DrawConfigurationStatus(SceneProvidedLocalPlayerAuthoring authoring)
        {
            FrameworkAuthoringInspectorGui.Section("Configuration Status");
            if (authoring.LastAuthoringStatus == SceneProvidedLocalPlayerAuthoringStatus.NotValidated)
            {
                EditorGUILayout.LabelField("Status", "Not Validated");
                return;
            }

            if (authoring.LastAuthoringStatus == SceneProvidedLocalPlayerAuthoringStatus.Valid)
            {
                EditorGUILayout.LabelField("Status", "Valid");
                EditorGUILayout.LabelField("Composition", "Authored and validated");
                return;
            }

            EditorGUILayout.LabelField("Status", "Invalid");
            EditorGUILayout.HelpBox(
                string.IsNullOrWhiteSpace(authoring.LastAuthoringDiagnostic)
                    ? "The Scene-Provided Local Player configuration is invalid."
                    : authoring.LastAuthoringDiagnostic,
                MessageType.Error);
        }

        private static void DrawActions(SceneProvidedLocalPlayerAuthoring authoring)
        {
            FrameworkAuthoringInspectorGui.Section("Actions");
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if (GUILayout.Button(new GUIContent(
                        "Validate",
                        "Validates authored composition and prefab provenance without creating or changing content.")))
                {
                    SceneProvidedLocalPlayerAuthoringUtility.Validate(authoring, true);
                }
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField(
                    "Authoring actions unavailable in Play Mode.",
                    EditorStyles.miniLabel);
            }
        }

        private static void DrawRuntimeStatus(SceneProvidedLocalPlayerAuthoring authoring)
        {
            FrameworkAuthoringInspectorGui.Section("Runtime Status");
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.LabelField("Admission", authoring.HasActiveAdmission ? "Admitted" : "Not Admitted");
                EditorGUILayout.LabelField("Runtime", authoring.RuntimeReady ? "Ready" : "Unavailable");
                EditorGUILayout.ObjectField("Host", authoring.LocalPlayerHost, typeof(LocalPlayerHostAuthoring), true);
            }

            if (!authoring.RuntimeReady && !string.IsNullOrWhiteSpace(authoring.RuntimeDiagnostic))
            {
                EditorGUILayout.HelpBox(authoring.RuntimeDiagnostic, MessageType.Warning);
            }
        }

        private void DrawDebug(SceneProvidedLocalPlayerAuthoring authoring)
        {
            EditorGUILayout.Space(7f);
            _showDebug = EditorGUILayout.Foldout(
                _showDebug,
                new GUIContent("Advanced / Debug", "Shows non-authoritative runtime diagnostics."),
                true);
            if (!_showDebug)
            {
                return;
            }

            EditorGUI.indentLevel++;
            EditorGUILayout.TextField(
                "Player Slot ID",
                authoring.TryGetPlayerSlotId(out var slot, out _) ? slot.StableText : string.Empty);
            EditorGUILayout.TextField("Runtime Diagnostic", authoring.RuntimeDiagnostic);
            ScenePlayerActorAdoptionResult adoption = authoring.LastActorAdoptionResult;
            EditorGUILayout.TextField("Adoption", adoption != null ? adoption.ToDiagnosticString() : string.Empty);
            EditorGUI.indentLevel--;
        }
    }
}
