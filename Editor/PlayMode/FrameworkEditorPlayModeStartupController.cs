using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Settings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Immersive.Framework.Editor.PlayMode
{
    /// <summary>
    /// Owns the Unity Editor Play Mode start scene for Immersive Framework startup.
    ///
    /// FrameworkStartup:
    ///   Play starts from the package-owned empty bootstrap scene so scenes that are
    ///   currently open for authoring never enter the runtime scene set.
    ///
    /// CurrentSceneOnly:
    ///   Unity's Play Mode start-scene override is cleared so the currently open
    ///   Editor scene is executed intentionally.
    /// </summary>
    [InitializeOnLoad]
    internal static class FrameworkEditorPlayModeStartupController
    {
        private const string BootstrapScenePath =
            "Packages/com.immersive.framework/Editor/PlayMode/FrameworkPlayModeBootstrap.unity";

        private static bool _synchronizationScheduled;

        static FrameworkEditorPlayModeStartupController()
        {
            EditorApplication.delayCall += SynchronizeFromSettings;
            EditorApplication.projectChanged += ScheduleSynchronization;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            Undo.undoRedoPerformed += ScheduleSynchronization;
        }

        /// <summary>
        /// Reconciles Unity's Editor Play Mode start scene with the authoritative
        /// Immersive Framework project setting.
        ///
        /// This method is intentionally exposed inside the Editor assembly so the
        /// Project Settings provider can synchronize immediately after applying a
        /// Startup setting change.
        /// </summary>
        internal static void SynchronizeFromSettings()
        {
            _synchronizationScheduled = false;

            ImmersiveFrameworkSettingsAsset settings =
                ImmersiveFrameworkEditorSettingsUtility.LoadOrCreateSettingsAsset();

            if (settings == null)
            {
                return;
            }

            switch (settings.EditorPlayModeStartup)
            {
                case FrameworkEditorPlayModeStartup.FrameworkStartup:
                    ApplyFrameworkBootstrapScene(cancelPlayOnFailure: false);
                    break;

                case FrameworkEditorPlayModeStartup.CurrentSceneOnly:
                    ApplyCurrentSceneOnly();
                    break;

                default:
                    Debug.LogError(
                        "[Immersive Framework] Unsupported Editor Play Mode startup value " +
                        $"'{settings.EditorPlayModeStartup}'.");
                    break;
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    ValidateBeforeEnteringPlayMode();
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    ScheduleSynchronization();
                    break;
            }
        }

        private static void ValidateBeforeEnteringPlayMode()
        {
            ImmersiveFrameworkSettingsAsset settings =
                ImmersiveFrameworkEditorSettingsUtility.LoadOrCreateSettingsAsset();

            if (settings == null)
            {
                return;
            }

            switch (settings.EditorPlayModeStartup)
            {
                case FrameworkEditorPlayModeStartup.FrameworkStartup:
                    ApplyFrameworkBootstrapScene(cancelPlayOnFailure: true);
                    break;

                case FrameworkEditorPlayModeStartup.CurrentSceneOnly:
                    ApplyCurrentSceneOnly();
                    break;

                default:
                    Debug.LogError(
                        "[Immersive Framework] Play Mode was cancelled because Editor Play Mode " +
                        $"startup value '{settings.EditorPlayModeStartup}' is unsupported.");
                    EditorApplication.isPlaying = false;
                    break;
            }
        }

        private static void ApplyFrameworkBootstrapScene(bool cancelPlayOnFailure)
        {
            SceneAsset bootstrapScene =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath);

            if (bootstrapScene == null)
            {
                Debug.LogError(
                    "[Immersive Framework] FrameworkStartup requires the package-owned empty " +
                    $"Play Mode bootstrap scene, but it was not found at '{BootstrapScenePath}'. " +
                    "Play Mode must not fall back to the currently open authoring scene.");

                if (cancelPlayOnFailure)
                {
                    EditorApplication.isPlaying = false;
                }

                return;
            }

            if (EditorSceneManager.playModeStartScene == bootstrapScene)
            {
                return;
            }

            EditorSceneManager.playModeStartScene = bootstrapScene;
        }

        private static void ApplyCurrentSceneOnly()
        {
            if (EditorSceneManager.playModeStartScene == null)
            {
                return;
            }

            EditorSceneManager.playModeStartScene = null;
        }

        private static void ScheduleSynchronization()
        {
            if (_synchronizationScheduled)
            {
                return;
            }

            _synchronizationScheduled = true;
            EditorApplication.delayCall += SynchronizeFromSettings;
        }
    }
}
