using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class LocalPlayerProvisioningRuntimeHostModule
    {
        /// <summary>
        /// Releases the Manager-Provisioned resources for one exact active Leave occurrence.
        /// A previously successful Host-evidence release may be supplied on retry so already
        /// released projection state is not recreated merely to call the physical release bridge.
        /// </summary>
        internal bool TryReleaseManagerProvisionedPlayerForSessionLeave(
            SessionPlayerLeaveToken leaveToken,
            PlayerHostEvidenceResult previousHostEvidenceRelease,
            string source,
            string reason,
            out PlayerHostEvidenceResult hostEvidenceRelease,
            out ManagerProvisionedSessionPlayerLeaveReleaseResult provisioningRelease,
            out string issue)
        {
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(LocalPlayerProvisioningRuntimeHostModule));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "manager-provisioned-session-player-leave");
            hostEvidenceRelease = previousHostEvidenceRelease;
            provisioningRelease = null;
            issue = string.Empty;

            if (!IsReady || bridge == null || runtimeHost == null)
            {
                issue = diagnostic;
                return false;
            }

            if (!leaveToken.IsValid)
            {
                issue = "Manager-Provisioned Session Player Leave requires a valid Leave token.";
                return false;
            }

            if (!runtimeHost.TryGetPlayerActorPreparationRuntime(
                    out PlayerActorPreparationRuntimeHostModule preparation))
            {
                issue =
                    "Manager-Provisioned Session Player Leave requires the ready Player Actor preparation authority.";
                return false;
            }

            if (hostEvidenceRelease == null)
            {
                if (!preparation.TryGetRetainedHostEvidence(
                        leaveToken.PlayerSlotId,
                        out PlayerHostEvidenceSnapshot retained))
                {
                    issue =
                        "Manager-Provisioned Session Player Leave has no retained Host evidence to release before the physical provisioning stage.";
                    return false;
                }

                if (!retained.HasSessionPhysicalHost ||
                    retained.PlayerSlotId != leaveToken.PlayerSlotId ||
                    !retained.HasRetainedHostReference)
                {
                    issue =
                        "Retained Host evidence does not identify the exact Session physical Host required by Session Player Leave.";
                    return false;
                }

                hostEvidenceRelease = preparation.ReleaseSessionPhysicalHost(
                    retained.PlayerSlotId,
                    retained.Host,
                    resolvedSource,
                    resolvedReason + "; release-session-physical-host");
            }

            if (hostEvidenceRelease == null ||
                hostEvidenceRelease.Status != PlayerHostEvidenceStatus.SucceededReleased ||
                !hostEvidenceRelease.PreviousEvidence.HasRetainedHostReference ||
                hostEvidenceRelease.CurrentEvidence.IsRecorded ||
                hostEvidenceRelease.PreviousEvidence.PlayerSlotId !=
                    leaveToken.PlayerSlotId)
            {
                issue = hostEvidenceRelease != null
                    ? "Manager-Provisioned Host evidence release is not exact successful release evidence. " +
                      hostEvidenceRelease.ToDiagnosticString()
                    : "Manager-Provisioned Host evidence release returned no result.";
                return false;
            }

            provisioningRelease = bridge.TryReleaseAdmittedPlayerForSessionLeave(
                leaveToken,
                hostEvidenceRelease,
                resolvedSource,
                resolvedReason + "; release-manager-provisioned-player");
            if (provisioningRelease == null || !provisioningRelease.Succeeded)
            {
                issue = provisioningRelease != null
                    ? provisioningRelease.ToDiagnosticString()
                    : "Manager-Provisioned Session Player resource release returned no result.";
                return false;
            }

            diagnostic = provisioningRelease.ToDiagnosticString();
            return true;
        }
    }
}
