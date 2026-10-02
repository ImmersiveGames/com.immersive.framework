using Immersive.Framework.ObjectEntry;
using Immersive.Framework.Reset;
using Immersive.Framework.RuntimeContent;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Authoring
{
    [CustomPropertyDrawer(typeof(ResetTarget))]
    internal sealed class ResetTargetPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            ResetTargetKind kind = (ResetTargetKind)property.FindPropertyRelative("kind").intValue;
            SerializedProperty payload = kind switch
            {
                ResetTargetKind.Object => property.FindPropertyRelative("objectTarget"),
                ResetTargetKind.Composition => property.FindPropertyRelative("compositionTarget"),
                _ => null
            };
            return EditorGUIUtility.singleLineHeight + 4f
                + (payload != null ? EditorGUI.GetPropertyHeight(payload, true) : 0f);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Rect row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            SerializedProperty kindProperty = property.FindPropertyRelative("kind");
            ResetTargetKind previousKind = (ResetTargetKind)kindProperty.intValue;
            EditorGUI.PropertyField(row, kindProperty, label);
            ResetTargetKind kind = (ResetTargetKind)kindProperty.intValue;
            if (kind != previousKind)
                ClearInactiveSemanticTarget(property, kind);

            SerializedProperty payload = kind switch
            {
                ResetTargetKind.Object => property.FindPropertyRelative("objectTarget"),
                ResetTargetKind.Composition => property.FindPropertyRelative("compositionTarget"),
                _ => null
            };
            if (payload != null)
            {
                row.y += EditorGUIUtility.singleLineHeight + 4f;
                row.height = EditorGUI.GetPropertyHeight(payload, true);
                EditorGUI.PropertyField(row, payload, true);
            }
            EditorGUI.EndProperty();
        }

        internal static void ClearInactiveSemanticTarget(
            SerializedProperty property,
            ResetTargetKind nextKind)
        {
            if (nextKind != ResetTargetKind.Object)
                ClearObjectTarget(property.FindPropertyRelative("objectTarget"));
            if (nextKind != ResetTargetKind.Composition)
                ClearCompositionTarget(property.FindPropertyRelative("compositionTarget"));
        }

        private static void ClearObjectTarget(SerializedProperty target)
        {
            if (target == null) return;
            target.FindPropertyRelative("referenceMode").intValue = (int)ResetReferenceMode.Direct;
            target.FindPropertyRelative("directResettable").objectReferenceValue = null;
            ClearStableReferenceForModeChange(target.FindPropertyRelative("stableReference"));
        }

        private static void ClearCompositionTarget(SerializedProperty target)
        {
            if (target == null) return;
            target.FindPropertyRelative("referenceMode").intValue = (int)ResetReferenceMode.Direct;
            target.FindPropertyRelative("directComposition").objectReferenceValue = null;
            ClearStableReferenceForModeChange(target.FindPropertyRelative("stableReference"));
        }

        internal static void ClearStableReferenceForModeChange(SerializedProperty reference)
        {
            if (reference == null) return;
            reference.FindPropertyRelative("objectEntryIdText").stringValue = string.Empty;
            reference.FindPropertyRelative("ownerSelectorKind").intValue = 0;
            reference.FindPropertyRelative("routeOwner").objectReferenceValue = null;
            reference.FindPropertyRelative("activityOwner").objectReferenceValue = null;
        }
    }

    [CustomPropertyDrawer(typeof(ResetObjectTarget))]
    internal sealed class ResetObjectTargetPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            ResetReferenceMode mode = (ResetReferenceMode)property.FindPropertyRelative("referenceMode").intValue;
            SerializedProperty payload = mode == ResetReferenceMode.Direct
                ? property.FindPropertyRelative("directResettable")
                : mode == ResetReferenceMode.Stable ? property.FindPropertyRelative("stableReference") : null;
            return EditorGUIUtility.singleLineHeight + 2f
                + (payload != null ? EditorGUI.GetPropertyHeight(payload, true) : 0f);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ResetTargetDrawerFields.DrawReferenceMode(position, property, label, "directResettable");
        }
    }

    [CustomPropertyDrawer(typeof(ResetCompositionTarget))]
    internal sealed class ResetCompositionTargetPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            ResetReferenceMode mode = (ResetReferenceMode)property.FindPropertyRelative("referenceMode").intValue;
            SerializedProperty payload = mode == ResetReferenceMode.Direct
                ? property.FindPropertyRelative("directComposition")
                : mode == ResetReferenceMode.Stable ? property.FindPropertyRelative("stableReference") : null;
            return EditorGUIUtility.singleLineHeight + 2f
                + (payload != null ? EditorGUI.GetPropertyHeight(payload, true) : 0f);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ResetTargetDrawerFields.DrawReferenceMode(position, property, label, "directComposition");
        }
    }

    [CustomPropertyDrawer(typeof(StableObjectReference))]
    internal sealed class StableObjectReferencePropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int rows = 2;
            SerializedProperty kind = property.FindPropertyRelative("ownerSelectorKind");
            if ((StableObjectOwnerSelectorKind)kind.intValue != StableObjectOwnerSelectorKind.Unspecified) rows++;
            if (!IsValidObjectEntryId(property.FindPropertyRelative("objectEntryIdText").stringValue)) rows++;
            return rows * EditorGUIUtility.singleLineHeight + (rows - 1) * 2f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            SerializedProperty objectEntryId = property.FindPropertyRelative("objectEntryIdText");
            SerializedProperty kind = property.FindPropertyRelative("ownerSelectorKind");
            Rect row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            const float buttonWidth = 78f;
            Rect idRect = new Rect(row.x, row.y, row.width - buttonWidth - 4f, row.height);
            using (new EditorGUI.DisabledScope(true))
                EditorGUI.TextField(idRect, new GUIContent("Object Entry ID"), objectEntryId.stringValue);
            Rect pasteRect = new Rect(idRect.xMax + 4f, row.y, buttonWidth, row.height);
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(EditorGUIUtility.systemCopyBuffer)))
                if (GUI.Button(pasteRect, "Paste ID")) objectEntryId.stringValue = EditorGUIUtility.systemCopyBuffer.Trim();

            row.y += EditorGUIUtility.singleLineHeight + 2f;
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(row, kind, new GUIContent("Owner"));
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

            row.y += EditorGUIUtility.singleLineHeight + 2f;
            if (!IsValidObjectEntryId(objectEntryId.stringValue))
                EditorGUI.HelpBox(row, "Paste a valid Object Entry ID copied from its declaration.", MessageType.Error);
            EditorGUI.EndProperty();
        }

        private static bool IsValidObjectEntryId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            try { return ObjectEntryId.From(value.Trim()).IsValid; }
            catch (System.ArgumentException) { return false; }
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

    internal static class ResetTargetDrawerFields
    {
        internal static void DrawReferenceMode(Rect position, SerializedProperty property, GUIContent label, string directField)
        {
            EditorGUI.BeginProperty(position, label, property);
            SerializedProperty mode = property.FindPropertyRelative("referenceMode");
            ResetReferenceMode previousMode = (ResetReferenceMode)mode.intValue;
            Rect row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(row, mode, label);
            ResetReferenceMode nextMode = (ResetReferenceMode)mode.intValue;
            if (previousMode != nextMode)
                ClearInactiveReferencePayload(property, directField, nextMode);

            SerializedProperty payload = nextMode == ResetReferenceMode.Direct
                ? property.FindPropertyRelative(directField)
                : nextMode == ResetReferenceMode.Stable ? property.FindPropertyRelative("stableReference") : null;
            if (payload != null)
            {
                row.y += EditorGUIUtility.singleLineHeight + 2f;
                row.height = EditorGUI.GetPropertyHeight(payload, true);
                EditorGUI.PropertyField(row, payload, true);
            }
            EditorGUI.EndProperty();
        }

        internal static void ClearInactiveReferencePayload(
            SerializedProperty property,
            string directField,
            ResetReferenceMode activeMode)
        {
            if (activeMode == ResetReferenceMode.Direct)
                ResetTargetPropertyDrawer.ClearStableReferenceForModeChange(property.FindPropertyRelative("stableReference"));
            else if (activeMode == ResetReferenceMode.Stable)
                property.FindPropertyRelative(directField).objectReferenceValue = null;
        }
    }
}
