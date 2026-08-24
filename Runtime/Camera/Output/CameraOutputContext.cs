using System;
using System.Collections.Generic;
using Immersive.Framework.Common;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Scoped runtime authority for exactly one camera output.
    /// It admits typed requests, selects one deterministic winner and restores
    /// the next valid request when the current winner is released.
    /// It does not publish requests and does not apply Cinemachine state.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    public sealed class CameraOutputContext
    {
        private readonly CameraOutputId _outputId;
        private readonly Dictionary<CameraRequestId, CameraRequest> _admittedRequests =
            new Dictionary<CameraRequestId, CameraRequest>();

        private bool _hasWinner;
        private CameraRequest _winner;

        public CameraOutputContext(CameraOutputId outputId)
        {
            if (!outputId.IsValid)
            {
                throw new ArgumentException(
                    "CameraOutputContext requires a valid output id.",
                    nameof(outputId));
            }

            this._outputId = outputId;
        }

        public CameraOutputId OutputId => _outputId;

        public int AdmittedRequestCount => _admittedRequests.Count;

        public bool HasWinner => _hasWinner;

        public CameraRequest Winner => _winner;

        public CameraOutputContextResult Admit(CameraRequest request)
        {
            if (!request.IsValid)
            {
                return Blocked(
                    request,
                    "camera.output-context.request.invalid",
                    "Camera output context rejected an invalid request.");
            }

            if (request.OutputId != _outputId)
            {
                return Blocked(
                    request,
                    "camera.output-context.output-mismatch",
                    $"Camera request output '{request.OutputId}' does not match context output '{_outputId}'.");
            }

            if (_admittedRequests.ContainsKey(request.RequestId))
            {
                return Blocked(
                    request,
                    "camera.output-context.request-duplicate",
                    $"Camera request '{request.RequestId}' is already admitted.");
            }

            bool previousHasWinner = _hasWinner;
            CameraRequest previousWinner = _winner;

            if (!CanAdmitWithoutAmbiguity(request, out CameraIssue ambiguityIssue))
            {
                return new CameraOutputContextResult(
                    CameraOutputContextOperationKind.Blocked,
                    CameraOutputContextChangeKind.None,
                    request,
                    previousHasWinner,
                    previousWinner,
                    previousHasWinner,
                    previousWinner,
                    new[] { ambiguityIssue },
                    ambiguityIssue.Message);
            }

            _admittedRequests.Add(request.RequestId, request);
            SelectWinner();

            CameraOutputContextChangeKind changeKind = ResolveChangeKind(
                previousHasWinner,
                previousWinner,
                _hasWinner,
                _winner);

            return new CameraOutputContextResult(
                CameraOutputContextOperationKind.Admitted,
                changeKind,
                request,
                previousHasWinner,
                previousWinner,
                _hasWinner,
                _winner,
                Array.Empty<CameraIssue>(),
                $"Camera request admitted. request='{request.RequestId}' output='{_outputId}' change='{changeKind}'.");
        }

        public CameraOutputContextResult Release(CameraRequestId requestId)
        {
            if (!requestId.IsValid)
            {
                return Blocked(
                    default,
                    "camera.output-context.release-id.invalid",
                    "Camera output context release requires a valid request id.");
            }

            if (!_admittedRequests.TryGetValue(requestId, out CameraRequest releasedRequest))
            {
                return new CameraOutputContextResult(
                    CameraOutputContextOperationKind.NotFound,
                    CameraOutputContextChangeKind.None,
                    default,
                    _hasWinner,
                    _winner,
                    _hasWinner,
                    _winner,
                    new[]
                    {
                        CameraIssue.Warning(
                            "camera.output-context.release-not-found",
                            $"Camera request '{requestId}' is not admitted on output '{_outputId}'.")
                    },
                    $"Camera request release skipped because request '{requestId}' was not found.");
            }

            bool previousHasWinner = _hasWinner;
            CameraRequest previousWinner = _winner;

            _admittedRequests.Remove(requestId);
            CameraIssue[] releaseIssues = PruneInvalidRequests();
            SelectWinner();

            CameraOutputContextChangeKind changeKind = ResolveChangeKind(
                previousHasWinner,
                previousWinner,
                _hasWinner,
                _winner);

            return new CameraOutputContextResult(
                CameraOutputContextOperationKind.Released,
                changeKind,
                releasedRequest,
                previousHasWinner,
                previousWinner,
                _hasWinner,
                _winner,
                releaseIssues,
                releaseIssues.Length == 0
                    ? $"Camera request released. request='{requestId}' output='{_outputId}' change='{changeKind}'."
                    : $"Camera request released and stale invalid requests were pruned. request='{requestId}' output='{_outputId}' change='{changeKind}' pruned='{releaseIssues.Length}'.");
        }

        public bool Contains(CameraRequestId requestId)
        {
            return requestId.IsValid && _admittedRequests.ContainsKey(requestId);
        }

        public CameraOutputContextSnapshot CaptureSnapshot()
        {
            var ids = new CameraRequestId[_admittedRequests.Count];
            int index = 0;

            foreach (CameraRequestId requestId in _admittedRequests.Keys)
            {
                ids[index++] = requestId;
            }

            Array.Sort(
                ids,
                (left, right) =>
                    string.Compare(
                        left.Value,
                        right.Value,
                        StringComparison.Ordinal));

            return new CameraOutputContextSnapshot(
                _outputId,
                _admittedRequests.Count,
                _hasWinner,
                _winner,
                ids);
        }

        private bool CanAdmitWithoutAmbiguity(
            CameraRequest candidate,
            out CameraIssue issue)
        {
            foreach (CameraRequest admitted in _admittedRequests.Values)
            {
                if (admitted.Policy.Precedence != candidate.Policy.Precedence)
                {
                    continue;
                }

                if (!admitted.Policy.HasDeterministicTieBreaker ||
                    !candidate.Policy.HasDeterministicTieBreaker)
                {
                    issue = CameraIssue.Blocking(
                        "camera.output-context.tie-breaker.missing",
                        $"Requests '{admitted.RequestId}' and '{candidate.RequestId}' share precedence " +
                        $"'{candidate.Policy.Precedence}' but do not both declare deterministic tie-breakers.");
                    return false;
                }

                if (string.Equals(
                    admitted.Policy.DeterministicTieBreakerId,
                    candidate.Policy.DeterministicTieBreakerId,
                    StringComparison.Ordinal))
                {
                    issue = CameraIssue.Blocking(
                        "camera.output-context.tie-breaker.duplicate",
                        $"Requests '{admitted.RequestId}' and '{candidate.RequestId}' share precedence " +
                        $"'{candidate.Policy.Precedence}' and tie-breaker " +
                        $"'{candidate.Policy.DeterministicTieBreakerId}'.");
                    return false;
                }
            }

            issue = default;
            return true;
        }

        private void SelectWinner()
        {
            _hasWinner = false;
            _winner = default;

            foreach (CameraRequest request in _admittedRequests.Values)
            {
                if (!_hasWinner || Compare(request, _winner) < 0)
                {
                    _winner = request;
                    _hasWinner = true;
                }
            }
        }

        private CameraIssue[] PruneInvalidRequests()
        {
            List<CameraRequestId> invalidRequestIds = null;

            foreach (KeyValuePair<CameraRequestId, CameraRequest> entry in _admittedRequests)
            {
                if (entry.Value.IsValid)
                {
                    continue;
                }

                invalidRequestIds ??= new List<CameraRequestId>();
                invalidRequestIds.Add(entry.Key);
            }

            if (invalidRequestIds == null)
            {
                return Array.Empty<CameraIssue>();
            }

            invalidRequestIds.Sort(
                (left, right) => string.Compare(
                    left.Value,
                    right.Value,
                    StringComparison.Ordinal));

            var issues = new CameraIssue[invalidRequestIds.Count];

            for (int index = 0; index < invalidRequestIds.Count; index++)
            {
                CameraRequestId invalidRequestId = invalidRequestIds[index];
                _admittedRequests.Remove(invalidRequestId);
                issues[index] = CameraIssue.Warning(
                    "camera.output-context.stale-request-pruned",
                    $"Camera output context pruned stale invalid request '{invalidRequestId}' while processing release on output '{_outputId}'.");
            }

            return issues;
        }

        private static int Compare(CameraRequest left, CameraRequest right)
        {
            int precedenceComparison =
                right.Policy.Precedence.CompareTo(left.Policy.Precedence);

            if (precedenceComparison != 0)
            {
                return precedenceComparison;
            }

            return string.Compare(
                left.Policy.DeterministicTieBreakerId,
                right.Policy.DeterministicTieBreakerId,
                StringComparison.Ordinal);
        }

        private static CameraOutputContextChangeKind ResolveChangeKind(
            bool previousHasWinner,
            CameraRequest previousWinner,
            bool currentHasWinner,
            CameraRequest currentWinner)
        {
            if (!previousHasWinner && currentHasWinner)
            {
                return CameraOutputContextChangeKind.WinnerEstablished;
            }

            if (previousHasWinner && !currentHasWinner)
            {
                return CameraOutputContextChangeKind.WinnerCleared;
            }

            if (!previousHasWinner)
            {
                return CameraOutputContextChangeKind.None;
            }

            return previousWinner.RequestId == currentWinner.RequestId
                ? CameraOutputContextChangeKind.WinnerPreserved
                : CameraOutputContextChangeKind.WinnerChanged;
        }

        private CameraOutputContextResult Blocked(
            CameraRequest request,
            string code,
            string message)
        {
            string normalizedMessage =
                message.NormalizeTextOrFallback(
                    "Camera output context operation was blocked.");

            return new CameraOutputContextResult(
                CameraOutputContextOperationKind.Blocked,
                CameraOutputContextChangeKind.None,
                request,
                _hasWinner,
                _winner,
                _hasWinner,
                _winner,
                new[]
                {
                    CameraIssue.Blocking(code, normalizedMessage)
                },
                normalizedMessage);
        }
    }
}
