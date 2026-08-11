using System;
using Immersive.Framework.ProgressionSave;

namespace Immersive.Framework.ApplicationLifecycle
{
    internal sealed partial class FrameworkRuntimeHost
    {
        private ProgressionSaveApplicationCompositionResult
            _progressionSaveComposition;

        internal bool ProgressionSaveConfigured =>
            _progressionSaveComposition != null &&
            _progressionSaveComposition.Configured;

        internal ProgressionSaveApplicationCompositionStatus
            ProgressionSaveCompositionStatus =>
                _progressionSaveComposition != null
                    ? _progressionSaveComposition.Status
                    : ProgressionSaveApplicationCompositionStatus.Unknown;

        internal ProgressionSaveBackendId ProgressionSaveBackendId =>
            ProgressionSaveConfigured
                ? _progressionSaveComposition.BackendId
                : default;

        internal string ProgressionSaveDiagnostic =>
            _progressionSaveComposition != null
                ? _progressionSaveComposition.Message
                : "Progression Save composition has not been resolved.";

        internal void ApplyProgressionSaveComposition(
            ProgressionSaveApplicationCompositionResult composition)
        {
            if (composition == null)
            {
                throw new ArgumentNullException(
                    nameof(composition));
            }

            if (!composition.Succeeded)
            {
                throw new ArgumentException(
                    "FrameworkRuntimeHost cannot own a failed Progression Save composition.",
                    nameof(composition));
            }

            if (_progressionSaveComposition != null)
            {
                throw new InvalidOperationException(
                    "FrameworkRuntimeHost Progression Save composition is already assigned for this application lifetime.");
            }

            _progressionSaveComposition =
                composition;
        }

        internal bool TryGetProgressionSaveRuntime(
            out ProgressionSaveRuntime runtime)
        {
            runtime =
                ProgressionSaveConfigured
                    ? _progressionSaveComposition.Runtime
                    : null;

            return runtime != null;
        }
    }
}
