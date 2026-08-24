using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.Editor.PlayerParticipation
{
    /// <summary>
    /// Canonical Editor creation surface for Session Local Player provisioning.
    /// It creates and wires only the deterministic Session/UIGlobal provisioning
    /// setup and authority. It does not create a Local Player Host instance; the
    /// consumer still authors the Local Player Host Prefab and gameplay-specific
    /// intent explicitly.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.DevelopmentTooling,
        "Canonical Editor create action for the Local Player provisioning setup.")]
    public static class ManagerProvisionedLocalPlayerCreator
    {
        private const string MenuPath =
            "GameObject/Immersive Framework/Player/Provisioning/Create Setup";

        private const string UndoName =
            "Create Local Player Provisioning";

        [MenuItem(MenuPath, false, 11)]
        private static void CreateFromMenu(MenuCommand command)
        {
            GameObject parent =
                command.context as GameObject ??
                Selection.activeGameObject;

            Create(parent);
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateCreateFromMenu() =>
            !EditorApplication.isPlayingOrWillChangePlaymode;

        public static LocalPlayerProvisioningAuthoring Create(
            GameObject parent = null)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Local Player provisioning authoring is unavailable while entering or running Play Mode.");
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);

            try
            {
                var root =
                    new GameObject("Local Player Provisioning");

                if (parent != null)
                {
                    GameObjectUtility.SetParentAndAlign(
                        root,
                        parent);
                }

                GameObjectUtility.EnsureUniqueNameForSibling(root);

                Undo.RegisterCreatedObjectUndo(
                    root,
                    UndoName);

                PlayerInputManager playerInputManager =
                    Undo.AddComponent<PlayerInputManager>(root);

                LocalPlayerProvisioningAuthoring provisioningAuthoring =
                    Undo.AddComponent<LocalPlayerProvisioningAuthoring>(root);

                LocalPlayerProvisioningHostRegistration hostRegistration =
                    Undo.AddComponent<LocalPlayerProvisioningHostRegistration>(root);

                ConfigurePlayerInputManager(playerInputManager);
                ConfigureProvisioningAuthoring(
                    provisioningAuthoring,
                    playerInputManager);
                ConfigureHostRegistration(
                    hostRegistration,
                    provisioningAuthoring);

                Selection.activeGameObject = root;
                EditorGUIUtility.PingObject(root);

                Undo.CollapseUndoOperations(undoGroup);
                return provisioningAuthoring;
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }
        }

        private static void ConfigurePlayerInputManager(
            PlayerInputManager playerInputManager)
        {
            Undo.RecordObject(
                playerInputManager,
                UndoName);

            playerInputManager.joinBehavior =
                PlayerJoinBehavior.JoinPlayersManually;
            playerInputManager.notificationBehavior =
                PlayerNotifications.InvokeCSharpEvents;

            EditorUtility.SetDirty(playerInputManager);
        }

        private static void ConfigureProvisioningAuthoring(
            LocalPlayerProvisioningAuthoring provisioningAuthoring,
            PlayerInputManager playerInputManager)
        {
            Undo.RecordObject(
                provisioningAuthoring,
                UndoName);

            var serializedAuthoring =
                new SerializedObject(provisioningAuthoring);
            serializedAuthoring.Update();

            SerializedProperty playerInputManagerProperty =
                serializedAuthoring.FindProperty("playerInputManager");
            SerializedProperty localPlayerHostPrefabProperty =
                serializedAuthoring.FindProperty("localPlayerHostPrefab");

            if (playerInputManagerProperty == null ||
                localPlayerHostPrefabProperty == null)
            {
                throw new InvalidOperationException(
                    "Local Player Provisioning serialized composition fields could not be resolved.");
            }

            playerInputManagerProperty.objectReferenceValue =
                playerInputManager;

            // Host Prefab is consumer-owned authoring intent. The Composer does
            // not invent or overwrite it.
            localPlayerHostPrefabProperty.objectReferenceValue = null;

            serializedAuthoring.ApplyModifiedProperties();
            EditorUtility.SetDirty(provisioningAuthoring);
        }

        private static void ConfigureHostRegistration(
            LocalPlayerProvisioningHostRegistration hostRegistration,
            LocalPlayerProvisioningAuthoring provisioningAuthoring)
        {
            Undo.RecordObject(
                hostRegistration,
                UndoName);

            var serializedRegistration =
                new SerializedObject(hostRegistration);
            serializedRegistration.Update();

            SerializedProperty provisioningAuthoringProperty =
                serializedRegistration.FindProperty("provisioningAuthoring");

            if (provisioningAuthoringProperty == null)
            {
                throw new InvalidOperationException(
                    "Local Player Provisioning Host Registration serialized composition field could not be resolved.");
            }

            provisioningAuthoringProperty.objectReferenceValue =
                provisioningAuthoring;

            serializedRegistration.ApplyModifiedProperties();
            EditorUtility.SetDirty(hostRegistration);
        }
    }
}
