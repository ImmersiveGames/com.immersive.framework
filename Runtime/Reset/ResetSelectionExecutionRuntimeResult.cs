namespace Immersive.Framework.Reset
{
    internal readonly struct ResetSelectionExecutionRuntimeResult
    {
        internal ResetSelectionExecutionRuntimeResult(
            ResetSelectionResolution selectionResolution,
            ResetExecutionResult executionResult)
        {
            SelectionResolution = selectionResolution;
            ExecutionResult = executionResult;
        }

        internal ResetSelectionResolution SelectionResolution { get; }

        internal ResetExecutionResult ExecutionResult { get; }
    }
}
