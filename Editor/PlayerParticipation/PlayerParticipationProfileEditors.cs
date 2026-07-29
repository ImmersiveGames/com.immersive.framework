using Immersive.Framework.Actors;
using Immersive.Framework.Editor.Editor.Validation;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.PlayerParticipation
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
            DrawIdentity();
            DrawPresentation();
            DrawActorSelectionDefault();
            bool authoringChanged = EditorGUI.EndChangeCheck();

            serializedObject.ApplyModifiedProperties();

            if (authoringChanged)
            {
                ClearValidationResult();
                Repaint();
            }

            DrawPrimaryActions();

            if (_hasValidationResult && _validationReport != null)
            {
                DrawValidationResult(_validationReport);
            }

            if (_showAdvanced)
            {
                DrawAdvanced((PlayerSlotProfile)target);
            }
        }

        private void DrawIdentity()
        {
            DrawSection("Identity");

            _playerSlotId.stringValue = EditorGUILayout.TextField(
                new GUIContent(
                    "Player Slot Id",
                    "Canonical stable PlayerSlotId owned by this Profile."),
                _playerSlotId.stringValue);

            EditorGUILayout.PropertyField(
                _displayName,
                new GUIContent(
                    "Display Name",
                    "Designer-facing name used to present this Slot."));

            EditorGUILayout.PropertyField(
                _description,
                new GUIContent(
                    "Description",
                    "Optional designer-facing description of this Slot."));
        }

        private void DrawPresentation()
        {
            DrawSection("Presentation");

            _accentColor.colorValue = EditorGUILayout.ColorField(
                new GUIContent(
                    "Accent Color",
                    "Presentation color associated with this Slot."),
                _accentColor.colorValue);

            EditorGUILayout.PropertyField(
                _icon,
                new GUIContent(
                    "Icon",
                    "Optional Sprite used to present this Slot."));

            DrawIconPreview();

            EditorGUILayout.PropertyField(
                _displayOrder,
                new GUIContent(
                    "Display Order",
                    "Presentation metadata only. Game Application array order controls default local Slot allocation."));
        }

        private void DrawActorSelectionDefault()
        {
            DrawSection("Actor Selection Default");

            _defaultActorProfile.objectReferenceValue = EditorGUILayout.ObjectField(
                new GUIContent(
                    "Default Actor Profile",
                    "Optional immutable default Actor selection intent. Session runtime applies it through the canonical selection operation after the Slot is Joined."),
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
                ? "No Player Slot Id"
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

        private void DrawPrimaryActions()
        {
            EditorGUILayout.Space(5f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate"))
                {
                    RunValidation();
                }

                string advancedLabel = _showAdvanced
                    ? "Hide Advanced / Debug"
                    : "Advanced / Debug";
                if (GUILayout.Button(advancedLabel))
                {
                    _showAdvanced = !_showAdvanced;
                }
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
        }

        private static void DrawValidationResult(
            FrameworkAuthoringValidationReport report)
        {
            DrawSection("Validation Result");
            FrameworkAuthoringValidationGui.DrawSummary(report);
            FrameworkAuthoringValidationGui.DrawIssues(report, false);
        }

        private static void DrawAdvanced(PlayerSlotProfile profile)
        {
            DrawSection("Advanced / Debug");

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Asset Path", AssetDatabase.GetAssetPath(profile));
                EditorGUILayout.TextField("Normalized Identity", profile.PlayerSlotIdText);

                string typedIdentity = profile.TryGetPlayerSlotId(
                    out var playerSlotId,
                    out string issue)
                    ? playerSlotId.ToString()
                    : $"Invalid: {issue}";
                EditorGUILayout.TextField("Typed PlayerSlotId", typedIdentity);
                EditorGUILayout.Toggle("Has Default Actor", profile.HasDefaultActorProfile);

                ActorProfile defaultActor = profile.DefaultActorProfile;
                string defaultIdentity = defaultActor != null &&
                    defaultActor.TryGetActorProfileId(
                        out ActorProfileId actorProfileId,
                        out _)
                    ? actorProfileId.ToString()
                    : string.Empty;
                EditorGUILayout.TextField("Default ActorProfileId", defaultIdentity);
            }
        }

        private static void DrawSection(string title)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }
    }
}
