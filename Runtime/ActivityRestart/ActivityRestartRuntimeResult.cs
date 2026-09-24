using Immersive.Framework.Reset;

namespace Immersive.Framework.ActivityRestart
{
    internal readonly struct ActivityRestartRuntimeResult
    {
        internal ActivityRestartRuntimeResult(
            ActivityRestartResult result,
            ResetSelectionResolution selectionResolution,
            ResetExecutionResult resetExecutionResult)
        {
            Result = result;
            SelectionResolution = selectionResolution;
            ResetExecutionResult = resetExecutionResult;
        }

        internal ActivityRestartResult Result { get; }

        internal ResetSelectionResolution SelectionResolution { get; }

        internal ResetExecutionResult ResetExecutionResult { get; }

        internal static ActivityRestartRuntimeResult From(
            ActivityRestartResult result,
            ResetSelectionResolution selectionResolution = default)
        {
            return new ActivityRestartRuntimeResult(
                result,
                selectionResolution,
                result != null ? result.ResetExecutionResult : default);
        }
    }
}
