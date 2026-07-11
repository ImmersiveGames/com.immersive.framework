using System;
using System.Collections.Generic;
using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Scoped runtime authority for exactly one camera output.
    /// It admits typed requests, selects one deterministic winner and restores
    /// the next valid request when the current winner is released.
    /// It does not publish requests and does not apply Cinemachine state.
    /// </summary>
    public sealed class CameraOutputContext
    {
        private readonly CameraOutputId outputId;
        private readonly Dictionary<CameraRequestId, CameraRequest> admittedRequests =
            new Dictionary<CameraRequestId, CameraRequest>();

        private bool hasWinner;
        private CameraRequest winner;

        public CameraOutputContext(CameraOutputId outputId)
        {
            if (!outputId.IsValid)
            {
                throw new ArgumentException(
                    "CameraOutputContext requires a valid output id.",
                    nameof(outputId));
            }

            this.outputId = outputId;
        }

        public CameraOutputId OutputId => outputId;

        public int AdmittedRequestCount => admittedRequests.Count;

        public bool HasWinner => hasWinner;

        public CameraRequest Winner => winner;

        public CameraOutputContextResult Admit(CameraRequest request)
        {
            if (!request.IsValid)
            {
                return Blocked(
                    request,
                    "camera.output-context.request.invalid",
                    "Camera output context rejected an invalid request.");
            }

            if (request.OutputId != outputId)
            {
                return Blocked(
                    request,
                    "camera.output-context.output-mismatch",
                    $"Camera request output '{request.OutputId}' does not match context output '{outputId}'.");
            }

            if (admittedRequests.ContainsKey(request.RequestId))
            {
                return Blocked(
                    request,
                    "camera.output-context.request-duplicate",
                    $"Camera request '{request.RequestId}' is already admitted.");
            }

            bool previousHasWinner = hasWinner;
            CameraRequest previousWinner = winner;

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

            admittedRequests.Add(request.RequestId, request);
            SelectWinner();

            CameraOutputContextChangeKind changeKind = ResolveChangeKind(
                previousHasWinner,
                previousWinner,
                hasWinner,
                winner);

            return new CameraOutputContextResult(
                CameraOutputContextOperationKind.Admitted,
                changeKind,
                request,
                previousHasWinner,
                previousWinner,
                hasWinner,
                winner,
                Array.Empty<CameraIssue>(),
                $"Camera request admitted. request='{request.RequestId}' output='{outputId}' change='{changeKind}'.");
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

            if (!admittedRequests.TryGetValue(requestId, out CameraRequest releasedRequest))
            {
                return new CameraOutputContextResult(
                    CameraOutputContextOperationKind.NotFound,
                    CameraOutputContextChangeKind.None,
                    default,
                    hasWinner,
                    winner,
                    hasWinner,
                    winner,
                    new[]
                    {
                        CameraIssue.Warning(
                            "camera.output-context.release-not-found",
                            $"Camera request '{requestId}' is not admitted on output '{outputId}'.")
                    },
                    $"Camera request release skipped because request '{requestId}' was not found.");
            }

            bool previousHasWinner = hasWinner;
            CameraRequest previousWinner = winner;

            admittedRequests.Remove(requestId);
            SelectWinner();

            CameraOutputContextChangeKind changeKind = ResolveChangeKind(
                previousHasWinner,
                previousWinner,
                hasWinner,
                winner);

            return new CameraOutputContextResult(
                CameraOutputContextOperationKind.Released,
                changeKind,
                releasedRequest,
                previousHasWinner,
                previousWinner,
                hasWinner,
                winner,
                Array.Empty<CameraIssue>(),
                $"Camera request released. request='{requestId}' output='{outputId}' change='{changeKind}'.");
        }

        public bool Contains(CameraRequestId requestId)
        {
            return requestId.IsValid && admittedRequests.ContainsKey(requestId);
        }

        public CameraOutputContextSnapshot CaptureSnapshot()
        {
            var ids = new CameraRequestId[admittedRequests.Count];
            int index = 0;

            foreach (CameraRequestId requestId in admittedRequests.Keys)
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
                outputId,
                admittedRequests.Count,
                hasWinner,
                winner,
                ids);
        }

        private bool CanAdmitWithoutAmbiguity(
            CameraRequest candidate,
            out CameraIssue issue)
        {
            foreach (CameraRequest admitted in admittedRequests.Values)
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
            hasWinner = false;
            winner = default;

            foreach (CameraRequest request in admittedRequests.Values)
            {
                if (!hasWinner || Compare(request, winner) < 0)
                {
                    winner = request;
                    hasWinner = true;
                }
            }
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
                hasWinner,
                winner,
                hasWinner,
                winner,
                new[]
                {
                    CameraIssue.Blocking(code, normalizedMessage)
                },
                normalizedMessage);
        }
    }
}
