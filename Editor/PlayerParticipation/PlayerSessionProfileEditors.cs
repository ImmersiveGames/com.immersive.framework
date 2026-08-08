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
        private SerializedProperty supportedSlots;
        private SerializedProperty initialCapacity;
        private SerializedProperty initialJoiningOpen;
        private SerializedProperty playerProvisioningProfile;
        private ReorderableList supportedSlotsList;
        private bool showAdvanced;

        private void OnEnable()
        {
            supportedSlots = serializedObject.FindProperty("supportedSlots");
            initialCapacity = serializedObject.FindProperty("initialCapacity");
            initialJoiningOpen = serializedObject.FindProperty("initialJoiningOpen");
            playerProvisioningProfile =
                serializedObject.FindProperty("playerProvisioningProfile");

            supportedSlotsList = new ReorderableList(
                serializedObject,
                supportedSlots,
                true,
                true,
                true,
                true)
            {
                drawHeaderCallback = rect =>
                    EditorGUI.LabelField(
                        rect,
                        "Supported Slots — Allocation / Join Order"),
                elementHeight = EditorGUIUtility.singleLineHeight + 4f,
                drawElementCallback = (rect, index, active, focused) =>
                {
                    rect.y += 2f;
                    rect.height = EditorGUIUtility.singleLineHeight;
                    EditorGUI.PropertyField(
                        rect,
                        supportedSlots.GetArrayElementAtIndex(index),
                        new GUIContent($"{index + 1}."));
                }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            supportedSlotsList?.DoLayoutList();
            EditorGUILayout.LabelField(
                "Allocation and Join use this exact order. Player Slot display order is presentation-only.",
                EditorStyles.miniLabel);

            DrawSection("Initial Session State");
            initialCapacity.intValue = EditorGUILayout.IntField(
                new GUIContent(
                    "Initial Capacity",
                    "Initial runtime capacity. Later changes use runtime Session commands."),
                initialCapacity.intValue);
            initialJoiningOpen.boolValue = EditorGUILayout.Toggle(
                new GUIContent(
                    "Initial Joining Open",
                    "Whether Joining begins open. Later changes use runtime Session commands."),
                initialJoiningOpen.boolValue);

            DrawSection("Provisioning");
            playerProvisioningProfile.objectReferenceValue =
                EditorGUILayout.ObjectField(
                new GUIContent(
                    "Player Provisioning Profile (Required)",
                    "Host provisioning and Actor resolution intent used to initialize this Session."),
                playerProvisioningProfile.objectReferenceValue,
                typeof(PlayerProvisioningProfile),
                false);

            serializedObject.ApplyModifiedProperties();
            PlayerSessionInspectorGui.DrawValidation((PlayerSessionProfile)target);

            EditorGUILayout.Space(7f);
            showAdvanced = EditorGUILayout.Foldout(
                showAdvanced,
                new GUIContent(
                    "Advanced / Debug",
                    "Read-only effective configuration preview. It does not create or change runtime state."),
                true);
            if (showAdvanced)
            {
                PlayerSessionInspectorGui.DrawResolution(
                    (PlayerSessionProfile)target,
                    includeHeader: true);
            }
        }

        private static void DrawSection(string title)
        {
            EditorGUILayout.Space(7f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }
    }

    [CustomEditor(typeof(PlayerProvisioningProfile))]
    internal sealed class PlayerProvisioningProfileEditor : UnityEditor.Editor
    {
        private SerializedProperty defaultHostProvisioning;
        private SerializedProperty slotOverrides;
        private SerializedProperty actorResolutionPolicy;
        private ReorderableList slotOverridesList;
        private bool showAdvanced;

        private void OnEnable()
        {
            defaultHostProvisioning =
                serializedObject.FindProperty("defaultHostProvisioning");
            slotOverrides = serializedObject.FindProperty("slotOverrides");
            actorResolutionPolicy =
                serializedObject.FindProperty("actorResolutionPolicy");

            slotOverridesList = new ReorderableList(
                serializedObject,
                slotOverrides,
                true,
                true,
                true,
                true)
            {
                drawHeaderCallback = rect =>
                    EditorGUI.LabelField(rect, "Slot Overrides (Explicit)"),
                elementHeight = EditorGUIUtility.singleLineHeight + 4f,
                drawElementCallback = (rect, index, active, focused) =>
                {
                    SerializedProperty element =
                        slotOverrides.GetArrayElementAtIndex(index);
                    SerializedProperty slot =
                        element.FindPropertyRelative("playerSlotProfile");
                    SerializedProperty mode =
                        element.FindPropertyRelative("hostProvisioningMode");
                    rect.y += 2f;
                    rect.height = EditorGUIUtility.singleLineHeight;
                    float slotWidth = rect.width * 0.56f;
                    EditorGUI.PropertyField(
                        new Rect(rect.x, rect.y, slotWidth - 3f, rect.height),
                        slot,
                        GUIContent.none);
                    EditorGUI.PropertyField(
                        new Rect(rect.x + slotWidth, rect.y, rect.width - slotWidth, rect.height),
                        mode,
                        GUIContent.none);
                }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            DrawSection("Host Provisioning");
            defaultHostProvisioning.intValue = System.Convert.ToInt32(
                EditorGUILayout.EnumPopup(
                    new GUIContent(
                        "Default Host Provisioning",
                        "Used by every Slot without an explicit override."),
                    (PlayerHostProvisioningMode)defaultHostProvisioning.intValue));

            EditorGUILayout.LabelField(
                "Overrides replace the default for their Slot; they never act as a fallback.",
                EditorStyles.miniLabel);
            slotOverridesList?.DoLayoutList();

            DrawSection("Actor Resolution");
            actorResolutionPolicy.intValue = System.Convert.ToInt32(
                EditorGUILayout.EnumPopup(
                    new GUIContent(
                        "Actor Resolution Policy",
                        "Resolve the Slot Default Actor or leave the Actor explicitly unresolved."),
                    (PlayerActorResolutionPolicy)actorResolutionPolicy.intValue));

            serializedObject.ApplyModifiedProperties();
            PlayerSessionInspectorGui.DrawValidation(
                (PlayerProvisioningProfile)target);

            EditorGUILayout.Space(7f);
            showAdvanced = EditorGUILayout.Foldout(
                showAdvanced,
                new GUIContent(
                    "Advanced / Debug",
                    "Read-only authored override evidence."),
                true);
            if (showAdvanced)
            {
                PlayerSessionInspectorGui.DrawProvisioningEvidence(
                    (PlayerProvisioningProfile)target);
            }
        }

        private static void DrawSection(string title)
        {
            EditorGUILayout.Space(7f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }
    }

    internal static class PlayerSessionInspectorGui
    {
        internal static void DrawValidation(PlayerSessionProfile profile)
        {
            if (profile == null)
            {
                DrawValidationTitle();
                EditorGUILayout.HelpBox(
                    "Player Session Profile is missing.",
                    MessageType.Error);
                return;
            }

            if (!profile.TryValidate(out string authoredIssue))
            {
                DrawValidationTitle();
                EditorGUILayout.HelpBox(authoredIssue, MessageType.Error);
                return;
            }

            PlayerSessionInitializationResult resolution =
                PlayerSessionConfigurationResolver.Resolve(profile);
            if (!resolution.Succeeded)
            {
                DrawValidationTitle();
                EditorGUILayout.HelpBox(
                    $"Effective configuration is invalid ({resolution.Failure}). {resolution.Message}",
                    MessageType.Error);
                return;
            }

            DrawValidationTitle();
            DrawValidationStatus(
                "Valid — resolves to an immutable initial Session configuration.");
        }

        internal static void DrawValidation(PlayerProvisioningProfile profile)
        {
            if (profile == null)
            {
                DrawValidationTitle();
                EditorGUILayout.HelpBox(
                    "Player Provisioning Profile is missing.",
                    MessageType.Error);
                return;
            }

            if (!profile.TryValidate(out string issue))
            {
                DrawValidationTitle();
                EditorGUILayout.HelpBox(issue, MessageType.Error);
                return;
            }

            DrawValidationTitle();
            DrawValidationStatus(
                "Valid — Supported Slot membership is validated by Player Session resolution.");
        }

        internal static void DrawResolution(
            PlayerSessionProfile profile,
            bool includeHeader)
        {
            if (includeHeader)
            {
                EditorGUILayout.Space(5f);
                EditorGUILayout.LabelField(
                    "Effective Initial Configuration",
                    EditorStyles.boldLabel);
            }

            if (profile == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a Player Session Profile to preview its effective configuration.",
                    MessageType.Info);
                return;
            }

            PlayerSessionInitializationResult resolution =
                PlayerSessionConfigurationResolver.Resolve(profile);
            if (!resolution.Succeeded)
            {
                EditorGUILayout.HelpBox(
                    $"Resolution failed ({resolution.Failure}). {resolution.Message}",
                    MessageType.Error);
                return;
            }

            EffectivePlayerSessionConfiguration configuration =
                resolution.Configuration;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField(
                    "Initial Capacity",
                    configuration.InitialCapacity);
                EditorGUILayout.Toggle(
                    "Initial Joining Open",
                    configuration.InitialJoiningOpen);
                EditorGUILayout.EnumPopup(
                    "Actor Resolution Policy",
                    configuration.ActorResolutionPolicy);
            }

            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField(
                "Effective Slot Order",
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
                    $"{slot.HostProvisioningMode} — {provenance}");
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField(
                        "    Default Actor (Captured)",
                        slot.DefaultActorProfile,
                        typeof(ActorProfile),
                        false);
                }
            }
        }

        internal static void DrawProvisioningEvidence(
            PlayerProvisioningProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.EnumPopup(
                    "Default Host Provisioning",
                    profile.DefaultHostProvisioning);
                EditorGUILayout.EnumPopup(
                    "Actor Resolution Policy",
                    profile.ActorResolutionPolicy);
            }

            IReadOnlyList<PlayerSlotProvisioningOverride> overrides =
                profile.SlotOverrides;
            EditorGUILayout.LabelField(
                "Explicit Overrides",
                EditorStyles.miniBoldLabel);
            for (int index = 0; index < overrides.Count; index++)
            {
                PlayerSlotProvisioningOverride slotOverride = overrides[index];
                string slotName = slotOverride?.PlayerSlotProfile != null
                    ? slotOverride.PlayerSlotProfile.name
                    : "<missing Slot>";
                string mode = slotOverride != null
                    ? slotOverride.HostProvisioningMode.ToString()
                    : "<missing Mode>";
                EditorGUILayout.LabelField($"{index + 1}. {slotName}", mode);
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

        private static void DrawValidationTitle()
        {
            EditorGUILayout.Space(7f);
            EditorGUILayout.LabelField(
                "Validation Summary",
                EditorStyles.boldLabel);
        }

        private static void DrawValidationStatus(string status)
        {
            EditorGUILayout.LabelField(
                "Status",
                status,
                EditorStyles.miniLabel);
        }
    }
}
