using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.UnityInput;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.Editor.PlayerParticipation
{
    /// <summary>
    /// Canonical Editor creation surface for one Scene-Provided Local Player.
    /// It creates and wires only the deterministic technical composition required
    /// before consumer-authored Slot, Actor and Input Action intent is assigned.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.DevelopmentTooling,
        "Canonical Editor create action for the Scene-Provided Local Player composition.")]
    public static class SceneProvidedLocalPlayerCreator
    {
        private const string MenuPath =
            "GameObject/Immersive Framework/Player/Scene-Provided/Create Local Player";

        private const string UndoName =
            "Create Scene-Provided Local Player";

        [MenuItem(MenuPath, false, 10)]
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

        public static SceneProvidedLocalPlayerAuthoring Create(
            GameObject parent = null)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Scene-Provided Local Player authoring is unavailable while entering or running Play Mode.");
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);

            try
            {
                var root =
                    new GameObject("Scene-Provided Local Player");

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

                PlayerInput playerInput =
                    Undo.AddComponent<PlayerInput>(root);

                LocalPlayerHostAuthoring host =
                    Undo.AddComponent<LocalPlayerHostAuthoring>(root);

                UnityPlayerInputGateAdapter inputGate =
                    Undo.AddComponent<UnityPlayerInputGateAdapter>(root);

                SceneProvidedLocalPlayerAuthoring sceneProvidedLocalPlayer =
                    Undo.AddComponent<SceneProvidedLocalPlayerAuthoring>(root);

                var actorMount =
                    new GameObject("ActorMount");
                GameObjectUtility.SetParentAndAlign(
                    actorMount,
                    root);
                Undo.RegisterCreatedObjectUndo(
                    actorMount,
                    UndoName);

                ConfigureHost(
                    host,
                    playerInput,
                    actorMount.transform);
                ConfigureInputGate(
                    inputGate,
                    playerInput);
                ConfigureSceneProvidedLocalPlayer(
                    sceneProvidedLocalPlayer,
                    host);

                Selection.activeGameObject = root;
                EditorGUIUtility.PingObject(root);

                Undo.CollapseUndoOperations(undoGroup);
                return sceneProvidedLocalPlayer;
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }
        }

        private static void ConfigureHost(
            LocalPlayerHostAuthoring host,
            PlayerInput playerInput,
            Transform actorMount)
        {
            Undo.RecordObject(
                host,
                UndoName);

            var serializedHost =
                new SerializedObject(host);
            serializedHost.Update();

            SerializedProperty playerInputProperty =
                serializedHost.FindProperty("playerInput");
            SerializedProperty actorMountProperty =
                serializedHost.FindProperty("actorMount");

            if (playerInputProperty == null ||
                actorMountProperty == null)
            {
                throw new InvalidOperationException(
                    "Local Player Host serialized composition fields could not be resolved.");
            }

            playerInputProperty.objectReferenceValue =
                playerInput;
            actorMountProperty.objectReferenceValue =
                actorMount;

            serializedHost.ApplyModifiedProperties();
            EditorUtility.SetDirty(host);
        }

        private static void ConfigureInputGate(
            UnityPlayerInputGateAdapter inputGate,
            PlayerInput playerInput)
        {
            Undo.RecordObject(
                inputGate,
                UndoName);

            var serializedInputGate =
                new SerializedObject(inputGate);
            serializedInputGate.Update();

            SerializedProperty playerInputProperty =
                serializedInputGate.FindProperty("playerInput");
            SerializedProperty legacyGameplayActionMapNameProperty =
                serializedInputGate.FindProperty("gameplayActionMapName");

            if (playerInputProperty == null ||
                legacyGameplayActionMapNameProperty == null)
            {
                throw new InvalidOperationException(
                    "Unity PlayerInput Gate serialized composition fields could not be resolved.");
            }

            playerInputProperty.objectReferenceValue =
                playerInput;
            legacyGameplayActionMapNameProperty.stringValue =
                string.Empty;

            serializedInputGate.ApplyModifiedProperties();
            EditorUtility.SetDirty(inputGate);
        }

        private static void ConfigureSceneProvidedLocalPlayer(
            SceneProvidedLocalPlayerAuthoring sceneProvidedLocalPlayer,
            LocalPlayerHostAuthoring host)
        {
            Undo.RecordObject(
                sceneProvidedLocalPlayer,
                UndoName);

            var serializedAdmission =
                new SerializedObject(sceneProvidedLocalPlayer);
            serializedAdmission.Update();

            SerializedProperty hostProperty =
                serializedAdmission.FindProperty("localPlayerHost");
            if (hostProperty == null)
            {
                throw new InvalidOperationException(
                    "Scene-Provided Local Player serialized Host field could not be resolved.");
            }

            hostProperty.objectReferenceValue = host;
            serializedAdmission.ApplyModifiedProperties();
            EditorUtility.SetDirty(sceneProvidedLocalPlayer);
        }
    }
}
