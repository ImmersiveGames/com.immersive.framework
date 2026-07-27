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
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            SceneLocalPlayerAdmissionAuthoring authoring =
                (SceneLocalPlayerAdmissionAuthoring)target;

            DrawInspectorHeader();
            DrawParticipation();
            DrawLogicalActor();
            DrawAdmission();

            bool modified =
                serializedObject.ApplyModifiedProperties();
            if (modified)
            {
                authoring.EditorSetAuthoringResult(
                    SceneLocalPlayerAdmissionAuthoringStatus.NotValidated,
                    "Scene-Provided Player configuration changed. Run Apply / Rebuild and Validate.");
                EditorUtility.SetDirty(authoring);
            }

            DrawActions(authoring);
            DrawStatus(authoring);
            DrawDebug(authoring);
        }

        private static void DrawInspectorHeader()
        {
            EditorGUILayout.LabelField(
                "Scene-Provided Player",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Composes one local Player that already exists in a scene. The Local Player Host is resolved from this same GameObject. The Logical Player Actor remains an explicit authored prefab instance under Actor Mount.",
                MessageType.Info);
        }

        private void DrawParticipation()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                "Participation",
                EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                playerSlotProfile,
                new GUIContent(
                    "Player Slot Profile",
                    "Exact configured Session Slot admitted by this Scene-Provided Player."));
        }

        private void DrawLogicalActor()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                "Logical Actor",
                EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                actorProfile,
                new GUIContent(
                    "Actor Profile",
                    "Player / Protagonist profile whose Logical Actor Host prefab is used by the authored Actor instance."));
            EditorGUILayout.PropertyField(
                sceneLogicalPlayerActor,
                new GUIContent(
                    "Scene Logical Player Actor",
                    "Exact PlayerActorDeclaration under the same-root Local Player Host Actor Mount."));
        }

        private void DrawAdmission()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                "Admission",
                EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                admissionTiming,
                new GUIContent(
                    "Admission Timing",
                    "Scoped lifecycle moment that requests admission. This component never self-admits from Awake, Start or OnEnable."));
        }

        private static void DrawActions(
            SceneLocalPlayerAdmissionAuthoring authoring)
        {
            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply / Rebuild"))
                {
                    SceneLocalPlayerAdmissionAuthoringUtility
                        .ApplyOrRebuild(
                            authoring,
                            true,
                            true);
                }

                if (GUILayout.Button("Validate"))
                {
                    SceneLocalPlayerAdmissionAuthoringUtility
                        .Validate(
                            authoring,
                            true);
                }
            }
        }

        private static void DrawStatus(
            SceneLocalPlayerAdmissionAuthoring authoring)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                "Validation",
                EditorStyles.boldLabel);

            if (authoring.LastAuthoringStatus ==
                SceneLocalPlayerAdmissionAuthoringStatus.NotValidated)
            {
                EditorGUILayout.HelpBox(
                    authoring.LastAuthoringDiagnostic,
                    MessageType.None);
                return;
            }

            if (authoring.LastAuthoringStatus ==
                SceneLocalPlayerAdmissionAuthoringStatus.Valid)
            {
                EditorGUILayout.HelpBox(
                    authoring.LastAuthoringDiagnostic,
                    MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(
                authoring.LastAuthoringDiagnostic,
                MessageType.Error);
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
                EditorGUILayout.TextField(
                    "Player Slot ID",
                    authoring.TryGetPlayerSlotId(
                        out var slot,
                        out _)
                            ? slot.StableText
                            : string.Empty);
                EditorGUILayout.Toggle(
                    "Typed Actor Evidence",
                    authoring.HasTypedActorEvidence);
                EditorGUILayout.ObjectField(
                    "Evidence Actor Profile",
                    authoring.EvidenceActorProfile,
                    typeof(Immersive.Framework.Actors.ActorProfile),
                    false);
                EditorGUILayout.ObjectField(
                    "Evidence Actor Prefab",
                    authoring.EvidenceLogicalActorHostPrefab,
                    typeof(GameObject),
                    false);
                EditorGUILayout.TextArea(
                    authoring.EvidenceDiagnostic,
                    GUILayout.MinHeight(42f));
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

                ScenePlayerActorAdoptionResult adoption =
                    authoring.LastActorAdoptionResult;
                EditorGUILayout.TextField(
                    "Actor Ownership",
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
    }
}
