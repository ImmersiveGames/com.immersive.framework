using System.Threading.Tasks;
using Immersive.Framework.Common;
using Immersive.Framework.Reset;

namespace Immersive.Framework.ApplicationLifecycle
{
    internal sealed partial class FrameworkRuntimeHost : IResetSelectionExecutionRuntimePort
    {
        async Task<ResetSelectionExecutionRuntimeResult> IResetSelectionExecutionRuntimePort.ExecuteResetSelectionAsync(
            ResetSelectionConfig selection,
            string source,
            string reason)
        {
            string resolvedSource = source.NormalizeTextOrFallback(nameof(IResetSelectionExecutionRuntimePort));
            string resolvedReason = reason.NormalizeText();

            if (selection == null)
            {
                ResetIssue issue = ResetIssue.Error(
                    ResetIssueKind.InvalidRequest,
                    "Reset selection execution requires a non-null selection configuration.");
                ResetSelectionResolution rejectedSelection = ResetSelectionResolution.FailedResult(
                    ResetSelectionMode.ExplicitSubjects,
                    ResetSelectionResolutionStatus.RejectedInvalidRequest,
                    issue,
                    resolvedSource,
                    resolvedReason,
                    "Reset selection execution failed because the selection configuration is unavailable.");
                ResetExecutionResult rejectedExecution = ResetExecutionResult.RejectedInvalidRequest(
                    issue,
                    resolvedSource,
                    resolvedReason);
                return new ResetSelectionExecutionRuntimeResult(
                    rejectedSelection,
                    rejectedExecution);
            }

            ResetSelectionResolution selectionResolution = selection.Resolve(
                this,
                resolvedSource,
                resolvedReason);
            if (selectionResolution.Failed)
            {
                ResetIssue issue = selectionResolution.Issues.Count > 0
                    ? selectionResolution.Issues[0]
                    : ResetIssue.Error(
                        ResetIssueKind.InvalidRequest,
                        "Reset selection failed before execution.");
                ResetExecutionResult rejectedExecution = ResetExecutionResult.RejectedInvalidRequest(
                    issue,
                    resolvedSource,
                    resolvedReason);
                return new ResetSelectionExecutionRuntimeResult(
                    selectionResolution,
                    rejectedExecution);
            }

            ResetExecutionRequest request = selection.CreateExecutionRequest(selectionResolution);
            ResetExecutionResult executionResult = await ((IResetExecutionRuntimePort)this).ExecuteResetAsync(request);
            return new ResetSelectionExecutionRuntimeResult(
                selectionResolution,
                executionResult);
        }
    }
}
