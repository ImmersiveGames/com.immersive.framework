using Immersive.Framework.Editor.Common;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.PlayerParticipation
{
    [CustomEditor(typeof(PlayerSessionObserver))]
    internal sealed class PlayerSessionObserverEditor : UnityEditor.Editor
    {
        private SerializedProperty _scope;
        private SerializedProperty _onJoiningOpened;
        private SerializedProperty _onJoiningClosed;
        private SerializedProperty _onPlayerJoined;
        private SerializedProperty _onPlayerLeft;
        private SerializedProperty _onActorSelected;
        private SerializedProperty _onActorChanged;
        private SerializedProperty _onActorCleared;
        private bool _hasValidation;
        private bool _validationIsValid;
        private LocalPlayerProvisioningConsumerScope _validatedScope;
        private string _validationIssue;
        private bool _showAdvanced;

        private void OnEnable()
        {
            if (!HasLiveTargets())
            {
                return;
            }

            _scope = serializedObject.FindProperty("scope");
            _onJoiningOpened = serializedObject.FindProperty("onJoiningOpened");
            _onJoiningClosed = serializedObject.FindProperty("onJoiningClosed");
            _onPlayerJoined = serializedObject.FindProperty("onPlayerJoined");
            _onPlayerLeft = serializedObject.FindProperty("onPlayerLeft");
            _onActorSelected = serializedObject.FindProperty("onActorSelected");
            _onActorChanged = serializedObject.FindProperty("onActorChanged");
            _onActorCleared = serializedObject.FindProperty("onActorCleared");
        }

        public override void OnInspectorGUI()
        {
            if (!HasLiveTargets())
            {
                return;
            }

            serializedObject.UpdateIfRequiredOrScript();
            var observer = (PlayerSessionObserver)target;

            FrameworkAuthoringInspectorGui.ProductHeader(
                "PLAYER SESSION OBSERVER",
                string.Empty);

            DrawConfiguration();

            DrawEvents();

            DrawConfigurationStatus(observer);

            DrawRuntimeStatus(observer);

            _showAdvanced = FrameworkAuthoringInspectorGui.AdvancedFoldout(_showAdvanced);
            if (_showAdvanced)
            {
                DrawAdvanced(observer);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawConfiguration()
        {
            FrameworkAuthoringInspectorGui.Section("Configuration");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(
                _scope,
                new GUIContent(
                    "Scope",
                    "Explicit Route or Activity scope for this read-only observer. Framework Core supplies scoped access directly at runtime."));

            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            serializedObject.ApplyModifiedProperties();
            InvalidateConfigurationStatus();
        }

        private void DrawEvents()
        {
            FrameworkAuthoringInspectorGui.Section("Events");
            EditorGUILayout.LabelField("Joining", EditorStyles.miniBoldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(
                    _onJoiningOpened,
                    new GUIContent("On Joining Opened"));
                EditorGUILayout.PropertyField(
                    _onJoiningClosed,
                    new GUIContent("On Joining Closed"));
            }

            EditorGUILayout.LabelField("Player", EditorStyles.miniBoldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(
                    _onPlayerJoined,
                    new GUIContent("On Player Joined"));
                EditorGUILayout.PropertyField(
                    _onPlayerLeft,
                    new GUIContent("On Player Left"));
            }

            EditorGUILayout.LabelField("Actor", EditorStyles.miniBoldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(
                    _onActorSelected,
                    new GUIContent("On Actor Selected"));
                EditorGUILayout.PropertyField(
                    _onActorChanged,
                    new GUIContent("On Actor Changed"));
                EditorGUILayout.PropertyField(
                    _onActorCleared,
                    new GUIContent("On Actor Cleared"));
            }
        }

        private void DrawConfigurationStatus(PlayerSessionObserver observer)
        {
            FrameworkAuthoringInspectorGui.Section("Configuration Status");
            if (_hasValidation && observer.Scope != _validatedScope)
            {
                InvalidateConfigurationStatus();
            }

            if (GUILayout.Button("Validate Scope"))
            {
                serializedObject.ApplyModifiedProperties();
                _hasValidation = true;
                _validationIsValid = observer.TryValidateConfiguration(
                    out _validationIssue);
                _validatedScope = observer.Scope;
            }

            string state = !_hasValidation
                ? "Not Checked"
                : _validationIsValid ? "Valid" : "Invalid";
            EditorGUILayout.LabelField("Status", state);
            if (_hasValidation && !_validationIsValid)
            {
                EditorGUILayout.LabelField(
                    "Diagnostic",
                    _validationIssue,
                    EditorStyles.wordWrappedMiniLabel);
            }
        }

        private static void DrawRuntimeStatus(PlayerSessionObserver observer)
        {
            FrameworkAuthoringInspectorGui.Section("Runtime Status");
            if (!Application.isPlaying)
            {
                EditorGUILayout.LabelField(
                    "Runtime Evidence",
                    "Available in Play Mode.");
                return;
            }

            EditorGUILayout.LabelField(
                "Binding",
                observer.ScopedAccessState.ToString());

            PlayerSessionScopedObservationSnapshot observation =
                observer.CurrentObservation;
            if (observation == null || observation.Participation == null)
            {
                EditorGUILayout.LabelField(
                    "Session",
                    observer.Availability.ToString());
                EditorGUILayout.LabelField("Joining", "Unavailable");
                EditorGUILayout.LabelField("Players", "Unavailable");
                return;
            }

            PlayerParticipationSnapshot participation = observation.Participation;
            EditorGUILayout.LabelField("Session", "Available");
            EditorGUILayout.LabelField(
                "Joining",
                participation.JoiningOpen ? "Open" : "Closed");
            EditorGUILayout.LabelField(
                "Players",
                $"{participation.JoinedCount} joined / {participation.ConfiguredSlotCount} configured");
        }

        private static void DrawAdvanced(PlayerSessionObserver observer)
        {
            EditorGUILayout.LabelField(
                "Diagnostic",
                observer.Diagnostic,
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField(
                "Initialization Evidence",
                observer.InitializationSummary,
                EditorStyles.wordWrappedMiniLabel);

            PlayerSessionScopedObservationSnapshot observation =
                observer.CurrentObservation;
            if (observation == null)
            {
                return;
            }

            EditorGUILayout.LabelField("Scope Owner", observation.ScopeOwner.StableText);
            EditorGUILayout.LabelField("Activity Occurrence", observation.ActivityOccurrence.ToString());
            EditorGUILayout.LabelField("Session Revision", observation.SessionRevision.ToString());
        }

        private void InvalidateConfigurationStatus()
        {
            _hasValidation = false;
            _validationIsValid = false;
            _validationIssue = string.Empty;
        }

        private bool HasLiveTargets()
        {
            if (target == null || targets == null || targets.Length == 0)
            {
                return false;
            }

            for (int index = 0; index < targets.Length; index++)
            {
                if (targets[index] == null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
