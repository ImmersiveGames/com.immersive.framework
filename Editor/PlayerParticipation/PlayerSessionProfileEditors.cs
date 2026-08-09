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
        private SerializedProperty _initialCapacity;
        private SerializedProperty _initialJoiningOpen;
        private SerializedProperty _playerProvisioningProfile;
        private ReorderableList _supportedSlotsList;
        private bool _showAdvanced;
        private InspectorValidationState _validationState;
        private string _validationMessage = string.Empty;
        private PlayerSessionInitializationResult _lastResolution;

        private void OnEnable()
        {
            _supportedSlots = serializedObject.FindProperty("supportedSlots");
            _initialCapacity = serializedObject.FindProperty("initialCapacity");
            _initialJoiningOpen = serializedObject.FindProperty("initialJoiningOpen");
            _playerProvisioningProfile =
                serializedObject.FindProperty("playerProvisioningProfile");

            _supportedSlotsList = new ReorderableList(
                serializedObject,
                _supportedSlots,
                true,
                true,
                true,
                true)
            {
                drawHeaderCallback = rect =>
                    EditorGUI.LabelField(rect, "Supported Player Slots"),
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

            _supportedSlotsList?.DoLayoutList();

            DrawSection("Initial Session State");
            EditorGUILayout.PropertyField(
                _initialCapacity,
                new GUIContent(
                    "Initial Capacity",
                    "Number of supported Player Slots available when the Session is created."));

            int joiningIndex = _initialJoiningOpen.boolValue ? 1 : 0;
            int nextJoiningIndex = EditorGUILayout.Popup(
                new GUIContent(
                    "Initial Joining",
                    "Whether new Players may join when the Session is created. Later changes use runtime Session commands."),
                joiningIndex,
                new[] { "Closed", "Open" });
            _initialJoiningOpen.boolValue = nextJoiningIndex == 1;

            DrawSection("Player Provisioning");
            EditorGUILayout.PropertyField(
                _playerProvisioningProfile,
                new GUIContent(
                    "Profile",
                    "Required Player Provisioning Profile used when the Session is created."));
            DrawRequiredReferenceStatus(_playerProvisioningProfile);

            bool guiChanged = EditorGUI.EndChangeCheck();
            bool propertiesApplied = serializedObject.ApplyModifiedProperties();
            if (guiChanged || propertiesApplied)
            {
                ClearValidation();
            }

            DrawSection("Product Actions");
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
                SetInvalid(
                    $"{resolution.Failure}: {resolution.Message}");
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
            if (!_showAdvanced)
            {
                return;
            }

            PlayerSessionInspectorGui.DrawSessionEvidence(
                (PlayerSessionProfile)target,
                _lastResolution);
        }

        private void SetInvalid(string message)
        {
            _validationState = InspectorValidationState.Invalid;
            _validationMessage = message ?? string.Empty;
            if (_lastResolution != null && _lastResolution.Succeeded)
            {
                _lastResolution = null;
            }
        }

        private void ClearValidation()
        {
            _validationState = InspectorValidationState.NotValidated;
            _validationMessage = string.Empty;
            _lastResolution = null;
        }

        private static void DrawRequiredReferenceStatus(
            SerializedProperty property)
        {
            if (property.objectReferenceValue != null)
            {
                return;
            }

            EditorGUILayout.LabelField(
                "Not Configured — assign a Player Provisioning Profile.",
                EditorStyles.wordWrappedMiniLabel);
        }

        private static void DrawSection(string title)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }
    }

    [CustomEditor(typeof(PlayerProvisioningProfile))]
    internal sealed class PlayerProvisioningProfileEditor : UnityEditor.Editor
    {
        private SerializedProperty _defaultHostProvisioning;
        private SerializedProperty _slotOverrides;
        private SerializedProperty _actorResolutionPolicy;
        private ReorderableList _slotOverridesList;
        private bool _showAdvanced;
        private InspectorValidationState _validationState;
        private string _validationMessage = string.Empty;

        private void OnEnable()
        {
            _defaultHostProvisioning =
                serializedObject.FindProperty("defaultHostProvisioning");
            _slotOverrides = serializedObject.FindProperty("slotOverrides");
            _actorResolutionPolicy =
                serializedObject.FindProperty("actorResolutionPolicy");

            _slotOverridesList = new ReorderableList(
                serializedObject,
                _slotOverrides,
                true,
                true,
                true,
                true)
            {
                drawHeaderCallback = rect =>
                    EditorGUI.LabelField(rect, "Slot Overrides"),
                elementHeight = EditorGUIUtility.singleLineHeight + 4f,
                drawElementCallback = (rect, index, active, focused) =>
                {
                    SerializedProperty element =
                        _slotOverrides.GetArrayElementAtIndex(index);
                    SerializedProperty slot =
                        element.FindPropertyRelative("playerSlotProfile");
                    SerializedProperty mode =
                        element.FindPropertyRelative("hostProvisioningMode");

                    rect.y += 2f;
                    rect.height = EditorGUIUtility.singleLineHeight;
                    float slotWidth = rect.width * 0.58f;

                    EditorGUI.PropertyField(
                        new Rect(
                            rect.x,
                            rect.y,
                            slotWidth - 3f,
                            rect.height),
                        slot,
                        GUIContent.none);
                    EditorGUI.PropertyField(
                        new Rect(
                            rect.x + slotWidth,
                            rect.y,
                            rect.width - slotWidth,
                            rect.height),
                        mode,
                        GUIContent.none);
                },
                onAddCallback = list =>
                {
                    int index = _slotOverrides.arraySize;
                    _slotOverrides.InsertArrayElementAtIndex(index);
                    SerializedProperty element =
                        _slotOverrides.GetArrayElementAtIndex(index);
                    element.FindPropertyRelative("playerSlotProfile")
                        .objectReferenceValue = null;
                    element.FindPropertyRelative("hostProvisioningMode")
                        .enumValueIndex = 0;
                }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            DrawSection("Host Provisioning");
            EditorGUILayout.PropertyField(
                _defaultHostProvisioning,
                new GUIContent(
                    "Default",
                    "Host provisioning used by supported Player Slots that do not have an explicit override."));
            _slotOverridesList?.DoLayoutList();

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

            DrawSection("Product Actions");
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
            PlayerProvisioningProfile profile =
                (PlayerProvisioningProfile)target;
            if (!profile.TryValidate(out string issue))
            {
                _validationState = InspectorValidationState.Invalid;
                _validationMessage = issue ?? string.Empty;
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
            if (!_showAdvanced)
            {
                return;
            }

            PlayerSessionInspectorGui.DrawProvisioningEvidence(
                (PlayerProvisioningProfile)target);
        }

        private void ClearValidation()
        {
            _validationState = InspectorValidationState.NotValidated;
            _validationMessage = string.Empty;
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
                EditorGUILayout.LabelField(
                    "Status",
                    "No Player Session Profile");
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

            EffectivePlayerSessionConfiguration configuration =
                resolution.Configuration;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField(
                    "Initial Capacity",
                    configuration.InitialCapacity);
                EditorGUILayout.TextField(
                    "Initial Joining",
                    configuration.InitialJoiningOpen ? "Open" : "Closed");
                EditorGUILayout.EnumPopup(
                    "Actor Resolution",
                    configuration.ActorResolutionPolicy);
            }

            IReadOnlyList<EffectivePlayerSlotProvisioning> slots =
                configuration.Slots;
            for (int index = 0; index < slots.Count; index++)
            {
                EffectivePlayerSlotProvisioning slot = slots[index];
                string provenance = IsOverride(
                    profile.PlayerProvisioningProfile,
                    slot.PlayerSlotId)
                    ? "Slot Override"
                    : "Profile Default";

                EditorGUILayout.LabelField(
                    $"{index + 1}. {GetSlotName(slot)}",
                    $"{slot.HostProvisioningMode} — {provenance}");
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

        internal static void DrawSessionEvidence(
            PlayerSessionProfile profile,
            PlayerSessionInitializationResult resolution)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    "Asset Path",
                    AssetDatabase.GetAssetPath(profile));
            }

            if (resolution == null)
            {
                EditorGUILayout.LabelField("Resolution", "Not Validated");
                return;
            }

            if (!resolution.Succeeded)
            {
                EditorGUILayout.LabelField(
                    "Resolution",
                    resolution.Failure.ToString());
                EditorGUILayout.LabelField(
                    resolution.Message,
                    EditorStyles.wordWrappedMiniLabel);
                return;
            }

            EffectivePlayerSessionConfiguration configuration =
                resolution.Configuration;

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Effective Initial Configuration",
                EditorStyles.miniBoldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField(
                    "Capacity",
                    configuration.InitialCapacity);
                EditorGUILayout.TextField(
                    "Joining",
                    configuration.InitialJoiningOpen ? "Open" : "Closed");
                EditorGUILayout.EnumPopup(
                    "Actor Resolution",
                    configuration.ActorResolutionPolicy);
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Resolved Player Slots",
                EditorStyles.miniBoldLabel);

            for (int index = 0; index < configuration.Slots.Count; index++)
            {
                EffectivePlayerSlotProvisioning slot =
                    configuration.Slots[index];
                string provenance = IsOverride(
                    profile.PlayerProvisioningProfile,
                    slot.PlayerSlotId)
                    ? "Slot Override"
                    : "Profile Default";

                EditorGUILayout.LabelField(
                    $"{index + 1}. {GetSlotName(slot)}",
                    slot.PlayerSlotId.StableText);
                EditorGUILayout.LabelField(
                    "    Host Provisioning",
                    $"{slot.HostProvisioningMode} — {provenance}");

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

        internal static void DrawProvisioningEvidence(
            PlayerProvisioningProfile profile)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    "Asset Path",
                    AssetDatabase.GetAssetPath(profile));
                EditorGUILayout.EnumPopup(
                    "Default Host Provisioning",
                    profile.DefaultHostProvisioning);
                EditorGUILayout.EnumPopup(
                    "Actor Resolution Policy",
                    profile.ActorResolutionPolicy);
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Resolved Override Identity",
                EditorStyles.miniBoldLabel);

            IReadOnlyList<PlayerSlotProvisioningOverride> overrides =
                profile.SlotOverrides;
            if (overrides.Count == 0)
            {
                EditorGUILayout.LabelField("None", EditorStyles.miniLabel);
                return;
            }

            for (int index = 0; index < overrides.Count; index++)
            {
                PlayerSlotProvisioningOverride slotOverride = overrides[index];
                if (slotOverride == null)
                {
                    EditorGUILayout.LabelField(
                        $"{index + 1}. <null override>",
                        EditorStyles.miniLabel);
                    continue;
                }

                PlayerSlotProfile slotProfile =
                    slotOverride.PlayerSlotProfile;
                if (slotProfile == null)
                {
                    EditorGUILayout.LabelField(
                        $"{index + 1}. <missing Player Slot>",
                        slotOverride.HostProvisioningMode.ToString());
                    continue;
                }

                string identity = slotProfile.TryGetPlayerSlotId(
                    out PlayerSlotId playerSlotId,
                    out string issue)
                    ? playerSlotId.StableText
                    : $"Invalid: {issue}";

                EditorGUILayout.LabelField(
                    $"{index + 1}. {slotProfile.name}",
                    identity);
                EditorGUILayout.LabelField(
                    "    Host Provisioning",
                    slotOverride.HostProvisioningMode.ToString());
            }
        }

        private static string GetSlotName(EffectivePlayerSlotProvisioning slot)
        {
            return slot.PlayerSlotProfile != null
                ? slot.PlayerSlotProfile.name
                : slot.PlayerSlotId.StableText;
        }

        private static bool IsOverride(
            PlayerProvisioningProfile provisioningProfile,
            PlayerSlotId playerSlotId)
        {
            if (provisioningProfile == null)
            {
                return false;
            }

            IReadOnlyList<PlayerSlotProvisioningOverride> overrides =
                provisioningProfile.SlotOverrides;
            for (int index = 0; index < overrides.Count; index++)
            {
                PlayerSlotProvisioningOverride slotOverride = overrides[index];
                if (slotOverride != null &&
                    slotOverride.PlayerSlotProfile != null &&
                    slotOverride.PlayerSlotProfile.TryGetPlayerSlotId(
                        out PlayerSlotId overrideSlotId,
                        out _) &&
                    overrideSlotId == playerSlotId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
