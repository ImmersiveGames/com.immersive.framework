using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.PlayerParticipation
{
    /// <summary>
    /// Non-canonical Editor utility retained for isolated QA scenes and development harnesses.
    /// Product projects should use an authored Player prefab or manually compose the component.
    /// This utility intentionally has no Unity menu entry.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.DevelopmentTooling,
        "Non-canonical Scene Local Player Admission creator retained for isolated QA and development use. Product authoring uses explicit prefabs or manual composition.")]
    public static class SceneLocalPlayerAdmissionDevelopmentCreator
    {
        public static SceneLocalPlayerAdmissionAuthoring Create(GameObject parent = null)
        {
            var root = new GameObject("Scene Local Player Admission");
            Undo.RegisterCreatedObjectUndo(
                root,
                "Create Development Scene Local Player Admission");

            if (parent != null)
            {
                GameObjectUtility.SetParentAndAlign(root, parent);
            }

            SceneLocalPlayerAdmissionAuthoring authoring =
                root.AddComponent<SceneLocalPlayerAdmissionAuthoring>();

            Selection.activeGameObject = root;
            return authoring;
        }
    }
}
