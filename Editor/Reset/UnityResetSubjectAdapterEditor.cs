using System.Collections.Generic;
using Immersive.Framework.Reset.Unity;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Reset
{
    [CustomEditor(typeof(UnityResetSubjectAdapter))]
    internal sealed class UnityResetSubjectAdapterEditor : UnityEditor.Editor
    {
        private SerializedProperty idGeneration, subjectId, scope, displayName, participantDiscovery, includeInactiveParticipants, includeUnityResettableComponents;
        private SerializedProperty registerOnEnable, unregisterOnDisable, retryUntilRuntimeAvailable, runtimeSubjectIdPrefix, diagnosticTag, sourceActor, sourcePlayerActor;
        private bool showAdvanced, showDiagnostics;
        private string validationMessage;
        private MessageType validationMessageType;

        private void OnEnable()
        {
            idGeneration = serializedObject.FindProperty("idGeneration"); subjectId = serializedObject.FindProperty("subjectId"); scope = serializedObject.FindProperty("scope"); displayName = serializedObject.FindProperty("displayName");
            participantDiscovery = serializedObject.FindProperty("participantDiscovery"); includeInactiveParticipants = serializedObject.FindProperty("includeInactiveParticipants"); includeUnityResettableComponents = serializedObject.FindProperty("includeUnityResettableComponents");
            registerOnEnable = serializedObject.FindProperty("registerOnEnable"); unregisterOnDisable = serializedObject.FindProperty("unregisterOnDisable"); retryUntilRuntimeAvailable = serializedObject.FindProperty("retryUntilRuntimeAvailable"); runtimeSubjectIdPrefix = serializedObject.FindProperty("runtimeSubjectIdPrefix"); diagnosticTag = serializedObject.FindProperty("diagnosticTag"); sourceActor = serializedObject.FindProperty("sourceActor"); sourcePlayerActor = serializedObject.FindProperty("sourcePlayerActor");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            var adapter = (UnityResetSubjectAdapter)target;
            DrawOverview(adapter);
            EditorGUILayout.Space(6f); DrawSubject();
            EditorGUILayout.Space(6f); DrawParticipants(adapter);
            EditorGUILayout.Space(6f); DrawActions(adapter);
            EditorGUILayout.Space(6f); DrawAdvanced();
            EditorGUILayout.Space(6f); DrawDiagnostics(adapter);
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawOverview(UnityResetSubjectAdapter adapter)
        {
            EditorGUILayout.LabelField("Reset Subject", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("A Reset Subject groups the resettable parts of one logical object. Triggers request a reset of the Subject; Participants restore its state.", MessageType.Info);
            EditorGUILayout.LabelField("Display Name", string.IsNullOrWhiteSpace(displayName.stringValue) ? adapter.name : displayName.stringValue);
            EditorGUILayout.LabelField("Scope", scope.enumDisplayNames[scope.enumValueIndex]);
            EditorGUILayout.LabelField("Participants", CountParticipants(adapter).ToString() + " configured");
            EditorGUILayout.LabelField("Identity", ResolveIdentityStatus());
        }

        private void DrawSubject()
        {
            EditorGUILayout.LabelField("Subject", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(displayName, new GUIContent("Display Name"));
            EditorGUILayout.PropertyField(scope, new GUIContent("Scope", "Defines the runtime context that owns Subject registration."));
            EditorGUILayout.PropertyField(participantDiscovery, new GUIContent("Participants"));
            using (new EditorGUI.DisabledScope(true)) EditorGUILayout.TextField("Identity", subjectId.stringValue);
            EditorGUILayout.LabelField(idGeneration.intValue == 10 ? "Generated automatically when missing." : "Identity is derived at runtime by the selected generation mode.", EditorStyles.wordWrappedMiniLabel);
        }

        private void DrawParticipants(UnityResetSubjectAdapter adapter)
        {
            EditorGUILayout.LabelField("Participants", EditorStyles.boldLabel);
            bool sameObject = participantDiscovery.intValue == (int)UnityResetParticipantDiscoveryMode.SameGameObject;
            EditorGUILayout.HelpBox(sameObject ? "Uses Reset Participants attached to this GameObject." : "Uses Reset Participants in this hierarchy according to the configured inclusion rules.", MessageType.None);
            UnityResetParticipantBehaviour[] participants = sameObject ? adapter.GetComponents<UnityResetParticipantBehaviour>() : adapter.GetComponentsInChildren<UnityResetParticipantBehaviour>(includeInactiveParticipants.boolValue);
            EditorGUILayout.LabelField("Configured Participants", participants.Length.ToString());
            for (int index = 0; index < participants.Length; index++)
            {
                UnityResetParticipantBehaviour participant = participants[index];
                if (participant == null) continue;
                EditorGUILayout.LabelField(participant.DisplayName, EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField("  " + participant.Requiredness + "  Order " + participant.Order + "  ID: " + participant.ParticipantIdText, EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void DrawActions(UnityResetSubjectAdapter adapter)
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(targets.Length != 1))
            {
                if (GUILayout.Button("Generate Missing IDs")) GenerateMissingIds(adapter);
                if (GUILayout.Button("Validate Subject")) ValidateSubject(adapter);
            }
            if (targets.Length != 1) EditorGUILayout.HelpBox("Identity actions are disabled for multi-object editing so a generated ID is never copied between Subjects.", MessageType.Info);
            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                EditorGUILayout.HelpBox(validationMessage, validationMessageType);
            }
        }

        private void DrawAdvanced()
        {
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced", true);
            if (!showAdvanced) return;
            EditorGUILayout.LabelField("Registration", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(registerOnEnable); EditorGUILayout.PropertyField(unregisterOnDisable); EditorGUILayout.PropertyField(retryUntilRuntimeAvailable);
            EditorGUILayout.LabelField("Identity", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(idGeneration); EditorGUILayout.PropertyField(subjectId); EditorGUILayout.PropertyField(runtimeSubjectIdPrefix);
            EditorGUILayout.LabelField("Actor Identity Bridge", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(sourceActor); EditorGUILayout.PropertyField(sourcePlayerActor);
            EditorGUILayout.HelpBox("Actor declarations take precedence over authored Subject ID when they provide the selected identity mode. Conflicting actor identities are rejected by runtime registration.", MessageType.None);
            EditorGUILayout.LabelField("Participant Discovery Details", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(includeInactiveParticipants); EditorGUILayout.PropertyField(includeUnityResettableComponents); EditorGUILayout.PropertyField(diagnosticTag);
        }

        private void DrawDiagnostics(UnityResetSubjectAdapter adapter)
        {
            showDiagnostics = EditorGUILayout.Foldout(showDiagnostics, "Diagnostics", true);
            if (!showDiagnostics) return;
            EditorGUILayout.LabelField("Serialized Subject ID", subjectId.stringValue);
            EditorGUILayout.LabelField("ID Generation", idGeneration.enumDisplayNames[idGeneration.enumValueIndex]);
            EditorGUILayout.LabelField("Discovered Participant Count", CountParticipants(adapter).ToString());
            EditorGUILayout.LabelField("Runtime Port", adapter.ResetRegistrationRuntimeBindingStatus);
            EditorGUILayout.LabelField("Registration", Application.isPlaying ? (adapter.IsRegistered ? "Registered" : "Not registered") : "Runtime-dependent");
            EditorGUILayout.LabelField("Resolved Subject ID", adapter.SubjectId.IsValid ? adapter.SubjectId.StableText : "Not resolved");
            EditorGUILayout.LabelField("Registered Participants", adapter.RegisteredParticipantCount.ToString());
            EditorGUILayout.HelpBox(adapter.ResetRegistrationRuntimeBindingDiagnostic, MessageType.None);
        }

        private string ResolveIdentityStatus() => idGeneration.intValue != 10 ? "Runtime-derived" : string.IsNullOrWhiteSpace(subjectId.stringValue) ? "Missing" : "Generated / Valid";
        private int CountParticipants(UnityResetSubjectAdapter adapter) => participantDiscovery.intValue == (int)UnityResetParticipantDiscoveryMode.SameGameObject ? adapter.GetComponents<UnityResetParticipantBehaviour>().Length : adapter.GetComponentsInChildren<UnityResetParticipantBehaviour>(includeInactiveParticipants.boolValue).Length;

        private void GenerateMissingIds(UnityResetSubjectAdapter adapter)
        {
            Undo.RegisterCompleteObjectUndo(adapter, "Generate Reset IDs");
            bool hasActorIdentityBridge =
                sourceActor.objectReferenceValue != null ||
                sourcePlayerActor.objectReferenceValue != null;
            bool changed = !hasActorIdentityBridge &&
                ResetAuthoringIdentityUtility.GenerateMissingSubjectId(
                    idGeneration,
                    subjectId);
            UnityResetParticipantBehaviour[] participants = participantDiscovery.intValue == (int)UnityResetParticipantDiscoveryMode.SameGameObject ? adapter.GetComponents<UnityResetParticipantBehaviour>() : adapter.GetComponentsInChildren<UnityResetParticipantBehaviour>(includeInactiveParticipants.boolValue);
            foreach (UnityResetParticipantBehaviour participant in participants)
            {
                if (participant == null) continue;
                var participantObject = new SerializedObject(participant); participantObject.Update();
                SerializedProperty participantId = participantObject.FindProperty("participantId");
                Undo.RecordObject(participant, "Generate Reset Participant ID");
                if (ResetAuthoringIdentityUtility.GenerateMissingParticipantId(participantId)) { participantObject.ApplyModifiedPropertiesWithoutUndo(); ResetAuthoringIdentityUtility.RecordPrefabModification(participant); changed = true; }
            }
            if (changed) { serializedObject.ApplyModifiedPropertiesWithoutUndo(); ResetAuthoringIdentityUtility.RecordPrefabModification(adapter); }
        }

        private void ValidateSubject(UnityResetSubjectAdapter adapter)
        {
            var issues = new List<string>();
            if (idGeneration.intValue == 10 && string.IsNullOrWhiteSpace(subjectId.stringValue) && sourceActor.objectReferenceValue == null && sourcePlayerActor.objectReferenceValue == null) issues.Add("Authored stable Subject ID is missing and no actor identity bridge is configured.");
            var ids = new HashSet<string>();
            UnityResetParticipantBehaviour[] participants = participantDiscovery.intValue == (int)UnityResetParticipantDiscoveryMode.SameGameObject ? adapter.GetComponents<UnityResetParticipantBehaviour>() : adapter.GetComponentsInChildren<UnityResetParticipantBehaviour>(includeInactiveParticipants.boolValue);
            foreach (UnityResetParticipantBehaviour participant in participants) { if (participant == null) { issues.Add("A discovered participant reference is invalid."); continue; } if (string.IsNullOrWhiteSpace(participant.ParticipantIdText)) issues.Add("Participant '" + participant.name + "' has no Participant ID."); else if (!ids.Add(participant.ParticipantIdText.Trim())) issues.Add("Duplicate Participant ID: " + participant.ParticipantIdText.Trim()); }
            validationMessage = issues.Count == 0
                ? "Authoring evidence is valid. Runtime registration remains runtime-dependent."
                : string.Join("\n", issues);
            validationMessageType = issues.Count == 0
                ? MessageType.Info
                : MessageType.Error;
        }
    }
}
