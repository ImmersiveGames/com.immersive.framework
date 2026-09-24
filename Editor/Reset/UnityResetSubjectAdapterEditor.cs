using System.Collections.Generic;
using Immersive.Framework.Reset.Unity;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Reset
{
    [CustomEditor(typeof(UnityResetSubjectAdapter))]
    internal sealed class UnityResetSubjectAdapterEditor : UnityEditor.Editor
    {
        private SerializedProperty _idGeneration, _subjectId, _scope, _displayName, _participantDiscovery, _includeInactiveParticipants, _includeUnityResettableComponents;
        private SerializedProperty _registerOnEnable, _unregisterOnDisable, _retryUntilRuntimeAvailable, _runtimeSubjectIdPrefix, _diagnosticTag, _sourceActor, _sourcePlayerActor;
        private bool _showAdvanced, _showDiagnostics;
        private string _validationMessage;
        private MessageType _validationMessageType;

        private void OnEnable()
        {
            _idGeneration = serializedObject.FindProperty("idGeneration"); _subjectId = serializedObject.FindProperty("subjectId"); _scope = serializedObject.FindProperty("scope"); _displayName = serializedObject.FindProperty("displayName");
            _participantDiscovery = serializedObject.FindProperty("participantDiscovery"); _includeInactiveParticipants = serializedObject.FindProperty("includeInactiveParticipants"); _includeUnityResettableComponents = serializedObject.FindProperty("includeUnityResettableComponents");
            _registerOnEnable = serializedObject.FindProperty("registerOnEnable"); _unregisterOnDisable = serializedObject.FindProperty("unregisterOnDisable"); _retryUntilRuntimeAvailable = serializedObject.FindProperty("retryUntilRuntimeAvailable"); _runtimeSubjectIdPrefix = serializedObject.FindProperty("runtimeSubjectIdPrefix"); _diagnosticTag = serializedObject.FindProperty("diagnosticTag"); _sourceActor = serializedObject.FindProperty("sourceActor"); _sourcePlayerActor = serializedObject.FindProperty("sourcePlayerActor");
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
            EditorGUILayout.LabelField("Display Name", string.IsNullOrWhiteSpace(_displayName.stringValue) ? adapter.name : _displayName.stringValue);
            EditorGUILayout.LabelField("Scope", _scope.enumDisplayNames[_scope.enumValueIndex]);
            EditorGUILayout.LabelField("Participants", CountParticipants(adapter).ToString() + " configured");
            EditorGUILayout.LabelField("Identity", ResolveIdentityStatus());
        }

        private void DrawSubject()
        {
            EditorGUILayout.LabelField("Subject", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_displayName, new GUIContent("Display Name"));
            EditorGUILayout.PropertyField(_scope, new GUIContent("Scope", "Defines the runtime context that owns Subject registration."));
            EditorGUILayout.PropertyField(_participantDiscovery, new GUIContent("Participants"));
            using (new EditorGUI.DisabledScope(true)) EditorGUILayout.TextField("Identity", _subjectId.stringValue);
            EditorGUILayout.LabelField(_idGeneration.intValue == 10 ? "Generated automatically when missing." : "Identity is derived at runtime by the selected generation mode.", EditorStyles.wordWrappedMiniLabel);
        }

        private void DrawParticipants(UnityResetSubjectAdapter adapter)
        {
            EditorGUILayout.LabelField("Participants", EditorStyles.boldLabel);
            bool sameObject = _participantDiscovery.intValue == (int)UnityResetParticipantDiscoveryMode.SameGameObject;
            EditorGUILayout.HelpBox(sameObject ? "Uses Reset Participants attached to this GameObject." : "Uses Reset Participants in this hierarchy according to the configured inclusion rules.", MessageType.None);
            UnityResetParticipantBehaviour[] participants = sameObject ? adapter.GetComponents<UnityResetParticipantBehaviour>() : adapter.GetComponentsInChildren<UnityResetParticipantBehaviour>(_includeInactiveParticipants.boolValue);
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
            if (!string.IsNullOrWhiteSpace(_validationMessage))
            {
                EditorGUILayout.HelpBox(_validationMessage, _validationMessageType);
            }
        }

        private void DrawAdvanced()
        {
            _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "Advanced", true);
            if (!_showAdvanced) return;
            EditorGUILayout.LabelField("Registration", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_registerOnEnable); EditorGUILayout.PropertyField(_unregisterOnDisable); EditorGUILayout.PropertyField(_retryUntilRuntimeAvailable);
            EditorGUILayout.LabelField("Identity", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_idGeneration); EditorGUILayout.PropertyField(_subjectId); EditorGUILayout.PropertyField(_runtimeSubjectIdPrefix);
            EditorGUILayout.LabelField("Actor Identity Bridge", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_sourceActor); EditorGUILayout.PropertyField(_sourcePlayerActor);
            EditorGUILayout.HelpBox("Actor declarations take precedence over authored Subject ID when they provide the selected identity mode. Conflicting actor identities are rejected by runtime registration.", MessageType.None);
            EditorGUILayout.LabelField("Participant Discovery Details", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_includeInactiveParticipants); EditorGUILayout.PropertyField(_includeUnityResettableComponents); EditorGUILayout.PropertyField(_diagnosticTag);
        }

        private void DrawDiagnostics(UnityResetSubjectAdapter adapter)
        {
            _showDiagnostics = EditorGUILayout.Foldout(_showDiagnostics, "Diagnostics", true);
            if (!_showDiagnostics) return;
            EditorGUILayout.LabelField("Serialized Subject ID", _subjectId.stringValue);
            EditorGUILayout.LabelField("ID Generation", _idGeneration.enumDisplayNames[_idGeneration.enumValueIndex]);
            EditorGUILayout.LabelField("Discovered Participant Count", CountParticipants(adapter).ToString());
            EditorGUILayout.LabelField("Runtime Port", adapter.ResetRegistrationRuntimeBindingStatus);
            EditorGUILayout.LabelField("Registration", Application.isPlaying ? (adapter.IsRegistered ? "Registered" : "Not registered") : "Runtime-dependent");
            EditorGUILayout.LabelField("Resolved Subject ID", adapter.SubjectId.IsValid ? adapter.SubjectId.StableText : "Not resolved");
            EditorGUILayout.LabelField("Registered Participants", adapter.RegisteredParticipantCount.ToString());
            EditorGUILayout.HelpBox(adapter.ResetRegistrationRuntimeBindingDiagnostic, MessageType.None);
        }

        private string ResolveIdentityStatus() => _idGeneration.intValue != 10 ? "Runtime-derived" : string.IsNullOrWhiteSpace(_subjectId.stringValue) ? "Missing" : "Generated / Valid";
        private int CountParticipants(UnityResetSubjectAdapter adapter) => _participantDiscovery.intValue == (int)UnityResetParticipantDiscoveryMode.SameGameObject ? adapter.GetComponents<UnityResetParticipantBehaviour>().Length : adapter.GetComponentsInChildren<UnityResetParticipantBehaviour>(_includeInactiveParticipants.boolValue).Length;

        private void GenerateMissingIds(UnityResetSubjectAdapter adapter)
        {
            Undo.RegisterCompleteObjectUndo(adapter, "Generate Reset IDs");
            bool hasActorIdentityBridge =
                _sourceActor.objectReferenceValue != null ||
                _sourcePlayerActor.objectReferenceValue != null;
            bool changed = !hasActorIdentityBridge &&
                ResetAuthoringIdentityUtility.GenerateMissingSubjectId(
                    _idGeneration,
                    _subjectId);
            UnityResetParticipantBehaviour[] participants = _participantDiscovery.intValue == (int)UnityResetParticipantDiscoveryMode.SameGameObject ? adapter.GetComponents<UnityResetParticipantBehaviour>() : adapter.GetComponentsInChildren<UnityResetParticipantBehaviour>(_includeInactiveParticipants.boolValue);
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
            if (_idGeneration.intValue == 10 && string.IsNullOrWhiteSpace(_subjectId.stringValue) && _sourceActor.objectReferenceValue == null && _sourcePlayerActor.objectReferenceValue == null) issues.Add("Authored stable Subject ID is missing and no actor identity bridge is configured.");
            var ids = new HashSet<string>();
            UnityResetParticipantBehaviour[] participants = _participantDiscovery.intValue == (int)UnityResetParticipantDiscoveryMode.SameGameObject ? adapter.GetComponents<UnityResetParticipantBehaviour>() : adapter.GetComponentsInChildren<UnityResetParticipantBehaviour>(_includeInactiveParticipants.boolValue);
            foreach (UnityResetParticipantBehaviour participant in participants) { if (participant == null) { issues.Add("A discovered participant reference is invalid."); continue; } if (string.IsNullOrWhiteSpace(participant.ParticipantIdText)) issues.Add("Participant '" + participant.name + "' has no Participant ID."); else if (!ids.Add(participant.ParticipantIdText.Trim())) issues.Add("Duplicate Participant ID: " + participant.ParticipantIdText.Trim()); }
            _validationMessage = issues.Count == 0
                ? "Authoring evidence is valid. Runtime registration remains runtime-dependent."
                : string.Join("\n", issues);
            _validationMessageType = issues.Count == 0
                ? MessageType.Info
                : MessageType.Error;
        }
    }
}
