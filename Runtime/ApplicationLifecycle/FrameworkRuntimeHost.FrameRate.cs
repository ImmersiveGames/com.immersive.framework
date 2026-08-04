using Immersive.Framework.Performance;
using Immersive.Logging.Records;

namespace Immersive.Framework.ApplicationLifecycle
{
    internal sealed partial class FrameworkRuntimeHost
    {
        private ApplicationFrameRateApplicationResult
            _lastFrameRateApplicationResult;

        internal ApplicationFrameRateApplicationResult
            LastFrameRateApplicationResult =>
                _lastFrameRateApplicationResult;

        private bool TryApplyApplicationFrameRatePolicy(
            out string failureMessage)
        {
            ApplicationFrameRatePolicy policy =
                _gameApplication != null
                    ? _gameApplication.FrameRatePolicy
                    : null;

            _lastFrameRateApplicationResult =
                ApplicationFrameRatePolicyApplier.Apply(policy);

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
