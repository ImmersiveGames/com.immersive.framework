using System;
using System.Collections.Generic;
using Immersive.Framework.ObjectEntry;
using Immersive.Framework.RuntimeContent;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Immersive.Framework.Editor.Authoring
{
    [CustomPropertyDrawer(typeof(ObjectEntryIdReference))]
    internal sealed class ObjectEntryIdReferencePropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return ObjectEntryIdentityPropertyField.GetPropertyHeight();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            ObjectEntryIdentityPropertyField.Draw(
                position,
                property.FindPropertyRelative("objectEntryIdText"),
                label);
            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(StableObjectReference))]
    internal sealed class StableObjectReferencePropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float identityHeight = ObjectEntryIdentityPropertyField.GetPropertyHeight();
            SerializedProperty kind = property.FindPropertyRelative("ownerSelectorKind");
            bool hasOwnerPayload = (StableObjectOwnerSelectorKind)kind.intValue != StableObjectOwnerSelectorKind.Unspecified;
            return identityHeight + 2f
                + EditorGUIUtility.singleLineHeight
                + (hasOwnerPayload ? EditorGUIUtility.singleLineHeight + 2f : 0f);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            SerializedProperty objectEntryId = property.FindPropertyRelative("objectEntryIdText");
            SerializedProperty kind = property.FindPropertyRelative("ownerSelectorKind");
            float identityHeight = ObjectEntryIdentityPropertyField.GetPropertyHeight();
            Rect identityRect = new Rect(position.x, position.y, position.width, identityHeight);
            ObjectEntryIdentityPropertyField.Draw(identityRect, objectEntryId, label);

            Rect row = new Rect(
                position.x,
                identityRect.yMax + 2f,
                position.width,
                EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(row, kind, new GUIContent("Resolution Owner (optional)"));
            bool selectorChanged = EditorGUI.EndChangeCheck();
            if (selectorChanged)
                ClearInactiveOwnerPayload(property, (StableObjectOwnerSelectorKind)kind.intValue);

            bool hasOwnerPayload = (StableObjectOwnerSelectorKind)kind.intValue != StableObjectOwnerSelectorKind.Unspecified;
            if (hasOwnerPayload)
            {
                row.y += EditorGUIUtility.singleLineHeight + 2f;
                if ((StableObjectOwnerSelectorKind)kind.intValue == StableObjectOwnerSelectorKind.Route)
                    EditorGUI.PropertyField(row, property.FindPropertyRelative("routeOwner"), new GUIContent("Route"));
                else if ((StableObjectOwnerSelectorKind)kind.intValue == StableObjectOwnerSelectorKind.Activity)
                    EditorGUI.PropertyField(row, property.FindPropertyRelative("activityOwner"), new GUIContent("Activity"));
            }
            EditorGUI.EndProperty();
        }

        private static void ClearInactiveOwnerPayload(
            SerializedProperty property,
            StableObjectOwnerSelectorKind nextKind)
        {
            switch (nextKind)
            {
                case StableObjectOwnerSelectorKind.Route:
                    property.FindPropertyRelative("activityOwner").objectReferenceValue = null;
                    break;
                case StableObjectOwnerSelectorKind.Activity:
                    property.FindPropertyRelative("routeOwner").objectReferenceValue = null;
                    break;
                case StableObjectOwnerSelectorKind.Unspecified:
                default:
                    property.FindPropertyRelative("routeOwner").objectReferenceValue = null;
                    property.FindPropertyRelative("activityOwner").objectReferenceValue = null;
                    break;
            }
        }
    }

    internal static class ObjectEntryIdentityPropertyField
    {
        private const float RowSpacing = 2f;
        private const float StatusHeight = 32f;

        internal static float GetPropertyHeight()
        {
            return EditorGUIUtility.singleLineHeight * 2f + RowSpacing * 2f + StatusHeight;
        }

        internal static void Draw(Rect position, SerializedProperty identityText, GUIContent label)
        {
            if (identityText == null)
            {
                return;
            }

            ObjectEntryAuthoringIndexSnapshot snapshot = ObjectEntryAuthoringIndex.GetSnapshot();
            string rawId = identityText.stringValue ?? string.Empty;
            float lineHeight = EditorGUIUtility.singleLineHeight;
            Rect identityRect = new Rect(position.x, position.y, position.width, lineHeight);
            bool previousShowMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = identityText.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            string enteredId = EditorGUI.TextField(identityRect, label, rawId);
            bool identityChanged = EditorGUI.EndChangeCheck();
            EditorGUI.showMixedValue = previousShowMixedValue;
            if (identityChanged)
            {
                identityText.stringValue = enteredId?.Trim() ?? string.Empty;
            }

            Rect actionsRect = new Rect(
                position.x,
                identityRect.yMax + RowSpacing,
                position.width,
                lineHeight);
            float actionSpacing = 4f;
            float browseWidth = (actionsRect.width - actionSpacing) * 0.58f;
            Rect browseRect = new Rect(actionsRect.x, actionsRect.y, browseWidth, actionsRect.height);
            Rect pasteRect = new Rect(
                browseRect.xMax + actionSpacing,
                actionsRect.y,
                actionsRect.width - browseWidth - actionSpacing,
                actionsRect.height);

            if (EditorGUI.DropdownButton(
                    browseRect,
                    new GUIContent("Browse Entries", "Choose an indexed declaration, or enter/paste any valid Object Entry ID above."),
                    FocusType.Keyboard,
                    EditorStyles.popup))
            {
                ObjectEntryAuthoringIndexSnapshot currentSnapshot = ObjectEntryAuthoringIndex.Refresh();
                UnityEngine.Object[] targets = identityText.serializedObject.targetObjects;
                string propertyPath = identityText.propertyPath;
                var dropdown = new ObjectEntryIdentityDropdown(
                    currentSnapshot,
                    selectedId => ApplySelectedId(targets, propertyPath, selectedId));
                dropdown.Show(browseRect);
            }

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(EditorGUIUtility.systemCopyBuffer)))
            {
                if (GUI.Button(pasteRect, "Paste ID"))
                {
                    identityText.stringValue = EditorGUIUtility.systemCopyBuffer.Trim();
                }
            }

            Rect statusRect = new Rect(
                position.x,
                actionsRect.yMax + RowSpacing,
                position.width,
                StatusHeight);
            string currentId = identityText.stringValue ?? string.Empty;
            string status = snapshot.GetStatusMessage(currentId);
            EditorGUI.HelpBox(statusRect, status, snapshot.GetStatusType(currentId));
        }

        private static void ApplySelectedId(UnityEngine.Object[] targets, string propertyPath, ObjectEntryId id)
        {
            if (targets == null || targets.Length == 0 || string.IsNullOrWhiteSpace(propertyPath) || !id.IsValid)
            {
                return;
            }

            for (int index = 0; index < targets.Length; index++)
            {
                if (targets[index] == null)
                {
                    return;
                }
            }

            var serialized = new SerializedObject(targets);
            serialized.Update();
            SerializedProperty identityText = serialized.FindProperty(propertyPath);
            if (identityText == null || identityText.propertyType != SerializedPropertyType.String)
            {
                return;
            }

            // Persist the canonical identity payload, not its diagnostic StableText form.
            identityText.stringValue = id.Value.Value;
            serialized.ApplyModifiedProperties();
        }
    }

    internal sealed class ObjectEntryIdentityDropdown : AdvancedDropdown
    {
        private readonly ObjectEntryAuthoringIndexSnapshot _snapshot;
        private readonly Action<ObjectEntryId> _onSelected;
        private readonly Dictionary<int, ObjectEntryId> _identitiesByItemId = new Dictionary<int, ObjectEntryId>();

        internal ObjectEntryIdentityDropdown(
            ObjectEntryAuthoringIndexSnapshot snapshot,
            Action<ObjectEntryId> onSelected)
            : base(new AdvancedDropdownState())
        {
            _snapshot = snapshot;
            _onSelected = onSelected;
            minimumSize = new Vector2(560f, 340f);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            string title = _snapshot.HasIssues
                ? $"Object Entries · {_snapshot.Issues.Count} discovery issue(s)"
                : "Object Entries";
            var root = new AdvancedDropdownItem(title);
            _identitiesByItemId.Clear();

            for (int index = 0; index < _snapshot.Candidates.Count; index++)
            {
                ObjectEntryAuthoringCandidate candidate = _snapshot.Candidates[index];
                _identitiesByItemId.Add(index, candidate.Id);
                var item = new AdvancedDropdownItem(candidate.DisplayLabel)
                {
                    id = index
                };
                root.AddChild(item);
            }

            if (_snapshot.Candidates.Count == 0)
            {
                root.AddChild(new AdvancedDropdownItem(
                    _snapshot.HasIssues
                        ? "No entries could be indexed completely; existing values are preserved."
                        : "No Object Entry declarations were found."));
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (_identitiesByItemId.TryGetValue(item.id, out ObjectEntryId id))
            {
                _onSelected?.Invoke(id);
            }
        }
    }
}
