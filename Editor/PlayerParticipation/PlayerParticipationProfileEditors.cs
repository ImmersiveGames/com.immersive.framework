using System;
using System.Globalization;
using System.Text;
using Immersive.Framework.Actors;
using Immersive.Framework.Editor.Validation;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;
namespace Immersive.Framework.Editor.PlayerParticipation
{
    [CustomEditor(typeof(PlayerSlotProfile))]
    internal sealed class PlayerSlotProfileEditor : UnityEditor.Editor
    {
        private const float PreviewSize = 72f;
        private const float PreviewHeight = 78f;
        private const float PreviewPadding = 6f;

        private SerializedProperty _playerSlotId;
        private SerializedProperty _displayName;
        private SerializedProperty _description;
        private SerializedProperty _accentColor;
        private SerializedProperty _icon;
        private SerializedProperty _displayOrder;
        private SerializedProperty _defaultActorProfile;

        private bool _showAdvanced;
        private bool _hasValidationResult;
        private FrameworkAuthoringValidationReport _validationReport;

        private void OnEnable()
        {
            _playerSlotId = serializedObject.FindProperty("playerSlotId");
            _displayName = serializedObject.FindProperty("displayName");
            _description = serializedObject.FindProperty("description");
            _accentColor = serializedObject.FindProperty("accentColor");
            _icon = serializedObject.FindProperty("icon");
            _displayOrder = serializedObject.FindProperty("displayOrder");
            _defaultActorProfile = serializedObject.FindProperty("defaultActorProfile");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            DrawPlayerSlot();
            DrawPresentation();
            DrawActorSelection();
            bool normalAuthoringChanged = EditorGUI.EndChangeCheck();

            bool normalPropertiesApplied = serializedObject.ApplyModifiedProperties();
            if (normalAuthoringChanged || normalPropertiesApplied)
            {
                ClearValidationResult();
            }

            DrawProductActions();
            DrawValidationSummary();
            DrawAdvancedSection();
        }

        private void DrawPlayerSlot()
        {
            DrawSection("Player Slot");

            EditorGUILayout.PropertyField(
                _displayName,
                new GUIContent(
                    "Display Name",
                    "Designer-facing name used to present this local participation Slot."));

            EditorGUILayout.PropertyField(
                _description,
                new GUIContent(
                    "Description",
                    "Optional designer-facing description explaining the purpose of this Slot."));
        }

        private void DrawPresentation()
        {
            DrawSection("Presentation");

            EditorGUILayout.PropertyField(
                _icon,
                new GUIContent(
                    "Icon",
                    "Optional Sprite used to recognize this Slot in selection and diagnostic surfaces."));

            _accentColor.colorValue = EditorGUILayout.ColorField(
                new GUIContent(
                    "Accent Color",
                    "Presentation color associated with this Slot."),
                _accentColor.colorValue);

            EditorGUILayout.PropertyField(
                _displayOrder,
                new GUIContent(
                    "Display Order",
                    "Controls presentation sorting only. Game Application Slot order controls default local allocation."));

            DrawIconPreview();
        }

        private void DrawActorSelection()
        {
            DrawSection("Actor Selection");

            _defaultActorProfile.objectReferenceValue = EditorGUILayout.ObjectField(
                new GUIContent(
                    "Default Actor",
                    "Optional Actor selected by default after this Slot joins. An Activity or explicit selection flow may provide another Actor later."),
                _defaultActorProfile.objectReferenceValue,
                typeof(ActorProfile),
                false);
        }

        private void DrawIconPreview()
        {
            Rect rowRect = EditorGUILayout.GetControlRect(false, PreviewHeight);
            Rect frameRect = new Rect(
                rowRect.x,
                rowRect.y + 3f,
                PreviewSize,
                PreviewSize);

            GUI.Box(frameRect, GUIContent.none);

            Rect backgroundRect = new Rect(
                frameRect.x + 1f,
                frameRect.y + 1f,
                frameRect.width - 2f,
                frameRect.height - 2f);
            Color backgroundColor = _accentColor.colorValue;
            backgroundColor.a = Mathf.Clamp(backgroundColor.a, 0.35f, 1f);
            EditorGUI.DrawRect(backgroundRect, backgroundColor);

            Sprite sprite = _icon.objectReferenceValue as Sprite;
            Rect contentRect = new Rect(
                frameRect.x + PreviewPadding,
                frameRect.y + PreviewPadding,
                frameRect.width - (PreviewPadding * 2f),
                frameRect.height - (PreviewPadding * 2f));

            if (sprite != null)
            {
                Texture2D previewTexture = AssetPreview.GetAssetPreview(sprite);
                if (previewTexture == null)
                {
                    previewTexture = AssetPreview.GetMiniThumbnail(sprite);
                }

                if (previewTexture != null)
                {
                    GUI.DrawTexture(
                        contentRect,
                        previewTexture,
                        ScaleMode.ScaleToFit,
                        true);
                }
                else
                {
                    DrawCenteredPreviewLabel(contentRect, "Loading…");
                }

                if (AssetPreview.IsLoadingAssetPreview(sprite.GetEntityId()))
                {
                    Repaint();
                }
            }
            else
            {
                DrawCenteredPreviewLabel(contentRect, "No Icon");
            }

            float textX = frameRect.xMax + 10f;
            float textWidth = Mathf.Max(0f, rowRect.xMax - textX);
            string displayName = string.IsNullOrWhiteSpace(_displayName.stringValue)
                ? target.name
                : _displayName.stringValue.Trim();
            string slotId = string.IsNullOrWhiteSpace(_playerSlotId.stringValue)
                ? "No Player Slot ID"
                : _playerSlotId.stringValue.Trim();
            string iconName = sprite != null ? sprite.name : "No icon assigned";

            GUI.Label(
                new Rect(textX, rowRect.y + 9f, textWidth, EditorGUIUtility.singleLineHeight),
                displayName,
                EditorStyles.boldLabel);
            GUI.Label(
                new Rect(textX, rowRect.y + 31f, textWidth, EditorGUIUtility.singleLineHeight),
                slotId,
                EditorStyles.miniLabel);
            GUI.Label(
                new Rect(textX, rowRect.y + 50f, textWidth, EditorGUIUtility.singleLineHeight),
                iconName,
                EditorStyles.miniLabel);
        }

        private static void DrawCenteredPreviewLabel(Rect rect, string text)
        {
            GUIStyle style = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                wordWrap = true
            };
            GUI.Label(rect, text, style);
        }

        private void DrawProductActions()
        {
            DrawSection("Product Actions");

            if (GUILayout.Button("Validate"))
            {
                RunValidation();
            }
        }

        private void RunValidation()
        {
            _validationReport =
                PlayerParticipationAuthoringValidator.ValidatePlayerSlotProfile(
                    (PlayerSlotProfile)target,
                    true);
            _hasValidationResult = true;
        }

        private void ClearValidationResult()
        {
            _validationReport = null;
            _hasValidationResult = false;
            Repaint();
        }

        private void DrawValidationSummary()
        {
            DrawSection("Validation Summary");

            if (!_hasValidationResult || _validationReport == null)
            {
                EditorGUILayout.LabelField(
                    "Not Validated",
                    EditorStyles.miniLabel);
                return;
            }

            FrameworkAuthoringValidationGui.DrawSummary(_validationReport);
            FrameworkAuthoringValidationGui.DrawIssues(_validationReport, false);
        }

        private void DrawAdvancedSection()
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

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(
                _playerSlotId,
                new GUIContent(
                    "Player Slot ID",
                    "Canonical stable PlayerSlotId owned by this Profile. Changing it may invalidate external references."));

            string suggestedId = PlayerProfileIdSuggestionUtility.SuggestPlayerSlotId(
                _displayName.stringValue,
                target.name);
            DrawIdSuggestion(
                _playerSlotId,
                suggestedId);

            bool advancedAuthoringChanged = EditorGUI.EndChangeCheck();
            bool advancedPropertiesApplied = serializedObject.ApplyModifiedProperties();
            if (advancedAuthoringChanged || advancedPropertiesApplied)
            {
                ClearValidationResult();
            }

            DrawAdvancedEvidence((PlayerSlotProfile)target);
        }

        private static void DrawAdvancedEvidence(PlayerSlotProfile profile)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    "Asset Path",
                    AssetDatabase.GetAssetPath(profile));
                EditorGUILayout.TextField(
                    "Normalized Identity",
                    profile.PlayerSlotIdText);

                string typedIdentity = profile.TryGetPlayerSlotId(
                    out var playerSlotId,
                    out string issue)
                    ? playerSlotId.ToString()
                    : $"Invalid: {issue}";
                EditorGUILayout.TextField(
                    "Typed PlayerSlotId",
                    typedIdentity);
                EditorGUILayout.Toggle(
                    "Has Default Actor",
                    profile.HasDefaultActorProfile);

                ActorProfile defaultActor = profile.DefaultActorProfile;
                string defaultIdentity = defaultActor != null &&
                    defaultActor.TryGetActorProfileId(
                        out ActorProfileId actorProfileId,
                        out _)
                    ? actorProfileId.ToString()
                    : string.Empty;
                EditorGUILayout.TextField(
                    "Default ActorProfileId",
                    defaultIdentity);
            }
        }

        private static void DrawIdSuggestion(
            SerializedProperty idProperty,
            string suggestedId)
        {
            EditorGUILayout.LabelField(
                "Suggested ID",
                string.IsNullOrWhiteSpace(suggestedId) ? "Unavailable" : suggestedId,
                EditorStyles.miniLabel);

            bool canApply =
                !idProperty.hasMultipleDifferentValues &&
                string.IsNullOrWhiteSpace(idProperty.stringValue) &&
                !string.IsNullOrWhiteSpace(suggestedId);

            using (new EditorGUI.DisabledScope(!canApply))
            {
                if (GUILayout.Button(new GUIContent(
                    "Use Suggested ID",
                    "Applies the displayed deterministic suggestion only while the ID is empty. Run Validate afterward to check project uniqueness.")))
                {
                    idProperty.stringValue = suggestedId;
                    GUI.FocusControl(null);
                }
            }
        }

        private static void DrawSection(string title)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }
    }

    internal static class PlayerProfileIdSuggestionUtility
    {
        internal static string SuggestPlayerSlotId(
            string displayName,
            string assetName)
        {
            string slug = BuildSlug(SelectSource(displayName, assetName));
            slug = TrimPrefix(slug, "player-slot-");
            slug = TrimPrefix(slug, "slot-");
            slug = TrimPrefix(slug, "player-");
            slug = TrimSuffix(slug, "-player-slot");
            slug = TrimSuffix(slug, "-slot");

            return $"player.{ResolveSuffix(slug)}";
        }

        internal static string SuggestActorProfileId(
            string displayName,
            string assetName)
        {
            string slug = BuildSlug(SelectSource(displayName, assetName));
            slug = TrimPrefix(slug, "actor-profile-");
            slug = TrimPrefix(slug, "actor-");
            slug = TrimSuffix(slug, "-actor-profile");
            slug = TrimSuffix(slug, "-actor");

            return $"actor-profile.{ResolveSuffix(slug)}";
        }

        private static string SelectSource(string displayName, string assetName)
        {
            return !string.IsNullOrWhiteSpace(displayName)
                ? displayName
                : assetName;
        }

        private static string ResolveSuffix(string slug)
        {
            return string.IsNullOrWhiteSpace(slug) ? "default" : slug;
        }

        private static string BuildSlug(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return string.Empty;
            }

            string normalizedSource = source
                .Trim()
                .Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalizedSource.Length);
            bool pendingSeparator = false;

            foreach (char character in normalizedSource)
            {
                UnicodeCategory category =
                    CharUnicodeInfo.GetUnicodeCategory(character);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(character))
                {
                    if (pendingSeparator && builder.Length > 0)
                    {
                        builder.Append('-');
                    }

                    builder.Append(char.ToLower(character, CultureInfo.InvariantCulture));
                    pendingSeparator = false;
                }
                else
                {
                    pendingSeparator = true;
                }
            }

            return builder
                .ToString()
                .Normalize(NormalizationForm.FormC)
                .Trim('-');
        }

        private static string TrimPrefix(string value, string prefix)
        {
            return value.StartsWith(prefix, StringComparison.Ordinal)
                ? value.Substring(prefix.Length)
                : value;
        }

        private static string TrimSuffix(string value, string suffix)
        {
            return value.EndsWith(suffix, StringComparison.Ordinal)
                ? value.Substring(0, value.Length - suffix.Length)
                : value;
        }
    }
}
