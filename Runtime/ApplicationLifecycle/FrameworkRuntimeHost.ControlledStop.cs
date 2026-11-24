using System;
using System.Threading.Tasks;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Common;
using Immersive.Framework.GameFlow;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.RouteLifecycle;
using Immersive.Logging.Records;

namespace Immersive.Framework.ApplicationLifecycle
{
    internal readonly struct FrameworkRuntimeHostStopResult
    {
        internal FrameworkRuntimeHostStopResult(bool succeeded, string diagnostic)
        {
            Succeeded = succeeded;
            Diagnostic = diagnostic ?? string.Empty;
        }

        internal bool Succeeded { get; }

        internal string Diagnostic { get; }
    }

    internal sealed partial class FrameworkRuntimeHost
    {
        private Task<FrameworkRuntimeHostStopResult> _controlledStopInFlight;

        internal Task<FrameworkRuntimeHostStopResult> StopAsync(string reason)
        {
            if (_controlledStopInFlight != null)
            {
                return _controlledStopInFlight;
            }

            // Publish the task before cleanup can call back into the Host on the Unity thread.
            var completion = new TaskCompletionSource<FrameworkRuntimeHostStopResult>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _controlledStopInFlight = completion.Task;
            _ = CompleteControlledStopAsync(reason, completion);
            return completion.Task;
        }

        private async Task CompleteControlledStopAsync(
            string reason,
            TaskCompletionSource<FrameworkRuntimeHostStopResult> completion)
        {
            FrameworkRuntimeHostStopResult result = default;
            Exception failure = null;
            try
            {
                result = await StopCoreAsync(reason);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                _controlledStopInFlight = null;
            }

            if (failure is OperationCanceledException)
            {
                completion.SetCanceled();
            }
            else if (failure != null)
            {
                completion.SetException(failure);
            }
            else
            {
                completion.SetResult(result);
            }
        }

        private async Task<FrameworkRuntimeHostStopResult> StopCoreAsync(string reason)
        {
            string resolvedReason = reason.NormalizeTextOrFallback("controlled-stop");
            while (true)
            {
                Task operationsDrained = _admission.BeginStopAndWaitForOperationsAsync();
                InvalidateDynamicObjectEntryCompositionWrites();
                await operationsDrained;
                if (LifecyclePhase == FrameworkRuntimeHostPhase.Stopped)
                {
                    return new FrameworkRuntimeHostStopResult(true, string.Empty);
                }

                if (!TryReleaseSessionPlayersForControlledStop(
                        resolvedReason,
                        out string playerReleaseDiagnostic))
                {
                    return RetainControlledStopForRetry(
                        "FrameworkRuntimeHost controlled stop retained the live Host because canonical Session Player Leave did not reach its terminal state.",
                        resolvedReason,
                        playerReleaseDiagnostic);
                }

                string activityReleaseDiagnostic =
                    await TryClearActivityForControlledStopAsync(resolvedReason);
                if (!string.IsNullOrEmpty(activityReleaseDiagnostic))
                {
                    return RetainControlledStopForRetry(
                        "FrameworkRuntimeHost controlled stop retained the live Host because canonical Activity clear did not reach an authoritative terminal state.",
                        resolvedReason,
                        activityReleaseDiagnostic);
                }

                if (!TryReleaseCameraSessionForControlledStop(
                        out string cameraReleaseDiagnostic))
                {
                    return RetainControlledStopForRetry(
                        "FrameworkRuntimeHost controlled stop retained the live Host because canonical Camera cleanup retained actionable receipts.",
                        resolvedReason,
                        cameraReleaseDiagnostic);
                }

                string routeReleaseDiagnostic =
                    await TryExitRouteForControlledStopAsync(resolvedReason);
                if (!string.IsNullOrEmpty(routeReleaseDiagnostic))
                {
                    return RetainControlledStopForRetry(
                        "FrameworkRuntimeHost controlled stop retained the live Host because canonical Route exit did not release every retained obligation.",
                        resolvedReason,
                        routeReleaseDiagnostic);
                }

                if (_admission.TryBeginStopFinalization())
                {
                    break;
                }
            }

            if (!TryReleasePersistentContentScope(
                    resolvedReason,
                    out string releaseDiagnostic))
            {
                _admission.CompleteStop(succeeded: false);
                return RetainControlledStopForRetry(
                    "FrameworkRuntimeHost controlled stop retained the live Host for retry.",
                    resolvedReason,
                    releaseDiagnostic);
            }

            if (!TryReleaseSessionActorGroups(out string actorGroupReleaseDiagnostic))
            {
                _admission.CompleteStop(succeeded: false);
                return RetainControlledStopForRetry(
                    "FrameworkRuntimeHost controlled stop retained the live Host because Session-owned Actor Groups were not released.",
                    resolvedReason,
                    actorGroupReleaseDiagnostic);
            }

            _admission.CompleteStop(succeeded: true);
            _logger?.Info(
                "FrameworkRuntimeHost controlled stop completed.",
                LogFields.Of(
                    LogFields.Field("phase", LifecyclePhase),
                    LogFields.Field("reason", resolvedReason),
                    LogFields.Field("retryAuthority", "none")));
            return new FrameworkRuntimeHostStopResult(true, string.Empty);
        }

        private FrameworkRuntimeHostStopResult RetainControlledStopForRetry(
            string message,
            string reason,
            string diagnostic)
        {
            _logger?.Error(
                message,
                LogFields.Of(
                    LogFields.Field("phase", LifecyclePhase),
                    LogFields.Field("reason", reason),
                    LogFields.Field("diagnostic", diagnostic),
                    LogFields.Field("retryAuthority", "same-framework-runtime-host")));
            return new FrameworkRuntimeHostStopResult(false, diagnostic);
        }

        private bool TryReleaseSessionPlayersForControlledStop(
            string reason,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            if (!this.TryGetPlayerParticipationSnapshot(
                    out PlayerParticipationSnapshot snapshot))
            {
                return true;
            }

            if (snapshot.ReservedCount > 0)
            {
                diagnostic =
                    $"Controlled stop is waiting for {snapshot.ReservedCount} in-flight Session Player reservation(s) to settle; no reserved Slot can be released through canonical Leave.";
                return false;
            }

            if (snapshot.JoinedCount == 0 && snapshot.LeavingCount == 0)
            {
                return true;
            }

            if (!SessionPlayerLeaveRuntimeHostModule.TryAttach(
                    this,
                    out SessionPlayerLeaveRuntimeHostModule leaveRuntime,
                    out string attachIssue))
            {
                diagnostic =
                    "Controlled stop cannot release Session Players because the canonical Leave owner is unavailable. " +
                    attachIssue;
                return false;
            }

            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                PlayerSlotRuntimeSnapshot slot = snapshot.Slots[index];
                if (slot.AllocationState != PlayerSlotAllocationState.Joined &&
                    slot.AllocationState != PlayerSlotAllocationState.Leaving)
                {
                    continue;
                }

                SessionPlayerLeaveRequest request;
                if (slot.AllocationState == PlayerSlotAllocationState.Joined)
                {
                    request = new SessionPlayerLeaveRequest(
                        slot.PlayerSlotId,
                        slot.Revision,
                        "FrameworkRuntimeHost",
                        reason + "; controlled-stop-canonical-leave");
                }
                else if (!leaveRuntime.TryGetPendingRequest(
                             slot.PlayerSlotId,
                             out request))
                {
                    diagnostic =
                        $"Controlled stop found Slot '{slot.PlayerSlotId.StableText}' already Leaving without a retained canonical Leave request. It will keep the Host and Session roots alive.";
                    return false;
                }

                if (!TryEnterRuntimeOperation(cleanup: true, out IDisposable admission))
                {
                    diagnostic =
                        $"Controlled stop could not enter the canonical Leave cleanup allowlist while Host phase is '{LifecyclePhase}'.";
                    return false;
                }

                SessionPlayerLeaveResult leaveResult;
                using (admission)
                {
                    leaveResult = leaveRuntime.TryLeave(request);
                }

                if (leaveResult == null || !leaveResult.Succeeded)
                {
                    diagnostic = leaveResult != null
                        ? leaveResult.ToDiagnosticString()
                        : "Canonical Session Player Leave returned no result.";
                    return false;
                }
            }

            if (!this.TryGetPlayerParticipationSnapshot(
                    out PlayerParticipationSnapshot afterRelease))
            {
                diagnostic =
                    "Controlled stop cannot verify Session Player Leave because the participation snapshot became unavailable.";
                return false;
            }

            if (afterRelease.JoinedCount > 0 ||
                afterRelease.ReservedCount > 0 ||
                afterRelease.LeavingCount > 0)
            {
                diagnostic =
                    $"Canonical Session Player Leave left an unresolved Session snapshot: joined={afterRelease.JoinedCount}, reserved={afterRelease.ReservedCount}, leaving={afterRelease.LeavingCount}.";
                return false;
            }

            return true;
        }

        private async Task<string> TryClearActivityForControlledStopAsync(
            string reason)
        {
            if (_gameFlowRuntime == null ||
                _gameFlowRuntime.CurrentRoute == null ||
                _gameFlowRuntime.CurrentActivity == null)
            {
                return string.Empty;
            }

            FrameworkActivityRequestResult result = await ClearActivityAsync(
                "FrameworkRuntimeHost",
                reason + "; controlled-stop-canonical-activity-clear");
            if (result.Succeeded || result.DestinationAuthoritative ||
                result.Kind == FrameworkActivityRequestKind.IgnoredNoActiveActivity)
            {
                if (!result.Succeeded && result.DestinationAuthoritative)
                {
                    _logger?.Warning(
                        $"FrameworkRuntimeHost controlled stop observed a committed Activity clear with a post-commit transition failure. kind='{result.Kind}'. {result.Message}");
                }
                return string.Empty;
            }

            return
                $"Canonical Activity clear failed before its destination became authoritative. kind='{result.Kind}'. {result.Message}";
        }

        private async Task<string> TryExitRouteForControlledStopAsync(
            string reason)
        {
            if (_gameFlowRuntime == null)
            {
                return string.Empty;
            }

            if (!TryEnterRuntimeOperation(cleanup: true, out IDisposable admission))
            {
                return $"Controlled stop could not enter Route cleanup while Host phase is '{LifecyclePhase}'.";
            }

            RouteExitResult result;
            using (admission)
            {
                result = await _gameFlowRuntime.ExitRouteForControlledStopAsync(
                    "FrameworkRuntimeHost",
                    reason + "; controlled-stop-canonical-route-exit");
            }

            if (!result.Completed && result.HasRoute)
            {
                return result.Message;
            }

            if (result.Completed)
            {
                _state = _state.WithoutRoutePreservingPrimaryScene();
                InvalidateObjectEntryRuntimeContextSnapshot(
                    "controlled-stop-route-exit");
            }

            return string.Empty;
        }

        private bool TryReleaseCameraSessionForControlledStop(
            out string diagnostic)
        {
            diagnostic = string.Empty;
            if (!TryEnterRuntimeOperation(cleanup: true, out IDisposable admission))
            {
                diagnostic =
                    $"Controlled stop could not enter Camera cleanup while Host phase is '{LifecyclePhase}'.";
                return false;
            }

            using IDisposable admissionLease = admission;
            try
            {
                if (_sessionCameraAssignmentRuntime != null &&
                    !_sessionCameraAssignmentRuntime.TryClearAllAssignments(
                        out diagnostic))
                {
                    diagnostic =
                        "Session Camera Assignment clear retained exact cleanup receipts. " + diagnostic;
                    return false;
                }

                _sessionCameraMembershipRuntime?.Dispose();
                _sessionCameraMembershipRuntime = null;

                if (_playerCameraOutputIntegrationRuntime != null &&
                    !_playerCameraOutputIntegrationRuntime.TryDispose(
                        out diagnostic))
                {
                    diagnostic =
                        "Player Camera Output integration retained exact PlayerInput/Output cleanup receipts. " + diagnostic;
                    return false;
                }
                _playerCameraOutputIntegrationRuntime = null;

                _sessionCameraAssignmentRuntime?.Dispose();
                _sessionCameraAssignmentRuntime = null;

                if (_cameraSessionOutputMaterializationRuntime != null &&
                    !_cameraSessionOutputMaterializationRuntime.TryDispose(
                        out diagnostic))
                {
                    diagnostic =
                        "Camera Output topology/materialization teardown retained exact Output owners. " + diagnostic;
                    return false;
                }
                _cameraSessionOutputMaterializationRuntime = null;
                _cameraOutputTopology = null;
                diagnostic = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                diagnostic =
                    $"Controlled Camera cleanup failed and the Host retained its remaining owners. {exception.GetType().Name}: {exception.Message}";
                return false;
            }
        }
    }
}
