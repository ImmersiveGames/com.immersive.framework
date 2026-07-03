using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Immersive.Framework.Editor.Editor.Validation
{
    internal static class FrameworkEditorSceneValidationUtility
    {
        internal static SceneValidationScope OpenSceneForValidation(string scenePath)
        {
            var alreadyLoadedScene = FindLoadedSceneByPath(scenePath);
            if (alreadyLoadedScene.IsValid())
            {
                return new SceneValidationScope(alreadyLoadedScene, false);
            }

            var openedScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            return new SceneValidationScope(openedScene, true);
        }

        private static Scene FindLoadedSceneByPath(string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                return default;
            }

            var sceneCount = SceneManager.sceneCount;
            for (var i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                if (scene.path == scenePath)
                {
                    return scene;
                }
            }

            return default;
        }
    }

    internal readonly struct SceneValidationScope
    {
        internal SceneValidationScope(Scene scene, bool closeOnDispose)
        {
            Scene = scene;
            _closeOnDispose = closeOnDispose;
        }

        internal Scene Scene { get; }

        private readonly bool _closeOnDispose;

        internal void CloseIfOwned()
        {
            if (!_closeOnDispose || !Scene.IsValid() || !Scene.isLoaded)
            {
                return;
            }

            if (SceneManager.sceneCount <= 1)
            {
                return;
            }

            EditorSceneManager.CloseScene(Scene, true);
        }
    }
}
