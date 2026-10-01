using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.Reset.Unity
{
    /// <summary>API status: Experimental. One scene-authored surface for semantic Reset requests.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Reset/Reset Request Trigger")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-035 RESET-035-E semantic Reset request surface.")]
    public sealed class ResetRequestTrigger : MonoBehaviour
    {
        private const string DefaultReason = "Reset Request";

        [SerializeField] private ResetTarget target;
        [SerializeField] private string reason;

        private IResetSelectionExecutionRuntimePort _runtime;
        private bool _requestInFlight;
        private ResetSelectionResolution _lastResolution;
        private ResetExecutionResult _lastResult;
        private string _bindingDiagnostic = "Reset request runtime is not bound.";

        public ResetTarget Target { get => target; set => target = value; }
        public bool IsRequestInFlight => _requestInFlight;
        public ResetSelectionResolution LastResolution => _lastResolution;
        public ResetExecutionResult LastResult => _lastResult;
        public string RuntimeBindingDiagnostic => _bindingDiagnostic;
[ContextMenu("Request Reset")]
private void RequestResetFromContextMenu()
{
    RequestReset();
}
        internal bool TryBind(IResetSelectionExecutionRuntimePort runtime, out string issue)
        {
            if (runtime == null)
            {
                issue = "Reset request trigger requires a non-null runtime port.";
                _bindingDiagnostic = issue;
                return false;
            }

            if (_runtime == null || ReferenceEquals(_runtime, runtime))
            {
                _runtime = runtime;
                issue = string.Empty;
                _bindingDiagnostic = $"Bound '{runtime.GetType().FullName}'.";
                return true;
            }

            issue = "Reset request trigger rejected a different runtime port for the current lifetime.";
            _bindingDiagnostic = issue;
            return false;
        }

        [ContextMenu("Request Reset")]
        public async void RequestReset() => await RequestResetAsync();

        public async Awaitable<ResetExecutionResult> RequestResetAsync()
        {
            string resolvedReason = reason.NormalizeTextOrFallback(DefaultReason);
            if (_requestInFlight) return Fail("Reset request is already in flight.", resolvedReason);
            if (_runtime == null) return Fail(_bindingDiagnostic, resolvedReason);
            if (!target.IsValid) return Fail($"Reset request target is invalid: {target}.", resolvedReason);

            _requestInFlight = true;
            try
            {
                ResetSelectionExecutionRuntimeResult result = await _runtime.ExecuteResetTargetAsync(
                    target, nameof(ResetRequestTrigger), resolvedReason);
                _lastResolution = result.SelectionResolution;
                _lastResult = result.ExecutionResult;
                return _lastResult;
            }
            catch (Exception exception)
            {
                return Fail($"Reset request failed with exception '{exception.GetType().Name}': {exception.Message}", resolvedReason);
            }
            finally
            {
                _requestInFlight = false;
            }
        }

        private ResetExecutionResult Fail(string message, string resolvedReason)
        {
            ResetIssue issue = ResetIssue.Error(ResetIssueKind.InvalidRequest, message);
            ResetSelectionMode mode = target.Kind == ResetTargetKind.CurrentActivity
                ? ResetSelectionMode.CurrentActivitySubjects
                : target.Kind == ResetTargetKind.CurrentRoute
                    ? ResetSelectionMode.CurrentRouteSubjects
                    : ResetSelectionMode.ExplicitSubjects;
            _lastResolution = ResetSelectionResolution.FailedResult(mode,
                ResetSelectionResolutionStatus.RejectedInvalidRequest, issue,
                nameof(ResetRequestTrigger), resolvedReason, message);
            _lastResult = ResetExecutionResult.RejectedInvalidRequest(issue, nameof(ResetRequestTrigger), resolvedReason);
            return _lastResult;
        }
    }
}
