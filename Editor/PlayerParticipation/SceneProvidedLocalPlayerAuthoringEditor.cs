using Immersive.Framework.Actors;
using Immersive.Framework.Editor.Common;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.PlayerParticipation
{
    [CustomEditor(typeof(SceneProvidedLocalPlayerAuthoring))]
    public sealed class SceneProvidedLocalPlayerAuthoringEditor :
        UnityEditor.Editor
    {
        private static readonly GUIContent PlayerSlotLabel =
            new GUIContent(
                "Player Slot",
                "Exact configured Session Player Slot admitted by this Scene-Provided Local Player.");

        private static readonly GUIContent ActorProfileLabel =
            new GUIContent(
                "Actor Profile",
                "Player / Protagonist Actor Profile. Its Presentation prefab is the authored presentation authority for this Scene-Provided Local Player.");

        private static readonly GUIContent AdmissionTimingLabel =
            new GUIContent(
                "Timing",
                "Activity lifecycle moment in which this Scene-Provided Local Player requests admission.");

        private static readonly GUIContent ApplyRebuildLabel =
            new GUIContent(
                "Apply / Rebuild",
                "Materializes the configured Player Actor Runtime Host under Actor Mount and the selected Actor Profile Presentation under its Presentation Mount. Matching authored prefab instances are preserved; conflicting content is never replaced silently.");

        private static readonly GUIContent ValidateLabel =
            new GUIContent(
                "Validate",
                "Validates the authored composition, Runtime Host and Presentation provenance, and stored typed evidence without creating content or starting runtime admission.");

        private SerializedProperty _playerSlotProfile;
        private SerializedProperty _localPlayerHost;
        private SerializedProperty _actorProfile;
        private SerializedProperty _scenePlayerActorRuntimeHost;
        private SerializedProperty _scenePresentation;
        private SerializedProperty _admissionTiming;

        private bool _showDebug;

        private void OnEnable()
        {
            _localPlayerHost =
                serializedObject.FindProperty(
                    "localPlayerHost");
            _playerSlotProfile =
                serializedObject.FindProperty(
                    "playerSlotProfile");
            _actorProfile =
                serializedObject.FindProperty(
                    "actorProfile");
            _scenePlayerActorRuntimeHost =
                serializedObject.FindProperty(
                    "scenePlayerActorRuntimeHost");
            _scenePresentation =
                serializedObject.FindProperty(
                    "scenePresentation");
            _admissionTiming =
                serializedObject.FindProperty(
                    "admissionTiming");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            SceneProvidedLocalPlayerAuthoring authoring =
                (SceneProvidedLocalPlayerAuthoring)target;

            EditorGUILayout.LabelField(
                new GUIContent(
                    "Scene-Provided Local Player",
                    "Authors one Local Player already present in Scene content. Player Slot and Actor Profile define admission intent; Apply / Rebuild materializes the generic Runtime Host and selected Presentation under the nearest ancestral Local Player Host."),
                EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            DrawConfiguration();
            bool authoringChanged =
                EditorGUI.EndChangeCheck();

            bool modified =
                serializedObject.ApplyModifiedProperties();

            if (authoringChanged || modified)
            {
                Undo.RecordObject(
                    authoring,
                    "Invalidate Scene-Provided Local Player Configuration");

                authoring.EditorSetAuthoringResult(
                    SceneProvidedLocalPlayerAuthoringStatus.NotValidated,
                    "Scene-Provided Local Player configuration changed. Run Apply / Rebuild and Validate.");

                EditorUtility.SetDirty(authoring);
                PrefabUtility.RecordPrefabInstancePropertyModifications(
                    authoring);
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
                PlayerSlotLabel);

            EditorGUILayout.PropertyField(
                _actorProfile,
                ActorProfileLabel);

            FrameworkAuthoringInspectorGui.Section("Local Player Host");
            EditorGUILayout.PropertyField(
                _localPlayerHost,
                new GUIContent(
                    "Host",
                    "Explicit Local Player Host that owns this Scene-Provided Local Player. It must be the nearest ancestral Local Player Host."));

            DrawActorRuntimeComposition(
                (SceneProvidedLocalPlayerAuthoring)target);

            FrameworkAuthoringInspectorGui.Section("Initial Placement");
            EditorGUILayout.PropertyField(
                _admissionTiming,
                AdmissionTimingLabel);
        }

        private static void DrawActorRuntimeComposition(
            SceneProvidedLocalPlayerAuthoring authoring)
        {
            FrameworkAuthoringInspectorGui.Section("Actor Runtime");

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    new GUIContent(
                        "Player Actor Runtime Host",
                        "Framework-owned generic runtime composition resolved or materialized by Apply / Rebuild."),
                    authoring.ScenePlayerActorRuntimeHost,
                    typeof(PlayerActorRuntimeHost),
                    true);
                EditorGUILayout.ObjectField(
                    new GUIContent(
                        "Presentation",
                        "Actor-specific visual content selected by the Actor Profile and resolved or materialized by Apply / Rebuild."),
                    authoring.ScenePresentation,
                    typeof(GameObject),
                    true);
            }
        }

        private static void DrawConfigurationStatus(
            SceneProvidedLocalPlayerAuthoring authoring)
        {
            FrameworkAuthoringInspectorGui.Section(
                "Configuration Status");

            SceneProvidedLocalPlayerAuthoringStatus status =
                authoring.LastAuthoringStatus;

            if (status ==
                SceneProvidedLocalPlayerAuthoringStatus.NotValidated)
            {
                EditorGUILayout.LabelField(
                    "Status",
                    "Not Validated");
                return;
            }

            if (status ==
                SceneProvidedLocalPlayerAuthoringStatus.Valid)
            {
                bool materialized =
                    authoring.HasTypedActorEvidence &&
                    authoring.ScenePlayerActorRuntimeHost != null &&
                    authoring.ScenePresentation != null;

                EditorGUILayout.LabelField(
                    "Status",
                    "Valid");

                EditorGUILayout.LabelField(
                    new GUIContent(
                    "Runtime Host + Presentation",
                        "Ready requires the exact Runtime Host and Presentation binding plus stored typed provenance from Apply / Rebuild."),
                    new GUIContent(
                        materialized
                            ? "Ready"
                            : "Incomplete"));

                if (!materialized)
                {
                    EditorGUILayout.HelpBox(
                        "Runtime Host or Presentation evidence is incomplete. Run Apply / Rebuild and Validate.",
                        MessageType.Warning);
                }

                return;
            }

            EditorGUILayout.LabelField(
                "Status",
                "Invalid");

            EditorGUILayout.HelpBox(
                string.IsNullOrWhiteSpace(
                    authoring.LastAuthoringDiagnostic)
                    ? "The Scene-Provided Local Player configuration is invalid."
                    : authoring.LastAuthoringDiagnostic,
                MessageType.Error);
        }

        private static void DrawActions(
            SceneProvidedLocalPlayerAuthoring authoring)
        {
            FrameworkAuthoringInspectorGui.Section(
                "Actions");

            using (new EditorGUI.DisabledScope(
                       Application.isPlaying))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(
                        ApplyRebuildLabel))
                {
                    SceneProvidedLocalPlayerAuthoringUtility
                        .ApplyOrRebuild(
                            authoring,
                            true,
                            true);
                }

                if (GUILayout.Button(
                        ValidateLabel))
                {
                    SceneProvidedLocalPlayerAuthoringUtility
                        .Validate(
                            authoring,
                            true);
                }
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField(
                    "Authoring actions unavailable in Play Mode.",
                    EditorStyles.miniLabel);
            }
        }

        private static void DrawRuntimeStatus(
            SceneProvidedLocalPlayerAuthoring authoring)
        {
            FrameworkAuthoringInspectorGui.Section(
                "Runtime Status");

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.LabelField(
                    new GUIContent(
                        "Admission",
                        "Whether this Scene-Provided Local Player currently owns an active admission."),
                    new GUIContent(
                        authoring.HasActiveAdmission
                            ? "Admitted"
                            : "Not Admitted"));

                EditorGUILayout.LabelField(
                    new GUIContent(
                        "Runtime",
                        "Whether the runtime composition required by this authoring surface is currently ready."),
                    new GUIContent(
                        authoring.RuntimeReady
                            ? "Ready"
                            : "Unavailable"));

                EditorGUILayout.ObjectField(
                    new GUIContent(
                        "Host",
                        "Resolved nearest ancestral Local Player Host."),
                    authoring.LocalPlayerHost,
                    typeof(LocalPlayerHostAuthoring),
                    true);

                EditorGUILayout.ObjectField(
                    new GUIContent(
                        "Runtime Host",
                        "Resolved Player Actor Runtime Host."),
                    authoring.ScenePlayerActorRuntimeHost,
                    typeof(PlayerActorRuntimeHost),
                    true);
            }

            if (!authoring.RuntimeReady &&
                !string.IsNullOrWhiteSpace(
                    authoring.RuntimeDiagnostic))
            {
                EditorGUILayout.HelpBox(
                    authoring.RuntimeDiagnostic,
                    MessageType.Warning);
            }
        }

        private void DrawDebug(
            SceneProvidedLocalPlayerAuthoring authoring)
        {
            EditorGUILayout.Space(7f);
            _showDebug =
                EditorGUILayout.Foldout(
                    _showDebug,
                    new GUIContent(
                        "Advanced / Debug",
                        "Shows resolved composition, typed provenance and runtime/adoption evidence."),
                    true);

            if (!_showDebug)
            {
                return;
            }

            EditorGUI.indentLevel++;

            DrawResolvedComposition(authoring);
            DrawTypedActorEvidence(authoring);
            DrawRuntimeEvidence(authoring);
            DrawActorAdoption(authoring);

            EditorGUI.indentLevel--;
        }

        private void DrawResolvedComposition(
            SceneProvidedLocalPlayerAuthoring authoring)
        {
            FrameworkAuthoringInspectorGui.Section(
                "Resolved Composition");

            ActorProfile selectedProfile =
                _actorProfile.objectReferenceValue as ActorProfile;
            GameObject presentationPrefab =
                selectedProfile != null
                    ? selectedProfile.PresentationPrefab
                    : null;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Local Player Host",
                    authoring.LocalPlayerHost,
                    typeof(LocalPlayerHostAuthoring),
                    true);

                EditorGUILayout.ObjectField(
                    "Actor Mount",
                    authoring.LocalPlayerHost != null
                        ? authoring.LocalPlayerHost.ActorMount
                        : null,
                    typeof(Transform),
                    true);

                EditorGUILayout.ObjectField(
                    "Profile Presentation Prefab",
                    presentationPrefab,
                    typeof(GameObject),
                    false);

                EditorGUILayout.ObjectField(
                    "Player Actor Runtime Host",
                    _scenePlayerActorRuntimeHost.objectReferenceValue,
                    typeof(PlayerActorRuntimeHost),
                    true);

                EditorGUILayout.ObjectField(
                    "Presentation",
                    _scenePresentation.objectReferenceValue,
                    typeof(GameObject),
                    true);

                EditorGUILayout.TextField(
                    "Player Slot ID",
                    authoring.TryGetPlayerSlotId(
                        out var slot,
                        out _)
                            ? slot.StableText
                            : string.Empty);
            }
        }

        private static void DrawTypedActorEvidence(
            SceneProvidedLocalPlayerAuthoring authoring)
        {
            FrameworkAuthoringInspectorGui.Section(
                "Typed Actor Evidence");

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle(
                    "Evidence Stored",
                    authoring.HasTypedActorEvidence);

                EditorGUILayout.ObjectField(
                    "Actor Profile",
                    authoring.EvidenceActorProfile,
                    typeof(ActorProfile),
                    false);

                EditorGUILayout.ObjectField(
                    "Presentation Prefab",
                    authoring.EvidencePresentationPrefab,
                    typeof(GameObject),
                    false);
            }

            DrawDiagnostic(
                "Evidence Diagnostic",
                authoring.EvidenceDiagnostic);
        }

        private static void DrawRuntimeEvidence(
            SceneProvidedLocalPlayerAuthoring authoring)
        {
            FrameworkAuthoringInspectorGui.Section(
                "Runtime Evidence");

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle(
                    "Host Joined",
                    authoring.LocalPlayerHost != null &&
                    authoring.LocalPlayerHost.IsJoined);

                EditorGUILayout.Toggle(
                    "Runtime Ready",
                    authoring.RuntimeReady);

                EditorGUILayout.Toggle(
                    "Active Admission",
                    authoring.HasActiveAdmission);
            }

            DrawDiagnostic(
                "Runtime Diagnostic",
                authoring.RuntimeDiagnostic);
        }

        private static void DrawActorAdoption(
            SceneProvidedLocalPlayerAuthoring authoring)
        {
            FrameworkAuthoringInspectorGui.Section(
                "Actor Adoption");

            ScenePlayerActorAdoptionResult adoption =
                authoring.LastActorAdoptionResult;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    "Physical Ownership",
                    authoring.ActorPhysicalOwnership.ToString());

                EditorGUILayout.TextField(
                    "Adoption Status",
                    adoption != null
                        ? adoption.Status.ToString()
                        : string.Empty);
            }

            DrawDiagnostic(
                "Adoption Diagnostic",
                adoption != null
                    ? adoption.ToDiagnosticString()
                    : "No Scene Actor adoption result has been recorded.");
        }

        private static void DrawDiagnostic(
            string label,
            string diagnostic)
        {
            if (string.IsNullOrWhiteSpace(diagnostic))
            {
                return;
            }

            EditorGUILayout.LabelField(
                new GUIContent(
                    label,
                    diagnostic),
                new GUIContent(diagnostic),
                EditorStyles.wordWrappedMiniLabel);
        }
    }
}
