using Immersive.Framework.Actors;
using Immersive.Framework.Editor.Common;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.PlayerParticipation
{
    [CustomEditor(typeof(SceneLocalPlayerAdmissionAuthoring))]
    public sealed class SceneLocalPlayerAdmissionAuthoringEditor :
        UnityEditor.Editor
    {
        private static readonly GUIContent PlayerSlotLabel =
            new GUIContent(
                "Player Slot",
                "Exact configured Session Player Slot admitted by this Scene-Provided Player.");

        private static readonly GUIContent ActorProfileLabel =
            new GUIContent(
                "Actor Profile",
                "Player / Protagonist Actor Profile. Its Logical Actor Host prefab is the authored prefab authority for this Scene-Provided Player.");

        private static readonly GUIContent AdmissionTimingLabel =
            new GUIContent(
                "Timing",
                "Activity lifecycle moment in which this existing Scene Player requests admission.");

        private static readonly GUIContent InitialPlacementLabel =
            new GUIContent(
                "Initial Placement",
                "Preserve the authored Scene Actor pose, or apply the exact Activity-local Player Slot placement before adoption.");

        private static readonly GUIContent ApplyRebuildLabel =
            new GUIContent(
                "Apply / Rebuild",
                "Materializes the selected Actor Profile Logical Actor Host under Actor Mount, binds the exact Scene Actor instance and stores typed Actor evidence. Matching authored prefab instances are preserved; conflicting content is never replaced silently.");

        private static readonly GUIContent ValidateLabel =
            new GUIContent(
                "Validate",
                "Validates the authored composition, Scene Actor prefab provenance and stored typed Actor evidence without creating content or starting runtime admission.");

        private SerializedProperty playerSlotProfile;
        private SerializedProperty actorProfile;
        private SerializedProperty sceneLogicalPlayerActor;
        private SerializedProperty admissionTiming;
        private SerializedProperty initialPlacementPolicy;

        private bool showDebug;

        private void OnEnable()
        {
            playerSlotProfile =
                serializedObject.FindProperty(
                    "playerSlotProfile");
            actorProfile =
                serializedObject.FindProperty(
                    "actorProfile");
            sceneLogicalPlayerActor =
                serializedObject.FindProperty(
                    "sceneLogicalPlayerActor");
            admissionTiming =
                serializedObject.FindProperty(
                    "admissionTiming");
            initialPlacementPolicy =
                serializedObject.FindProperty(
                    "initialPlacementPolicy");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            SceneLocalPlayerAdmissionAuthoring authoring =
                (SceneLocalPlayerAdmissionAuthoring)target;

            EditorGUILayout.LabelField(
                new GUIContent(
                    "Scene-Provided Local Player",
                    "Authors one local Player already present in the Scene. Player Slot and Actor Profile define admission intent; Apply / Rebuild materializes the exact Actor composition under the same-root Local Player Host."),
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
                    "Invalidate Scene-Provided Player Configuration");

                authoring.EditorSetAuthoringResult(
                    SceneLocalPlayerAdmissionAuthoringStatus.NotValidated,
                    "Scene-Provided Player configuration changed. Run Apply / Rebuild and Validate.");

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
            FrameworkAuthoringInspectorGui.Section(
                "Configuration");

            EditorGUILayout.PropertyField(
                playerSlotProfile,
                PlayerSlotLabel);

            EditorGUILayout.PropertyField(
                actorProfile,
                ActorProfileLabel);

            EditorGUILayout.PropertyField(
                admissionTiming,
                AdmissionTimingLabel);

            EditorGUILayout.PropertyField(
                initialPlacementPolicy,
                InitialPlacementLabel);
        }

        private static void DrawConfigurationStatus(
            SceneLocalPlayerAdmissionAuthoring authoring)
        {
            FrameworkAuthoringInspectorGui.Section(
                "Configuration Status");

            SceneLocalPlayerAdmissionAuthoringStatus status =
                authoring.LastAuthoringStatus;

            if (status ==
                SceneLocalPlayerAdmissionAuthoringStatus.NotValidated)
            {
                EditorGUILayout.LabelField(
                    "Status",
                    "Not Validated");
                return;
            }

            if (status ==
                SceneLocalPlayerAdmissionAuthoringStatus.Valid)
            {
                bool materialized =
                    authoring.HasTypedActorEvidence &&
                    authoring.SceneLogicalPlayerActor != null;

                EditorGUILayout.LabelField(
                    "Status",
                    "Valid");

                EditorGUILayout.LabelField(
                    new GUIContent(
                        "Materialization",
                        "Ready requires the exact Scene Actor binding plus stored typed Actor provenance from Apply / Rebuild."),
                    new GUIContent(
                        materialized
                            ? "Ready"
                            : "Incomplete"));

                if (!materialized)
                {
                    EditorGUILayout.HelpBox(
                        "Materialized Actor evidence is incomplete. Run Apply / Rebuild and Validate.",
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
                    ? "The Scene-Provided Player configuration is invalid."
                    : authoring.LastAuthoringDiagnostic,
                MessageType.Error);
        }

        private static void DrawActions(
            SceneLocalPlayerAdmissionAuthoring authoring)
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
                    SceneLocalPlayerAdmissionAuthoringUtility
                        .ApplyOrRebuild(
                            authoring,
                            true,
                            true);
                }

                if (GUILayout.Button(
                        ValidateLabel))
                {
                    SceneLocalPlayerAdmissionAuthoringUtility
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
            SceneLocalPlayerAdmissionAuthoring authoring)
        {
            FrameworkAuthoringInspectorGui.Section(
                "Runtime Status");

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.LabelField(
                    new GUIContent(
                        "Admission",
                        "Whether this Scene-Provided Player currently owns an active admission."),
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
                        "Resolved same-root Local Player Host."),
                    authoring.LocalPlayerHost,
                    typeof(LocalPlayerHostAuthoring),
                    true);

                EditorGUILayout.ObjectField(
                    new GUIContent(
                        "Actor",
                        "Resolved Scene logical Player Actor."),
                    authoring.SceneLogicalPlayerActor,
                    typeof(PlayerActorDeclaration),
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
            SceneLocalPlayerAdmissionAuthoring authoring)
        {
            EditorGUILayout.Space(7f);
            showDebug =
                EditorGUILayout.Foldout(
                    showDebug,
                    new GUIContent(
                        "Advanced / Debug",
                        "Shows resolved composition, typed provenance and runtime/adoption evidence."),
                    true);

            if (!showDebug)
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
            SceneLocalPlayerAdmissionAuthoring authoring)
        {
            FrameworkAuthoringInspectorGui.Section(
                "Resolved Composition");

            ActorProfile selectedProfile =
                actorProfile.objectReferenceValue as ActorProfile;
            GameObject logicalActorPrefab =
                selectedProfile != null
                    ? selectedProfile.LogicalActorHostPrefab
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
                    "Profile Actor Prefab",
                    logicalActorPrefab,
                    typeof(GameObject),
                    false);

                EditorGUILayout.ObjectField(
                    "Scene Actor",
                    sceneLogicalPlayerActor.objectReferenceValue,
                    typeof(PlayerActorDeclaration),
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
            SceneLocalPlayerAdmissionAuthoring authoring)
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
                    "Actor Prefab",
                    authoring.EvidenceLogicalActorHostPrefab,
                    typeof(GameObject),
                    false);
            }

            DrawDiagnostic(
                "Evidence Diagnostic",
                authoring.EvidenceDiagnostic);
        }

        private static void DrawRuntimeEvidence(
            SceneLocalPlayerAdmissionAuthoring authoring)
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
            SceneLocalPlayerAdmissionAuthoring authoring)
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
