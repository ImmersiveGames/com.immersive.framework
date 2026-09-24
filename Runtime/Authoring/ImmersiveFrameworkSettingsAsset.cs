using Immersive.Framework.ApiStatus;
using Immersive.Framework.Performance;
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
        [Tooltip("Required project-level frame pacing baseline. Use Unity Defaults is an explicit valid policy that preserves Unity values.")]
        private ApplicationFrameRatePolicy frameRatePolicy =
            new ApplicationFrameRatePolicy();

        [SerializeField]
        private LoggingConfigAsset loggingConfig;

        public GameApplicationAsset ActiveGameApplication => activeGameApplication;

        public FrameworkEditorPlayModeStartup EditorPlayModeStartup => editorPlayModeStartup;

        /// <summary>
        /// Required project-level frame pacing baseline.
        /// Runtime receives this policy explicitly during framework boot.
        /// </summary>
        public ApplicationFrameRatePolicy FrameRatePolicy => frameRatePolicy;

        public LoggingConfigAsset LoggingConfig => loggingConfig;
    }
}
