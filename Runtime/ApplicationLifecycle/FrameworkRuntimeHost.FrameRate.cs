using System;
using Immersive.Framework.Authoring;
using Immersive.Framework.Performance;
using Immersive.Framework.PlayerParticipation;
using Immersive.Logging.Records;

namespace Immersive.Framework.ApplicationLifecycle
{
    internal sealed partial class FrameworkRuntimeHost
    {
        private ApplicationFrameRatePolicy _projectFrameRatePolicy;

        private ApplicationFrameRateApplicationResult
            _lastFrameRateApplicationResult;

        internal ApplicationFrameRateApplicationResult
            LastFrameRateApplicationResult =>
                _lastFrameRateApplicationResult;

        /// <summary>
        /// Canonical bootstrap path for a runtime host with an explicit project-level
        /// frame-rate baseline. The policy is resolved before host creation and is not
        /// discovered again from Resources or GameApplicationAsset.
        /// </summary>
        internal static bool TryCreateWithProjectFrameRate(
            GameApplicationAsset gameApplication,
            ApplicationFrameRatePolicy projectFrameRatePolicy,
            PlayerSessionProfile explicitPlayerSessionProfile,
            out FrameworkRuntimeHost runtimeHost,
            out PlayerSessionInitializationResult playerSessionResolution,
            out PlayerParticipationOperationResult playerParticipationInitialization)
        {
            if (projectFrameRatePolicy == null)
            {
                throw new ArgumentNullException(
                    nameof(projectFrameRatePolicy));
            }

            if (!projectFrameRatePolicy.TryValidate(
                    out string frameRateIssue))
            {
                throw new ArgumentException(
                    $"Project Frame Rate policy is invalid. {frameRateIssue}",
                    nameof(projectFrameRatePolicy));
            }

            bool created = TryCreate(
                gameApplication,
                explicitPlayerSessionProfile,
                out runtimeHost,
                out playerSessionResolution,
                out playerParticipationInitialization);

            if (!created)
            {
                return false;
            }

            runtimeHost._projectFrameRatePolicy =
                projectFrameRatePolicy;
            return true;
        }

        private bool TryApplyApplicationFrameRatePolicy(
            out string failureMessage)
        {
            _lastFrameRateApplicationResult =
                ApplicationFrameRatePolicyApplier.Apply(
                    _projectFrameRatePolicy);

            LogFrameRateApplicationResult(
                _lastFrameRateApplicationResult);

            if (_lastFrameRateApplicationResult.Succeeded)
            {
                failureMessage = string.Empty;
                return true;
            }

            failureMessage =
                _lastFrameRateApplicationResult.Message;
            return false;
        }

        private void LogFrameRateApplicationResult(
            ApplicationFrameRateApplicationResult result)
        {
            LogField[] summaryFields =
                LogFields.Of(
                    LogFields.Field("source", "ProjectSettings"),
                    LogFields.Field("status", result.Status),
                    LogFields.Field("mode", result.RequestedMode),
                    LogFields.Field(
                        "appliedTargetFrameRate",
                        result.AppliedTargetFrameRate),
                    LogFields.Field(
                        "appliedVSyncCount",
                        result.AppliedVSyncCount),
                    LogFields.Field("platform", result.Platform));

            if (!result.Succeeded)
            {
                _logger?.Error(
                    "Application frame-rate policy failed.",
                    summaryFields);
            }
            else if (result.IsPlatformLimited)
            {
                _logger?.Warning(
                    "Application frame-rate policy has platform limitations.",
                    summaryFields);
            }
            else
            {
                _logger?.Info(
                    "Application frame-rate policy completed.",
                    summaryFields);
            }

            _logger?.Debug(
                "Application frame-rate policy diagnostics.",
                LogFields.Of(
                    LogFields.Field("source", "ProjectSettings"),
                    LogFields.Field("status", result.Status),
                    LogFields.Field("mode", result.RequestedMode),
                    LogFields.Field(
                        "requestedTargetFrameRate",
                        result.RequestedTargetFrameRate),
                    LogFields.Field(
                        "requestedVSyncCount",
                        result.RequestedVSyncCount),
                    LogFields.Field(
                        "previousTargetFrameRate",
                        result.PreviousTargetFrameRate),
                    LogFields.Field(
                        "previousVSyncCount",
                        result.PreviousVSyncCount),
                    LogFields.Field(
                        "appliedTargetFrameRate",
                        result.AppliedTargetFrameRate),
                    LogFields.Field(
                        "appliedVSyncCount",
                        result.AppliedVSyncCount),
                    LogFields.Field("platform", result.Platform),
                    LogFields.Field("message", result.Message)));
        }
    }
}
