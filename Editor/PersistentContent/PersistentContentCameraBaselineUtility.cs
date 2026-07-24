using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Immersive.Framework.Editor.Editor.PersistentContent
{
    /// <summary>
    /// Explicit one-shot authoring utility used to prepare the minimum persistent
    /// Camera composition before the scene is promoted to an official Scene Template.
    ///
    /// The utility never opens, saves, repairs or rebuilds a scene implicitly.
    /// The assigned Content Scene must already be open. Existing complete composition
    /// is preserved; partial or conflicting Camera content blocks the operation.
    /// </summary>
    internal static class PersistentContentCameraBaselineUtility
    {
        private const string RootObjectName =
            "Persistent Camera";
        private const string OutputObjectName =
            "Camera Output";
        private const string TargetObjectName =
            "Session Camera Target";
        private const string RigObjectName =
            "Session Camera Rig";

        private const string OutputId =
            "camera.output.main";
        private const string ScopeId =
            "camera.scope.session.default";
        private const string RequestId =
            "camera.request.session.default";
        private const string TieBreakerId =
            "camera.tie.session.default";
        private const int SessionPrecedence = 300;

        private static readonly Vector3 CameraPosition =
            new Vector3(0f, 5f, -8f);
        private static readonly Vector3 FollowOffset =
            new Vector3(0f, 5f, -8f);

        internal static bool TryCreateOrPreserve(
            GameApplicationAsset gameApplication,
            out GameObject cameraRoot,
            out string diagnostic)
        {
            cameraRoot = null;

            if (gameApplication == null)
            {
                diagnostic =
                    "Camera baseline requires a Game Application.";
                return false;
            }

            PersistentContentComposition composition =
                gameApplication.PersistentContent;
            SceneAsset sceneAsset =
                composition?.ContainerScene as SceneAsset;

            if (sceneAsset == null)
            {
                diagnostic =
                    "Assign a Persistent Content Scene before creating the Camera baseline.";
                return false;
            }

            string scenePath =
                AssetDatabase.GetAssetPath(sceneAsset);
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                diagnostic =
                    "The assigned Persistent Content Scene has no valid asset path.";
                return false;
            }

            Scene scene =
                SceneManager.GetSceneByPath(scenePath);
            if (!scene.IsValid() ||
                !scene.isLoaded)
            {
                diagnostic =
                    $"Open Persistent Content Scene '{sceneAsset.name}' before creating the Camera baseline.";
                return false;
            }

            UnityEngine.Camera[] cameras =
                GetSceneComponents<UnityEngine.Camera>(scene);
            CinemachineBrain[] brains =
                GetSceneComponents<CinemachineBrain>(scene);
            CameraOutputSessionBinding[] outputBindings =
                GetSceneComponents<CameraOutputSessionBinding>(scene);
            SessionCameraOverrideBinding[] sessionBindings =
                GetSceneComponents<SessionCameraOverrideBinding>(scene);
            CameraRigComposer[] rigComposers =
                GetSceneComponents<CameraRigComposer>(scene);
            CinemachineCamera[] cinemachineCameras =
                GetSceneComponents<CinemachineCamera>(scene);

            if (TryResolveCompleteExistingBaseline(
                    gameApplication,
                    cameras,
                    brains,
                    outputBindings,
                    sessionBindings,
                    rigComposers,
                    cinemachineCameras,
                    out cameraRoot))
            {
                diagnostic =
                    "Persistent Content Camera baseline is already complete. No objects were created or changed.";
                return true;
            }

            if (HasAnyCameraComposition(
                    cameras,
                    brains,
                    outputBindings,
                    sessionBindings,
                    rigComposers,
                    cinemachineCameras))
            {
                diagnostic =
                    "Persistent Content Scene contains a partial or conflicting Camera composition. The framework will not repair or merge it automatically. Complete or remove the existing Camera objects manually, then run validation.";
                return false;
            }

            int undoGroup =
                Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(
                "Create Persistent Content Camera Baseline");

            try
            {
                cameraRoot =
                    CreateSceneObject(
                        RootObjectName,
                        scene,
                        null);

                GameObject outputObject =
                    CreateSceneObject(
                        OutputObjectName,
                        scene,
                        cameraRoot.transform);
                GameObject targetObject =
                    CreateSceneObject(
                        TargetObjectName,
                        scene,
                        cameraRoot.transform);
                GameObject rigObject =
                    CreateSceneObject(
                        RigObjectName,
                        scene,
                        cameraRoot.transform);

                ConfigureTransforms(
                    outputObject.transform,
                    targetObject.transform,
                    rigObject.transform);

                UnityEngine.Camera unityCamera =
                    Undo.AddComponent<UnityEngine.Camera>(
                        outputObject);
                CinemachineBrain brain =
                    Undo.AddComponent<CinemachineBrain>(
                        outputObject);
                CameraOutputSessionBinding outputBinding =
                    Undo.AddComponent<CameraOutputSessionBinding>(
                        outputObject);
                SessionCameraOverrideBinding sessionBinding =
                    Undo.AddComponent<SessionCameraOverrideBinding>(
                        outputObject);

                CinemachineCamera cinemachineCamera =
                    Undo.AddComponent<CinemachineCamera>(
                        rigObject);
                CinemachineFollow follow =
                    Undo.AddComponent<CinemachineFollow>(
                        rigObject);
                Undo.AddComponent<CinemachineRotationComposer>(
                    rigObject);
                CameraRigComposer rigComposer =
                    Undo.AddComponent<CameraRigComposer>(
                        rigObject);

                ConfigureCinemachine(
                    cinemachineCamera,
                    follow,
                    targetObject.transform);
                ConfigureRigComposer(
                    rigComposer,
                    cinemachineCamera,
                    targetObject.transform);
                ConfigureOutputBinding(
                    outputBinding,
                    unityCamera,
                    brain);
                ConfigureSessionBinding(
                    sessionBinding,
                    gameApplication,
                    outputBinding,
                    rigComposer,
                    targetObject.transform);

                EditorSceneManager.MarkSceneDirty(scene);
                Selection.activeGameObject =
                    cameraRoot;
                EditorGUIUtility.PingObject(
                    cameraRoot);

                Undo.CollapseUndoOperations(
                    undoGroup);

                diagnostic =
                    $"Created minimum Persistent Content Camera baseline in scene '{scene.name}'. Save the scene, then run Validate Configuration.";
                return true;
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(
                    undoGroup);
                cameraRoot = null;
                diagnostic =
                    $"Persistent Content Camera baseline creation failed explicitly. exception='{exception.GetType().Name}' message='{exception.Message}'.";
                return false;
            }
        }

        private static bool TryResolveCompleteExistingBaseline(
            GameApplicationAsset gameApplication,
            IReadOnlyList<UnityEngine.Camera> cameras,
            IReadOnlyList<CinemachineBrain> brains,
            IReadOnlyList<CameraOutputSessionBinding> outputBindings,
            IReadOnlyList<SessionCameraOverrideBinding> sessionBindings,
            IReadOnlyList<CameraRigComposer> rigComposers,
            IReadOnlyList<CinemachineCamera> cinemachineCameras,
            out GameObject cameraRoot)
        {
            cameraRoot = null;

            if (cameras.Count != 1 ||
                brains.Count != 1 ||
                outputBindings.Count != 1 ||
                sessionBindings.Count != 1 ||
                rigComposers.Count != 1 ||
                cinemachineCameras.Count != 1)
            {
                return false;
            }

            CameraOutputSessionBinding output =
                outputBindings[0];
            SessionCameraOverrideBinding session =
                sessionBindings[0];
            CameraRigComposer rig =
                rigComposers[0];

            bool complete =
                !string.IsNullOrWhiteSpace(output.OutputIdText) &&
                output.UnityCamera == cameras[0] &&
                output.CinemachineBrain == brains[0] &&
                cameras[0].gameObject == brains[0].gameObject &&
                session.AssignedGameApplication == gameApplication &&
                session.PersistentOutputSession == output &&
                !string.IsNullOrWhiteSpace(session.ScopeId) &&
                !string.IsNullOrWhiteSpace(session.RequestIdText) &&
                session.RigComposer == rig &&
                session.TargetSource != null &&
                !string.IsNullOrWhiteSpace(session.TieBreakerId) &&
                rig.CinemachineCamera == cinemachineCameras[0];

            if (!complete)
            {
                return false;
            }

            cameraRoot =
                output.transform.root.gameObject;
            return true;
        }

        private static bool HasAnyCameraComposition(
            IReadOnlyCollection<UnityEngine.Camera> cameras,
            IReadOnlyCollection<CinemachineBrain> brains,
            IReadOnlyCollection<CameraOutputSessionBinding> outputBindings,
            IReadOnlyCollection<SessionCameraOverrideBinding> sessionBindings,
            IReadOnlyCollection<CameraRigComposer> rigComposers,
            IReadOnlyCollection<CinemachineCamera> cinemachineCameras)
        {
            return cameras.Count > 0 ||
                   brains.Count > 0 ||
                   outputBindings.Count > 0 ||
                   sessionBindings.Count > 0 ||
                   rigComposers.Count > 0 ||
                   cinemachineCameras.Count > 0;
        }

        private static GameObject CreateSceneObject(
            string objectName,
            Scene scene,
            Transform parent)
        {
            var gameObject =
                new GameObject(objectName);
            Undo.RegisterCreatedObjectUndo(
                gameObject,
                $"Create {objectName}");

            SceneManager.MoveGameObjectToScene(
                gameObject,
                scene);

            if (parent != null)
            {
                Undo.SetTransformParent(
                    gameObject.transform,
                    parent,
                    $"Parent {objectName}");
            }

            return gameObject;
        }

        private static void ConfigureTransforms(
            Transform output,
            Transform target,
            Transform rig)
        {
            Undo.RecordObject(
                output,
                "Configure Camera Output Transform");
            output.localPosition =
                CameraPosition;
            output.localRotation =
                Quaternion.LookRotation(
                    -CameraPosition.normalized,
                    Vector3.up);
            output.localScale =
                Vector3.one;

            Undo.RecordObject(
                target,
                "Configure Session Camera Target");
            target.localPosition =
                Vector3.zero;
            target.localRotation =
                Quaternion.identity;
            target.localScale =
                Vector3.one;

            Undo.RecordObject(
                rig,
                "Configure Session Camera Rig Transform");
            rig.localPosition =
                Vector3.zero;
            rig.localRotation =
                Quaternion.identity;
            rig.localScale =
                Vector3.one;
        }

        private static void ConfigureCinemachine(
            CinemachineCamera cinemachineCamera,
            CinemachineFollow follow,
            Transform target)
        {
            Undo.RecordObject(
                cinemachineCamera,
                "Configure Cinemachine Camera");
            cinemachineCamera.Follow =
                target;
            cinemachineCamera.LookAt =
                target;
            EditorUtility.SetDirty(
                cinemachineCamera);

            Undo.RecordObject(
                follow,
                "Configure Cinemachine Follow");
            follow.FollowOffset =
                FollowOffset;
            EditorUtility.SetDirty(
                follow);
        }

        private static void ConfigureRigComposer(
            CameraRigComposer rigComposer,
            CinemachineCamera cinemachineCamera,
            Transform target)
        {
            var serializedObject =
                new SerializedObject(rigComposer);

            SetEnum(
                serializedObject,
                "presentationIntent",
                (int)CameraRigPresentationIntent.Follow);
            SetEnum(
                serializedObject,
                "targetSourceKind",
                (int)CameraTargetSourceKind.ExplicitTransform);
            SetObject(
                serializedObject,
                "targetSource",
                null);
            SetObject(
                serializedObject,
                "explicitFollowTarget",
                target);
            SetObject(
                serializedObject,
                "explicitLookAtTarget",
                target);
            SetEnum(
                serializedObject,
                "followRequirement",
                (int)CameraTargetRequirement.Required);
            SetEnum(
                serializedObject,
                "lookAtRequirement",
                (int)CameraTargetRequirement.Optional);
            SetVector3(
                serializedObject,
                "followOffset",
                FollowOffset);
            SetObject(
                serializedObject,
                "cinemachineCamera",
                cinemachineCamera);
            SetBool(
                serializedObject,
                "createCinemachineCameraIfMissing",
                false);
            SetString(
                serializedObject,
                "cinemachineCameraObjectName",
                RigObjectName);
            SetBool(
                serializedObject,
                "logApplyRebuildDiagnostics",
                true);

            serializedObject.ApplyModifiedProperties();
        }

        private static void ConfigureOutputBinding(
            CameraOutputSessionBinding outputBinding,
            UnityEngine.Camera unityCamera,
            CinemachineBrain brain)
        {
            var serializedObject =
                new SerializedObject(outputBinding);

            SetString(
                serializedObject,
                "outputId",
                OutputId);
            SetObject(
                serializedObject,
                "unityCamera",
                unityCamera);
            SetObject(
                serializedObject,
                "cinemachineBrain",
                brain);
            SetBool(
                serializedObject,
                "initializeOnAwake",
                true);
            SetBool(
                serializedObject,
                "logDiagnostics",
                true);

            serializedObject.ApplyModifiedProperties();
        }

        private static void ConfigureSessionBinding(
            SessionCameraOverrideBinding sessionBinding,
            GameApplicationAsset gameApplication,
            CameraOutputSessionBinding outputBinding,
            CameraRigComposer rigComposer,
            Transform target)
        {
            var serializedObject =
                new SerializedObject(sessionBinding);

            SetObject(
                serializedObject,
                "assignedGameApplication",
                gameApplication);
            SetObject(
                serializedObject,
                "persistentOutputSession",
                outputBinding);
            SetString(
                serializedObject,
                "scopeId",
                ScopeId);
            SetString(
                serializedObject,
                "requestId",
                RequestId);
            SetObject(
                serializedObject,
                "rigComposer",
                rigComposer);
            SetObject(
                serializedObject,
                "targetSource",
                target);
            SetInt(
                serializedObject,
                "precedence",
                SessionPrecedence);
            SetString(
                serializedObject,
                "tieBreakerId",
                TieBreakerId);
            SetBool(
                serializedObject,
                "logDiagnostics",
                true);

            serializedObject.ApplyModifiedProperties();
        }

        private static void SetString(
            SerializedObject serializedObject,
            string propertyName,
            string value)
        {
            SerializedProperty property =
                RequireProperty(
                    serializedObject,
                    propertyName);
            property.stringValue =
                value ?? string.Empty;
        }

        private static void SetObject(
            SerializedObject serializedObject,
            string propertyName,
            Object value)
        {
            SerializedProperty property =
                RequireProperty(
                    serializedObject,
                    propertyName);
            property.objectReferenceValue =
                value;
        }

        private static void SetInt(
            SerializedObject serializedObject,
            string propertyName,
            int value)
        {
            SerializedProperty property =
                RequireProperty(
                    serializedObject,
                    propertyName);
            property.intValue =
                value;
        }

        private static void SetEnum(
            SerializedObject serializedObject,
            string propertyName,
            int value)
        {
            SerializedProperty property =
                RequireProperty(
                    serializedObject,
                    propertyName);
            property.intValue =
                value;
        }

        private static void SetBool(
            SerializedObject serializedObject,
            string propertyName,
            bool value)
        {
            SerializedProperty property =
                RequireProperty(
                    serializedObject,
                    propertyName);
            property.boolValue =
                value;
        }

        private static void SetVector3(
            SerializedObject serializedObject,
            string propertyName,
            Vector3 value)
        {
            SerializedProperty property =
                RequireProperty(
                    serializedObject,
                    propertyName);
            property.vector3Value =
                value;
        }

        private static SerializedProperty RequireProperty(
            SerializedObject serializedObject,
            string propertyName)
        {
            SerializedProperty property =
                serializedObject.FindProperty(
                    propertyName);

            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Serialized field '{propertyName}' was not found on '{serializedObject.targetObject.GetType().FullName}'.");
            }

            return property;
        }

        private static TComponent[] GetSceneComponents<TComponent>(
            Scene scene)
            where TComponent : Component
        {
            if (!scene.IsValid() ||
                !scene.isLoaded)
            {
                return Array.Empty<TComponent>();
            }

            var components =
                new List<TComponent>();
            GameObject[] roots =
                scene.GetRootGameObjects();

            for (int index = 0;
                 index < roots.Length;
                 index++)
            {
                TComponent[] rootComponents =
                    roots[index]
                        .GetComponentsInChildren<TComponent>(
                            true);

                if (rootComponents != null &&
                    rootComponents.Length > 0)
                {
                    components.AddRange(
                        rootComponents);
                }
            }

            return components.ToArray();
        }
    }
}
