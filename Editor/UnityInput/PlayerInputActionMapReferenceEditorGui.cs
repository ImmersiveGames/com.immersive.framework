using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.Editor.UnityInput
{
    internal static class PlayerInputActionMapReferenceEditorGui
    {
        private const string AssetField =
            "actionAsset";

        private const string MapIdField =
            "actionMapId";

        private const string CachedNameField =
            "cachedActionMapName";

        internal static void DrawForPlayerInput(
            GUIContent label,
            SerializedProperty referenceProperty,
            PlayerInput playerInput)
        {
            InputActionAsset expectedAsset =
                playerInput != null
                    ? playerInput.actions
                    : null;

            SynchronizeAsset(
                referenceProperty,
                expectedAsset);

            DrawMapPopup(
                label,
                referenceProperty,
                expectedAsset);
        }

        internal static void Assign(
            SerializedProperty referenceProperty,
            InputActionMap actionMap)
        {
            SerializedProperty asset =
                RequiredChild(
                    referenceProperty,
                    AssetField);

            SerializedProperty mapId =
                RequiredChild(
                    referenceProperty,
                    MapIdField);

            SerializedProperty cachedName =
                RequiredChild(
                    referenceProperty,
                    CachedNameField);

            asset.objectReferenceValue =
                actionMap != null
                    ? actionMap.asset
                    : null;

            mapId.stringValue =
                actionMap != null
                    ? actionMap.id.ToString("D")
                    : string.Empty;

            cachedName.stringValue =
                actionMap != null
                    ? actionMap.name
                    : string.Empty;
        }

        internal static bool TryAssignByName(
            SerializedProperty referenceProperty,
            InputActionAsset actionAsset,
            string mapName,
            out string diagnostic)
        {
            if (actionAsset == null)
            {
                diagnostic =
                    "Action Map migration requires an InputActionAsset.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(
                    mapName))
            {
                diagnostic =
                    "Action Map migration requires a legacy map name.";
                return false;
            }

            InputActionMap actionMap =
                actionAsset.FindActionMap(
                    mapName.Trim(),
                    false);

            if (actionMap == null)
            {
                diagnostic =
                    $"Legacy Action Map '{mapName.Trim()}' was not found in '{actionAsset.name}'.";
                return false;
            }

            Assign(
                referenceProperty,
                actionMap);

            diagnostic =
                $"Migrated legacy Action Map '{actionMap.name}' to GUID '{actionMap.id:D}'.";
            return true;
        }

        private static void DrawMapPopup(
            GUIContent label,
            SerializedProperty referenceProperty,
            InputActionAsset actionAsset)
        {
            SerializedProperty mapId =
                RequiredChild(
                    referenceProperty,
                    MapIdField);

            SerializedProperty cachedName =
                RequiredChild(
                    referenceProperty,
                    CachedNameField);

            if (actionAsset == null)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Popup(
                        label,
                        0,
                        new[]
                        {
                            "Assign PlayerInput Actions"
                        });
                }

                return;
            }

            int mapCount =
                actionAsset.actionMaps.Count;

            string[] options =
                new string[mapCount + 1];

            options[0] =
                "<None>";

            int selectedIndex = 0;
            bool hasId =
                Guid.TryParse(
                    mapId.stringValue,
                    out Guid selectedId);

            for (int index = 0;
                 index < mapCount;
                 index++)
            {
                InputActionMap map =
                    actionAsset.actionMaps[index];

                options[index + 1] =
                    map.name;

                if (hasId &&
                    map.id == selectedId)
                {
                    selectedIndex =
                        index + 1;

                    if (!string.Equals(
                            cachedName.stringValue,
                            map.name,
                            StringComparison.Ordinal))
                    {
                        cachedName.stringValue =
                            map.name;
                    }
                }
            }

            int nextIndex =
                EditorGUILayout.Popup(
                    label,
                    selectedIndex,
                    options);

            if (nextIndex ==
                selectedIndex)
            {
                return;
            }

            Assign(
                referenceProperty,
                nextIndex > 0
                    ? actionAsset.actionMaps[
                        nextIndex - 1]
                    : null);
        }

        private static void SynchronizeAsset(
            SerializedProperty referenceProperty,
            InputActionAsset expectedAsset)
        {
            SerializedProperty asset =
                RequiredChild(
                    referenceProperty,
                    AssetField);

            if (ReferenceEquals(
                    asset.objectReferenceValue,
                    expectedAsset))
            {
                return;
            }

            SerializedProperty mapId =
                RequiredChild(
                    referenceProperty,
                    MapIdField);

            bool retainSelection =
                expectedAsset != null &&
                Guid.TryParse(
                    mapId.stringValue,
                    out Guid retainedId) &&
                expectedAsset.FindActionMap(
                    retainedId) != null;

            asset.objectReferenceValue =
                expectedAsset;

            if (!retainSelection)
            {
                Assign(
                    referenceProperty,
                    null);

                asset.objectReferenceValue =
                    expectedAsset;
            }
        }

        private static SerializedProperty RequiredChild(
            SerializedProperty parent,
            string childName)
        {
            SerializedProperty child =
                parent.FindPropertyRelative(
                    childName);

            if (child == null)
            {
                throw new InvalidOperationException(
                    $"Missing serialized Action Map reference field '{parent.propertyPath}.{childName}'.");
            }

            return child;
        }
    }
}
