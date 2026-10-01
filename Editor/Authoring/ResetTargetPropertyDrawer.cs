using Immersive.Framework.Reset;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.ObjectEntry;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Authoring
{
    [CustomPropertyDrawer(typeof(ResetTarget))]
    internal sealed class ResetTargetPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty kind = property.FindPropertyRelative("kind");
            string payload = PayloadName((ResetTargetKind)kind.intValue);
            return EditorGUIUtility.singleLineHeight + 6f + (payload == null ? 0f : EditorGUI.GetPropertyHeight(property.FindPropertyRelative(payload), true));
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Rect row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            SerializedProperty kind = property.FindPropertyRelative("kind");
            EditorGUI.PropertyField(row, kind, label);
            string payloadName = PayloadName((ResetTargetKind)kind.intValue);
            if (payloadName != null)
            {
                SerializedProperty payload = property.FindPropertyRelative(payloadName);
                row.y += EditorGUIUtility.singleLineHeight + 4f;
                row.height = EditorGUI.GetPropertyHeight(payload, true);
                EditorGUI.PropertyField(row, payload, true);
            }
            EditorGUI.EndProperty();
        }

        private static string PayloadName(ResetTargetKind kind) => kind switch
        {
            ResetTargetKind.Object => "resettable",
            ResetTargetKind.Composition => "composition",
            ResetTargetKind.StableReference => "stableReference",
            _ => null
        };
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
            {
                if (GUI.Button(pasteRect, "Paste ID")) objectEntryId.stringValue = EditorGUIUtility.systemCopyBuffer.Trim();
            }
            row.y += EditorGUIUtility.singleLineHeight + 2f;
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(row, kind, new GUIContent("Owner"));
            bool selectorChanged = EditorGUI.EndChangeCheck();
            if (selectorChanged)
            {
                property.FindPropertyRelative("routeOwner").objectReferenceValue = null;
                property.FindPropertyRelative("activityOwner").objectReferenceValue = null;
            }
            bool hasOwnerPayload = (StableObjectOwnerSelectorKind)kind.intValue != StableObjectOwnerSelectorKind.Unspecified;
            if (hasOwnerPayload) row.y += EditorGUIUtility.singleLineHeight + 2f;
            switch ((StableObjectOwnerSelectorKind)kind.intValue)
            {
                case StableObjectOwnerSelectorKind.Route:
                    EditorGUI.PropertyField(row, property.FindPropertyRelative("routeOwner"), new GUIContent("Route"));
                    break;
                case StableObjectOwnerSelectorKind.Activity:
                    EditorGUI.PropertyField(row, property.FindPropertyRelative("activityOwner"), new GUIContent("Activity"));
                    break;
            }
            row.y += EditorGUIUtility.singleLineHeight + 2f;
            if (!IsValidObjectEntryId(objectEntryId.stringValue))
            {
                EditorGUI.HelpBox(row, "Paste a valid Object Entry ID copied from its declaration.", MessageType.Error);
            }
            EditorGUI.EndProperty();
        }

        private static bool IsValidObjectEntryId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            try { return ObjectEntryId.From(value.Trim()).IsValid; }
            catch (System.ArgumentException) { return false; }
        }
    }
}
