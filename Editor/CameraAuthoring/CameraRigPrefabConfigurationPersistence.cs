using System;
using System.Collections.Generic;
using Immersive.Framework.CameraAuthoring;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    internal static class CameraRigPrefabConfigurationPersistence
    {
        private sealed class PropertyOverride
        {
            public UnityEngine.Object Instance;
            public UnityEngine.Object Source;
            public string PropertyPath;
            public string AssetPath;
        }

        private sealed class AddedObjectOverride
        {
            public Component Component;
            public GameObject GameObject;
            public string AssetPath;
        }

        private static readonly string[] ComposerProperties =
        {
            "cinemachineCamera",
            "materializedPresentationIntent",
            "frameworkOwnedCinemachineCamera",
            "frameworkOwnedPositionControl",
            "frameworkOwnedRotationControl",
            "frameworkOwnedGroupTargetGroup",
            "frameworkOwnedGroupFraming",
            "materializationRevision",
            "hasCapturedCameraPosition",
            "capturedCameraLocalPosition",
            "hasCapturedCameraRotation",
            "capturedCameraLocalEulerAngles",
            "hasCapturedCameraLens",
            "capturedLensMode",
            "capturedFieldOfView",
            "capturedOrthographicSize",
            "capturedNearClipPlane",
            "capturedFarClipPlane",
            "capturedDutch"
        };

        public static bool TryPersist(
            CameraRigComposer composer,
            CinemachineCamera camera,
            bool capturePosition,
            bool captureRotation,
            out string issue)
        {
            issue = string.Empty;
            if (composer == null || camera == null)
            {
                issue = "The Composer or CinemachineCamera disappeared before prefab persistence.";
                return false;
            }

            PrefabStage stage = PrefabStageUtility.GetPrefabStage(composer.gameObject);
            if (stage != null && stage.scene == composer.gameObject.scene)
            {
                try
                {
                    PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot, stage.assetPath, out bool saved);
                    if (!saved)
                    {
                        issue = $"Unity did not confirm saving Prefab Stage '{stage.assetPath}'.";
                        return false;
                    }
                    return true;
                }
                catch (Exception exception)
                {
                    issue = $"Could not save Prefab Stage '{stage.assetPath}': {exception.Message}";
                    return false;
                }
            }

            var authoredObjects = new List<UnityEngine.Object>
            {
                composer,
                camera
            };
            if (camera.transform != composer.transform)
                authoredObjects.Add(camera.transform);
            CinemachineFollow follow = camera.GetComponent<CinemachineFollow>();
            CinemachinePositionComposer screen = camera.GetComponent<CinemachinePositionComposer>();
            if (follow != null)
                authoredObjects.Add(follow);
            if (screen != null)
                authoredObjects.Add(screen);

            var overrides = new List<PropertyOverride>();
            var addedOverrides = new List<AddedObjectOverride>();
            var addedGameObjects = new HashSet<GameObject>();
            var changedAssets = new HashSet<UnityEngine.Object>();
            foreach (UnityEngine.Object authoredObject in authoredObjects)
            {
                if (authoredObject == null || !PrefabUtility.IsPartOfPrefabInstance(authoredObject))
                    continue;

                if (authoredObject is CinemachineCamera ownedCamera &&
                    composer.FrameworkOwnedCinemachineCamera == ownedCamera &&
                    PrefabUtility.IsAddedGameObjectOverride(ownedCamera.gameObject))
                {
                    if (!TryCollectAddedGameObject(ownedCamera.gameObject, addedOverrides, out issue))
                        return false;
                    addedGameObjects.Add(ownedCamera.gameObject);
                    continue;
                }

                if (authoredObject is Component componentOnAddedObject &&
                    addedGameObjects.Contains(componentOnAddedObject.gameObject))
                    continue;

                if (authoredObject is Component addedComponent &&
                    PrefabUtility.IsAddedComponentOverride(addedComponent))
                {
                    if (!IsRecordedFrameworkOwnedControl(composer, addedComponent))
                    {
                        issue = $"Added external component '{addedComponent.GetType().Name}' cannot be persisted as a Framework-owned camera control.";
                        return false;
                    }
                    if (!TryCollectAddedComponent(addedComponent, addedOverrides, out issue))
                        return false;
                    continue;
                }

                string[] allowedProperties = GetAllowedProperties(
                    authoredObject,
                    capturePosition,
                    captureRotation);
                if (allowedProperties.Length == 0)
                    continue;

                if (!TryCollectProperties(authoredObject, allowedProperties, overrides, out issue))
                    return false;
            }

            foreach (AddedObjectOverride addedOverride in addedOverrides)
            {
                try
                {
                    if (addedOverride.GameObject != null)
                        PrefabUtility.ApplyAddedGameObject(
                            addedOverride.GameObject,
                            addedOverride.AssetPath,
                            InteractionMode.UserAction);
                    else
                        PrefabUtility.ApplyAddedComponent(
                            addedOverride.Component,
                            addedOverride.AssetPath,
                            InteractionMode.UserAction);

                    UnityEngine.Object addedAsset = AssetDatabase.LoadMainAssetAtPath(addedOverride.AssetPath);
                    if (addedAsset != null)
                        changedAssets.Add(addedAsset);
                }
                catch (Exception exception)
                {
                    issue = $"Could not persist Framework-owned prefab object '{(UnityEngine.Object)addedOverride.GameObject ?? addedOverride.Component}': {exception.Message}";
                    return false;
                }
            }

            foreach (PropertyOverride propertyOverride in overrides)
            {
                var serialized = new SerializedObject(propertyOverride.Instance);
                SerializedProperty property = serialized.FindProperty(propertyOverride.PropertyPath);
                if (property == null)
                {
                    issue = $"Prefab override '{propertyOverride.PropertyPath}' on '{propertyOverride.Instance.name}' could not be read.";
                    return false;
                }

                try
                {
                    PrefabUtility.ApplyPropertyOverride(
                        property,
                        propertyOverride.AssetPath,
                        InteractionMode.UserAction);
                    changedAssets.Add(propertyOverride.Source);
                }
                catch (Exception exception)
                {
                    issue = $"Could not persist authored property '{propertyOverride.PropertyPath}' from '{propertyOverride.Instance.name}' to '{propertyOverride.AssetPath}': {exception.Message}";
                    return false;
                }
            }

            foreach (UnityEngine.Object source in changedAssets)
            {
                if (source == null)
                    continue;

                try
                {
                    AssetDatabase.SaveAssetIfDirty(source);
                }
                catch (Exception exception)
                {
                    issue = $"Could not save prefab asset '{AssetDatabase.GetAssetPath(source)}': {exception.Message}";
                    return false;
                }

                if (EditorUtility.IsDirty(source))
                {
                    issue = $"Unity did not confirm saving prefab asset '{AssetDatabase.GetAssetPath(source)}'.";
                    return false;
                }
            }

            return true;
        }

        private static bool TryCollectAddedGameObject(
            GameObject gameObject,
            List<AddedObjectOverride> overrides,
            out string issue)
        {
            string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                issue = $"Could not resolve the source prefab for Framework-owned object '{gameObject.name}'.";
                return false;
            }

            overrides.Add(new AddedObjectOverride { GameObject = gameObject, AssetPath = assetPath });
            issue = string.Empty;
            return true;
        }

        private static bool TryCollectAddedComponent(
            Component component,
            List<AddedObjectOverride> overrides,
            out string issue)
        {
            string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(component);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                issue = $"Could not resolve the source prefab for Framework-owned component '{component.GetType().Name}'.";
                return false;
            }

            overrides.Add(new AddedObjectOverride { Component = component, AssetPath = assetPath });
            issue = string.Empty;
            return true;
        }

        private static bool IsRecordedFrameworkOwnedControl(
            CameraRigComposer composer,
            Component component) =>
            composer.FrameworkOwnedPositionControl == component ||
            composer.FrameworkOwnedRotationControl == component;

        private static string[] GetAllowedProperties(
            UnityEngine.Object authoredObject,
            bool capturePosition,
            bool captureRotation)
        {
            if (authoredObject is CameraRigComposer)
                return ComposerProperties;
            if (authoredObject is CinemachineCamera)
                return new[] { "Lens" };
            if (authoredObject is CinemachineFollow)
                return new[] { "Follow Offset", "Tracker Settings" };
            if (authoredObject is CinemachinePositionComposer)
                return new[]
                {
                    "Camera Distance",
                    "Dead Zone Depth",
                    "Composition",
                    "Center On Activate",
                    "Target Offset",
                    "Damping",
                    "Lookahead"
                };
            if (authoredObject is Transform)
            {
                var properties = new List<string>();
                if (capturePosition)
                    properties.Add("m_LocalPosition");
                if (captureRotation)
                    properties.Add("m_LocalRotation");
                return properties.ToArray();
            }
            return Array.Empty<string>();
        }

        private static bool TryCollectProperties(
            UnityEngine.Object instance,
            IReadOnlyCollection<string> allowedProperties,
            List<PropertyOverride> overrides,
            out string issue)
        {
            issue = string.Empty;
            UnityEngine.Object source = PrefabUtility.GetCorrespondingObjectFromSource(instance);
            string assetPath = source != null ? AssetDatabase.GetAssetPath(source) : string.Empty;
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                issue = $"Could not resolve the source prefab for '{instance.name}' ({instance.GetType().Name}).";
                return false;
            }

            PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
            var serialized = new SerializedObject(instance);
            SerializedProperty property = serialized.GetIterator();
            var matching = new List<string>();
            while (property.Next(true))
            {
                if (!IsAllowedProperty(property, allowedProperties) ||
                    !property.prefabOverride)
                    continue;
                matching.Add(property.propertyPath);
            }

            foreach (string propertyPath in matching)
                overrides.Add(new PropertyOverride
                {
                    Instance = instance,
                    Source = source,
                    PropertyPath = propertyPath,
                    AssetPath = assetPath
                });
            return true;
        }

        private static bool IsAllowedProperty(
            SerializedProperty property,
            IReadOnlyCollection<string> allowedProperties)
        {
            if (property.depth != 1)
                return false;

            foreach (string allowed in allowedProperties)
                if (string.Equals(property.name, allowed, StringComparison.Ordinal) ||
                    string.Equals(property.displayName, allowed, StringComparison.Ordinal))
                    return true;
            return false;
        }
    }
}
