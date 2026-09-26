using System.Collections.Generic;
using Immersive.Framework.Editor.Common;
using Immersive.Framework.Reset.Unity;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Reset
{
    [CustomEditor(typeof(UnityResetSubjectAdapter))]
    internal sealed class UnityResetSubjectAdapterEditor : UnityEditor.Editor
    {
        private static readonly GUIContent ScopeLabel =
            new GUIContent("Scope", "Defines the runtime context that owns Subject registration.");
        private static readonly GUIContent ParticipantsLabel =
            new GUIContent("Participants", "How Reset Participants are discovered for this Subject.");
        private static readonly GUIContent SubjectIdLabel =
            new GUIContent("Subject ID", "Authored stable identity for this Reset Subject.");
        private static readonly GUIContent IdentityLabel = new GUIContent("Identity");
        private static readonly GUIContent ProvidedByActorValue = new GUIContent(
            "Provided by Actor",
            "Provided by the Actor Identity Bridge in Advanced; takes precedence over Subject ID.");
        private static readonly GUIContent RuntimeGeneratedValue = new GUIContent(
            "Runtime-generated",
            "Generated automatically at runtime. Switch Generation in Advanced to author a stable id instead.");

        private SerializedProperty _idGeneration, _subjectId, _scope, _displayName, _participantDiscovery, _includeInactiveParticipants, _includeUnityResettableComponents;
        private SerializedProperty _registerOnEnable, _unregisterOnDisable, _retryUntilRuntimeAvailable, _runtimeSubjectIdPrefix, _diagnosticTag, _sourceActor, _sourcePlayerActor;
        private bool _showAdvanced, _showDiagnostics;
        private string _validationMessage;
        private MessageType _validationMessageType;

        private void OnEnable()
        {
            _idGeneration = serializedObject.FindProperty("idGeneration");
            _subjectId = serializedObject.FindProperty("subjectId");
            _scope = serializedObject.FindProperty("scope");
            _displayName = serializedObject.FindProperty("displayName");
            _participantDiscovery = serializedObject.FindProperty("participantDiscovery");
            _includeInactiveParticipants = serializedObject.FindProperty("includeInactiveParticipants");
            _includeUnityResettableComponents = serializedObject.FindProperty("includeUnityResettableComponents");
            _registerOnEnable = serializedObject.FindProperty("registerOnEnable");
            _unregisterOnDisable = serializedObject.FindProperty("unregisterOnDisable");
            _retryUntilRuntimeAvailable = serializedObject.FindProperty("retryUntilRuntimeAvailable");
            _runtimeSubjectIdPrefix = serializedObject.FindProperty("runtimeSubjectIdPrefix");
            _diagnosticTag = serializedObject.FindProperty("diagnosticTag");
            _sourceActor = serializedObject.FindProperty("sourceActor");
            _sourcePlayerActor = serializedObject.FindProperty("sourcePlayerActor");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            var adapter = (UnityResetSubjectAdapter)target;

            FrameworkAuthoringInspectorGui.ProductHeader("Reset Subject", string.Empty);

            DrawConfiguration(adapter);
            DrawConfigurationStatus(adapter);
            DrawActions(adapter);
            DrawAdvanced();
            DrawDiagnostics(adapter);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawConfiguration(UnityResetSubjectAdapter adapter)
        {
            FrameworkAuthoringInspectorGui.Section("Configuration");
            EditorGUILayout.PropertyField(_displayName, new GUIContent("Display Name"));
            EditorGUILayout.PropertyField(_scope, ScopeLabel);
            DrawRequiredIdentity();
            EditorGUILayout.PropertyField(_participantDiscovery, ParticipantsLabel);
        }

        private void DrawRequiredIdentity()
        {
            bool hasActorIdentityBridge =
                _sourceActor.objectReferenceValue != null ||
                _sourcePlayerActor.objectReferenceValue != null;
            if (hasActorIdentityBridge)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.LabelField(IdentityLabel, ProvidedByActorValue);
                }

                return;
            }

            if (_idGeneration.intValue != (int)UnityResetSubjectIdGenerationMode.AuthoredStableId)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.LabelField(IdentityLabel, RuntimeGeneratedValue);
                }

                return;
            }

            EditorGUILayout.PropertyField(_subjectId, SubjectIdLabel);
            if (string.IsNullOrWhiteSpace(_subjectId.stringValue))
            {
                EditorGUILayout.HelpBox(
                    "Subject ID is required. Use Generate Missing IDs or author one explicitly.",
                    MessageType.Warning);
            }
        }

        private void DrawConfigurationStatus(UnityResetSubjectAdapter adapter)
        {
            FrameworkAuthoringInspectorGui.Section("Configuration Status");
            FrameworkAuthoringInspectorGui.Status(ResolveIdentityReady() ? "Ready" : "Incomplete");
            EditorGUILayout.LabelField("Participants", CountParticipants(adapter) + " configured");
        }

        private bool ResolveIdentityReady()
        {
            bool hasActorIdentityBridge =
                _sourceActor.objectReferenceValue != null ||
                _sourcePlayerActor.objectReferenceValue != null;
            if (hasActorIdentityBridge)
            {
                return true;
            }

            if (_idGeneration.intValue != (int)UnityResetSubjectIdGenerationMode.AuthoredStableId)
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(_subjectId.stringValue);
        }

        private void DrawActions(UnityResetSubjectAdapter adapter)
        {
            FrameworkAuthoringInspectorGui.Section("Actions");
            using (new EditorGUI.DisabledScope(targets.Length != 1))
            {
                if (GUILayout.Button(new GUIContent(
                        "Generate Missing IDs",
                        targets.Length != 1
                            ? "Disabled for multi-object editing so a generated ID is never copied between Subjects."
                            : string.Empty)))
                {
                    GenerateMissingIds(adapter);
                }

                if (GUILayout.Button("Validate Subject"))
                {
                    ValidateSubject(adapter);
                }
            }

            if (!string.IsNullOrWhiteSpace(_validationMessage))
            {
                EditorGUILayout.HelpBox(_validationMessage, _validationMessageType);
            }
        }

        private void DrawAdvanced()
        {
            _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "Advanced", true);
            if (!_showAdvanced)
            {
                return;
            }

            EditorGUILayout.LabelField("Registration", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_registerOnEnable);
            EditorGUILayout.PropertyField(_unregisterOnDisable);
            EditorGUILayout.PropertyField(_retryUntilRuntimeAvailable);

            EditorGUILayout.LabelField("Identity Mode", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(
                _idGeneration,
                new GUIContent(
                    "Generation",
                    "AuthoredStableId authors the Subject ID above. RuntimeInstanceId generates identity automatically using the prefix below."));
            EditorGUILayout.PropertyField(_runtimeSubjectIdPrefix, new GUIContent("Runtime ID Prefix"));

            EditorGUILayout.LabelField("Actor Identity Bridge", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(
                _sourceActor,
                new GUIContent(
                    "Source Actor",
                    "Takes precedence over authored Subject ID when it provides the selected identity mode. Conflicting actor identities are rejected by runtime registration."));
            EditorGUILayout.PropertyField(
                _sourcePlayerActor,
                new GUIContent(
                    "Source Player Actor",
                    "Takes precedence over authored Subject ID when it provides the selected identity mode. Conflicting actor identities are rejected by runtime registration."));

            EditorGUILayout.LabelField("Participant Discovery Details", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_includeInactiveParticipants);
            EditorGUILayout.PropertyField(_includeUnityResettableComponents);
            EditorGUILayout.PropertyField(_diagnosticTag);
        }

        private void DrawDiagnostics(UnityResetSubjectAdapter adapter)
        {
            _showDiagnostics = EditorGUILayout.Foldout(_showDiagnostics, "Diagnostics", true);
            if (!_showDiagnostics)
            {
                return;
            }

            EditorGUILayout.LabelField(
                "Resolved Subject ID",
                adapter.SubjectId.IsValid ? adapter.SubjectId.StableText : "Not resolved");
            EditorGUILayout.LabelField(
                "Registration",
                Application.isPlaying ? (adapter.IsRegistered ? "Registered" : "Not registered") : "Runtime-dependent");
            EditorGUILayout.LabelField("Registered Participants", adapter.RegisteredParticipantCount.ToString());
            EditorGUILayout.LabelField("Runtime Port", adapter.ResetRegistrationRuntimeBindingStatus);
            if (!string.IsNullOrWhiteSpace(adapter.ResetRegistrationRuntimeBindingDiagnostic))
            {
                EditorGUILayout.LabelField(
                    adapter.ResetRegistrationRuntimeBindingDiagnostic,
                    EditorStyles.wordWrappedMiniLabel);
            }
        }

        private int CountParticipants(UnityResetSubjectAdapter adapter) =>
            _participantDiscovery.intValue == (int)UnityResetParticipantDiscoveryMode.SameGameObject
                ? adapter.GetComponents<UnityResetParticipantBehaviour>().Length
                : adapter.GetComponentsInChildren<UnityResetParticipantBehaviour>(_includeInactiveParticipants.boolValue).Length;

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
