using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.PlayerParticipation
{
    [CustomEditor(typeof(PlayerSessionProfile))]
    internal sealed class PlayerSessionProfileEditor : UnityEditor.Editor
    {
        private SerializedProperty _supportedSlots;
        private SerializedProperty _initialJoiningOpen;
        private SerializedProperty _hostProvisioning;
        private SerializedProperty _actorResolutionPolicy;
        private ReorderableList _supportedSlotsList;
        private bool _showAdvanced;
        private InspectorValidationState _validationState;
        private string _validationMessage = string.Empty;
        private PlayerSessionInitializationResult _lastResolution;

        private void OnEnable()
        {
            _supportedSlots = serializedObject.FindProperty("supportedSlots");
            _initialJoiningOpen = serializedObject.FindProperty("initialJoiningOpen");
            _hostProvisioning = serializedObject.FindProperty("hostProvisioning");
            _actorResolutionPolicy =
                serializedObject.FindProperty("actorResolutionPolicy");

            _supportedSlotsList = new ReorderableList(
                serializedObject,
                _supportedSlots,
                true,
                true,
                true,
                true)
            {
                drawHeaderCallback = rect =>
                    EditorGUI.LabelField(rect, "Supported Slots"),
                elementHeight = EditorGUIUtility.singleLineHeight + 4f,
                drawElementCallback = (rect, index, active, focused) =>
                {
                    rect.y += 2f;
                    rect.height = EditorGUIUtility.singleLineHeight;
                    EditorGUI.PropertyField(
                        rect,
                        _supportedSlots.GetArrayElementAtIndex(index),
                        new GUIContent($"{index + 1}."));
                },
                onAddCallback = list =>
                {
                    int index = _supportedSlots.arraySize;
                    _supportedSlots.InsertArrayElementAtIndex(index);
                    _supportedSlots
                        .GetArrayElementAtIndex(index)
                        .objectReferenceValue = null;
                }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            DrawSection("Primary Intent");
            _supportedSlotsList?.DoLayoutList();

            DrawSection("Initial Joining");
            int joiningIndex = _initialJoiningOpen.boolValue ? 1 : 0;
            int nextJoiningIndex = EditorGUILayout.Popup(
                new GUIContent(
                    "Initial Joining",
                    "Whether new Players may join when the Session is created. Later changes use runtime Session commands."),
                joiningIndex,
                new[] { "Closed", "Open" });
            _initialJoiningOpen.boolValue = nextJoiningIndex == 1;

            DrawSection("Host Provisioning");
            EditorGUILayout.PropertyField(
                _hostProvisioning,
                new GUIContent(
                    "Mode",
                    "Host provisioning applied uniformly to every Supported Slot."));

            DrawSection("Actor Resolution");
            EditorGUILayout.PropertyField(
                _actorResolutionPolicy,
                new GUIContent(
                    "Policy",
                    "Initial Actor resolution intent used when the Session is created."));

            bool guiChanged = EditorGUI.EndChangeCheck();
            bool propertiesApplied = serializedObject.ApplyModifiedProperties();
            if (guiChanged || propertiesApplied)
            {
                ClearValidation();
            }

            if (GUILayout.Button("Validate"))
            {
                RunValidation();
            }

            DrawSection("Validation Summary");
            PlayerSessionInspectorGui.DrawValidationSummary(
                _validationState,
                _validationMessage);

            DrawAdvanced();
        }

        private void RunValidation()
        {
            PlayerSessionProfile profile = (PlayerSessionProfile)target;
            if (!profile.TryValidate(out string authoredIssue))
            {
                SetInvalid(authoredIssue);
                return;
            }

            PlayerSessionInitializationResult resolution =
                PlayerSessionConfigurationResolver.Resolve(profile);
            _lastResolution = resolution;
            if (!resolution.Succeeded)
            {
                SetInvalid($"{resolution.Failure}: {resolution.Message}");
                return;
            }

            _validationState = InspectorValidationState.Valid;
            _validationMessage = string.Empty;
        }

        private void DrawAdvanced()
        {
            EditorGUILayout.Space(6f);
            _showAdvanced = EditorGUILayout.Foldout(
                _showAdvanced,
                "Advanced / Debug",
                true);
            if (_showAdvanced)
            {
                PlayerSessionInspectorGui.DrawSessionEvidence(
                    (PlayerSessionProfile)target,
                    _lastResolution);
            }
        }

        private void SetInvalid(string message)
        {
            _validationState = InspectorValidationState.Invalid;
            _validationMessage = message ?? string.Empty;
            _lastResolution = null;
        }

        private void ClearValidation()
        {
            _validationState = InspectorValidationState.NotValidated;
            _validationMessage = string.Empty;
            _lastResolution = null;
        }

        private static void DrawSection(string title)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }
    }

    internal enum InspectorValidationState
    {
        NotValidated = 0,
        Valid = 10,
        Invalid = 20
    }

    internal static class PlayerSessionInspectorGui
    {
        internal static void DrawValidationSummary(
            InspectorValidationState state,
            string message)
        {
            EditorGUILayout.LabelField("Scope", "Selected Definition");
            switch (state)
            {
                case InspectorValidationState.Valid:
                    EditorGUILayout.LabelField("Status", "Valid");
                    break;
                case InspectorValidationState.Invalid:
                    EditorGUILayout.LabelField("Status", "Invalid");
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        EditorGUILayout.LabelField(
                            message,
                            EditorStyles.wordWrappedMiniLabel);
                    }

                    break;
                default:
                    EditorGUILayout.LabelField("Status", "Not Validated");
                    break;
            }
        }

        internal static void DrawResolution(
            PlayerSessionProfile profile,
            bool includeHeader)
        {
            if (includeHeader)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField(
                    "Effective Initial Configuration",
                    EditorStyles.miniBoldLabel);
            }

            if (profile == null)
            {
                EditorGUILayout.LabelField("Status", "No Player Session Profile");
                return;
            }

            PlayerSessionInitializationResult resolution =
                PlayerSessionConfigurationResolver.Resolve(profile);
            if (!resolution.Succeeded)
            {
                EditorGUILayout.LabelField(
                    "Status",
                    $"Resolution Failed ({resolution.Failure})");
                if (!string.IsNullOrWhiteSpace(resolution.Message))
                {
                    EditorGUILayout.LabelField(
                        resolution.Message,
                        EditorStyles.wordWrappedMiniLabel);
                }

                return;
            }

            DrawConfiguration(resolution.Configuration);
        }

        internal static void DrawSessionEvidence(
            PlayerSessionProfile profile,
            PlayerSessionInitializationResult resolution)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Asset Path", AssetDatabase.GetAssetPath(profile));
            }

            if (resolution == null)
            {
                EditorGUILayout.LabelField("Resolution", "Not Validated");
                return;
            }

            if (!resolution.Succeeded)
            {
                EditorGUILayout.LabelField("Resolution", resolution.Failure.ToString());
                EditorGUILayout.LabelField(
                    resolution.Message,
                    EditorStyles.wordWrappedMiniLabel);
                return;
            }

            DrawConfiguration(resolution.Configuration);
        }

        private static void DrawConfiguration(
            EffectivePlayerSessionConfiguration configuration)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Effective Initial Configuration",
                EditorStyles.miniBoldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    "Initial Joining",
                    configuration.InitialJoiningOpen ? "Open" : "Closed");
                EditorGUILayout.EnumPopup(
                    "Host Provisioning",
                    configuration.HostProvisioning);
                EditorGUILayout.EnumPopup(
                    "Actor Resolution",
                    configuration.ActorResolutionPolicy);
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Resolved Player Slots", EditorStyles.miniBoldLabel);
            IReadOnlyList<EffectivePlayerSlotProvisioning> slots = configuration.Slots;
            for (int index = 0; index < slots.Count; index++)
            {
                EffectivePlayerSlotProvisioning slot = slots[index];
                EditorGUILayout.LabelField(
                    $"{index + 1}. {GetSlotName(slot)}",
                    slot.PlayerSlotId.StableText);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField(
                        "    Default Actor",
                        slot.DefaultActorProfile,
                        typeof(ActorProfile),
                        false);
                }
            }
        }

        private static string GetSlotName(EffectivePlayerSlotProvisioning slot)
        {
            return slot.PlayerSlotProfile != null
                ? slot.PlayerSlotProfile.name
                : slot.PlayerSlotId.StableText;
        }
    }
}
