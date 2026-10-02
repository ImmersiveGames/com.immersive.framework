using System.Threading.Tasks;
using Immersive.Framework.Common;
using Immersive.Framework.Reset;

namespace Immersive.Framework.ApplicationLifecycle
{
    internal sealed partial class FrameworkRuntimeHost
    {
        internal static bool ShouldContinueActivityRestartAfterReset(ResetExecutionResult result) => !result.Failed;

        async Task<ResetSelectionExecutionRuntimeResult> IResetTargetExecutionRuntimePort.ExecuteResetTargetAsync(
            ResetTarget target,
            string source,
            string reason)
        {
            string resolvedSource = source.NormalizeTextOrFallback(nameof(IResetTargetExecutionRuntimePort));
            string resolvedReason = reason.NormalizeText();
            ResetSelectionResolution resolution = ResetTargetResolver.Resolve(this, target, resolvedSource, resolvedReason);
            if (resolution.Failed)
            {
                ResetIssue issue = resolution.Issues.Count > 0
                    ? resolution.Issues[0]
                    : ResetIssue.Error(ResetIssueKind.InvalidRequest, "Reset target resolution failed.");
                return new ResetSelectionExecutionRuntimeResult(
                    resolution,
                    ResetExecutionResult.RejectedInvalidRequest(issue, resolvedSource, resolvedReason));
            }

            ResetExecutionRequest request = resolution.ToExecutionRequest(
                allowNoSubjects: true,
                allowNoParticipants: true,
                stopOnFailure: true,
                yieldBetweenSubjects: false);
            ResetExecutionResult execution = await ((IResetExecutionRuntimePort)this).ExecuteResetAsync(request);
            return new ResetSelectionExecutionRuntimeResult(resolution, execution);
        }
    }
}
