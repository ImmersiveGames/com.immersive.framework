using System;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.Editor.Editor.PlayerParticipation
{
    /// <summary>
    /// Official editor creation surface for one valid Local Player technical host.
    /// It creates authoring structure only and never executes join or gameplay.
    /// </summary>
    public static class LocalPlayerHostCreationUtility
    {
        private const string MenuPath =
            "GameObject/Immersive Framework/Player/Local Player Host";

        [MenuItem(MenuPath, false, 20)]
        private static void CreateFromMenu(MenuCommand command)
        {
            Transform parent = command.context is GameObject parentObject
                ? parentObject.transform
                : null;
            LocalPlayerHostAuthoring host = CreateLocalPlayerHost(
                "Local Player Host",
                parent,
                useUndo: true);
            Selection.activeGameObject = host.gameObject;
        }

        public static LocalPlayerHostAuthoring CreateLocalPlayerHost(
            string objectName,
            Transform parent = null,
            bool useUndo = true)
        {
            string resolvedName = string.IsNullOrWhiteSpace(objectName)
                ? "Local Player Host"
                : objectName.Trim();

            var root = new GameObject(resolvedName);
            if (useUndo)
            {
                Undo.RegisterCreatedObjectUndo(root, "Create Local Player Host");
            }

            if (parent != null)
            {
                if (useUndo)
                {
                    Undo.SetTransformParent(root.transform, parent, "Parent Local Player Host");
                }
                else
                {
                    root.transform.SetParent(parent, false);
                }
            }

            PlayerInput playerInput = useUndo
                ? Undo.AddComponent<PlayerInput>(root)
                : root.AddComponent<PlayerInput>();

            var actorMountObject = new GameObject("ActorMount");
            if (useUndo)
            {
                Undo.RegisterCreatedObjectUndo(actorMountObject, "Create Local Player Actor Mount");
                Undo.SetTransformParent(
                    actorMountObject.transform,
                    root.transform,
                    "Parent Local Player Actor Mount");
            }
            else
            {
                actorMountObject.transform.SetParent(root.transform, false);
            }

            actorMountObject.transform.localPosition = Vector3.zero;
            actorMountObject.transform.localRotation = Quaternion.identity;
            actorMountObject.transform.localScale = Vector3.one;

            LocalPlayerHostAuthoring host = useUndo
                ? Undo.AddComponent<LocalPlayerHostAuthoring>(root)
                : root.AddComponent<LocalPlayerHostAuthoring>();

            var serializedHost = new SerializedObject(host);
            SerializedProperty playerInputProperty =
                serializedHost.FindProperty("playerInput");
            SerializedProperty actorMountProperty =
                serializedHost.FindProperty("actorMount");
            if (playerInputProperty == null || actorMountProperty == null)
            {
                UnityEngine.Object.DestroyImmediate(root);
                throw new MissingFieldException(
                    nameof(LocalPlayerHostAuthoring),
                    "playerInput/actorMount");
            }

            playerInputProperty.objectReferenceValue = playerInput;
            actorMountProperty.objectReferenceValue = actorMountObject.transform;
            serializedHost.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(host);
            EditorUtility.SetDirty(root);
            return host;
        }
    }
}
