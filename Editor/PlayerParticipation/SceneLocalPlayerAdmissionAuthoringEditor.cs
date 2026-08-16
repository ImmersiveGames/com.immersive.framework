using Immersive.Framework.Actors;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.PlayerParticipation
{
    [CustomEditor(typeof(SceneLocalPlayerAdmissionAuthoring))]
    public sealed class SceneLocalPlayerAdmissionAuthoringEditor :
        UnityEditor.Editor
    {
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

            EditorGUI.BeginChangeCheck();
            DrawScenePlayer(authoring);
            DrawAdmission();
            bool authoringChanged = EditorGUI.EndChangeCheck();

            bool modified =
                serializedObject.ApplyModifiedProperties();

            if (authoringChanged || modified)
            {
                authoring.EditorSetAuthoringResult(
                    SceneLocalPlayerAdmissionAuthoringStatus.NotValidated,
                    "Scene-Provided Player configuration changed. Run Apply / Rebuild and Validate.");
                EditorUtility.SetDirty(authoring);
            }

            DrawActions(authoring);
            DrawValidationSummary(authoring);

            if (Application.isPlaying)
            {
                DrawRuntimeStatus(authoring);
            }

            DrawDebug(authoring);
        }

        private void DrawScenePlayer(
            SceneLocalPlayerAdmissionAuthoring authoring)
        {
            DrawSection("Scene Player");

            EditorGUILayout.PropertyField(
                playerSlotProfile,
                new GUIContent(
                    "Player Slot",
                    "Exact configured Session Player Slot admitted by this Scene-Provided Player."));

            EditorGUILayout.PropertyField(
                actorProfile,
                new GUIContent(
                    "Actor Profile",
                    "Player / Protagonist Actor Profile. Its Logical Actor Host prefab is the single authored prefab authority for this Scene-Provided Player."));

            ActorProfile selectedProfile =
                actorProfile.objectReferenceValue as ActorProfile;
            GameObject logicalActorPrefab =
                selectedProfile != null
                    ? selectedProfile.LogicalActorHostPrefab
                    : null;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Logical Actor Prefab",
                    logicalActorPrefab,
                    typeof(GameObject),
                    false);

                EditorGUILayout.ObjectField(
                    "Scene Actor Instance",
                    authoring.SceneLogicalPlayerActor,
                    typeof(PlayerActorDeclaration),
                    true);
            }

            if (selectedProfile != null &&
                logicalActorPrefab != null &&
                authoring.SceneLogicalPlayerActor == null)
            {
                EditorGUILayout.HelpBox(
                    $"Scene Actor is derived from Actor Profile. Apply / Rebuild will instantiate '{logicalActorPrefab.name}' under the Local Player Host Actor Mount and bind its PlayerActorDeclaration. Existing conflicting Actor content is never replaced silently.",
                    MessageType.Info);
            }
        }

        private void DrawAdmission()
        {
            DrawSection("Admission");

            EditorGUILayout.PropertyField(
                admissionTiming,
                new GUIContent(
                    "Timing",
                    "Activity lifecycle moment in which this existing scene Player requests admission."));

            EditorGUILayout.PropertyField(
                initialPlacementPolicy,
                new GUIContent(
                    "Initial Placement",
                    "Preserve the authored Scene Actor pose, or apply the exact Activity-local Player Slot placement before adoption."));
        }

        private static void DrawActions(
            SceneLocalPlayerAdmissionAuthoring authoring)
        {
            DrawSection("Actions");

            using (new EditorGUI.DisabledScope(
                       Application.isPlaying))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(
                        new GUIContent(
                            "Apply / Rebuild",
                            "Ensure the selected Actor Profile Logical Actor Host prefab exists under Actor Mount, bind the exact Scene Actor instance, and store typed Actor evidence. Matching authored prefab instances are preserved; conflicting content is never replaced or unpacked silently.")))
                {
                    SceneLocalPlayerAdmissionAuthoringUtility
                        .ApplyOrRebuild(
                            authoring,
                            true,
                            true);
                }

                if (GUILayout.Button(
                        new GUIContent(
                            "Validate",
                            "Validate the authored composition, Scene Actor prefab provenance and stored typed Actor evidence without creating content or starting runtime admission.")))
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
                    "Authoring actions are unavailable in Play Mode.",
                    EditorStyles.miniLabel);
            }
        }

        private static void DrawValidationSummary(
            SceneLocalPlayerAdmissionAuthoring authoring)
        {
            DrawSection("Validation Summary");

            SceneLocalPlayerAdmissionAuthoringStatus status =
                authoring.LastAuthoringStatus;

            if (status ==
                SceneLocalPlayerAdmissionAuthoringStatus.NotValidated)
            {
                EditorGUILayout.LabelField(
                    "Status",
                    "Not Validated");

                if (!string.IsNullOrWhiteSpace(
                        authoring.LastAuthoringDiagnostic))
                {
                    EditorGUILayout.LabelField(
                        authoring.LastAuthoringDiagnostic,
                        EditorStyles.wordWrappedMiniLabel);
                }

                return;
            }

            if (status ==
                SceneLocalPlayerAdmissionAuthoringStatus.Valid)
            {
                EditorGUILayout.LabelField(
                    "Status",
                    authoring.HasTypedActorEvidence
                        ? "Valid — Scene Actor and typed evidence are materialized"
                        : "Valid");

                return;
            }

            EditorGUILayout.LabelField(
                "Status",
                "Invalid");

            EditorGUILayout.HelpBox(
                string.IsNullOrWhiteSpace(
                    authoring.LastAuthoringDiagnostic)
                    ? "The Scene-Provided Player authoring is invalid."
                    : authoring.LastAuthoringDiagnostic,
                MessageType.Error);
        }

        private static void DrawRuntimeStatus(
            SceneLocalPlayerAdmissionAuthoring authoring)
        {
            DrawSection("Runtime Status");

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.LabelField(
                    "Admission",
                    authoring.HasActiveAdmission
                        ? "Admitted"
                        : "Not Admitted");

                EditorGUILayout.LabelField(
                    "Runtime",
                    authoring.RuntimeReady
                        ? "Ready"
                        : "Unavailable");

                EditorGUILayout.ObjectField(
                    "Host",
                    authoring.LocalPlayerHost,
                    typeof(LocalPlayerHostAuthoring),
                    true);

                EditorGUILayout.ObjectField(
                    "Actor",
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
            EditorGUILayout.Space(6f);
            showDebug =
                EditorGUILayout.Foldout(
                    showDebug,
                    "Advanced / Debug",
                    true);

            if (!showDebug)
            {
                return;
            }

            DrawSection("Resolved Composition");

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Resolved Local Player Host",
                    authoring.LocalPlayerHost,
                    typeof(LocalPlayerHostAuthoring),
                    true);

                EditorGUILayout.ObjectField(
                    "Resolved Actor Mount",
                    authoring.LocalPlayerHost != null
                        ? authoring.LocalPlayerHost.ActorMount
                        : null,
                    typeof(Transform),
                    true);

                EditorGUILayout.ObjectField(
                    "Resolved Scene Actor",
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

            DrawSection("Typed Actor Evidence");

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

                EditorGUILayout.TextArea(
                    authoring.EvidenceDiagnostic,
                    GUILayout.MinHeight(42f));
            }

            DrawSection("Runtime Evidence");

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

                EditorGUILayout.TextArea(
                    authoring.RuntimeDiagnostic,
                    GUILayout.MinHeight(42f));
            }

            DrawSection("Actor Adoption");

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

                EditorGUILayout.TextArea(
                    adoption != null
                        ? adoption.ToDiagnosticString()
                        : "No Scene Actor adoption result has been recorded.",
                    GUILayout.MinHeight(72f));
            }
        }

        private static void DrawSection(
            string title)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                title,
                EditorStyles.boldLabel);
        }
    }
}
