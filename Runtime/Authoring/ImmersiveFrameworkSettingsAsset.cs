using Immersive.Framework.ApiStatus;
using Immersive.Logging.Unity;
using UnityEngine;

namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// Project-level backing asset for Immersive Framework settings.
    /// Users should edit this through Project Settings > Immersive Framework.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable product authoring surface for application/route/activity configuration. Breaking changes require ADR/migration.")]
    public sealed class ImmersiveFrameworkSettingsAsset : ScriptableObject
    {
        public const string ResourcesPath = "ImmersiveFrameworkSettings";

        [SerializeField]
        private GameApplicationAsset activeGameApplication;

        [SerializeField]
        private FrameworkEditorPlayModeStartup editorPlayModeStartup = FrameworkEditorPlayModeStartup.FrameworkStartup;

        [SerializeField]
        private LoggingConfigAsset loggingConfig;

        public GameApplicationAsset ActiveGameApplication => activeGameApplication;

        public FrameworkEditorPlayModeStartup EditorPlayModeStartup => editorPlayModeStartup;

        public LoggingConfigAsset LoggingConfig => loggingConfig;
    }
}
