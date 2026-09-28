using System;
using System.Collections.Generic;
using Immersive.Framework.Transition;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Diagnostics;
using Immersive.Logging.Records;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Covers each Output with its Fallback Camera while the visual transition curtain is closed.
    /// The active normal Assignment remains untouched.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    internal sealed class SessionCameraTransitionOrchestrator : ITransitionOrchestrator
    {
        private static readonly CameraOutputFallbackCoverageOwnerId FallbackCoverageOwnerId =
            new CameraOutputFallbackCoverageOwnerId("SessionCameraTransitionOrchestrator");

        private readonly ITransitionOrchestrator _inner;
        private readonly CameraOutputSessionTopology _topology;
        private readonly FrameworkLogger _logger =
            FrameworkLogger.Create<SessionCameraTransitionOrchestrator>();

        internal SessionCameraTransitionOrchestrator(
            ITransitionOrchestrator inner,
            CameraOutputSessionTopology topology)
        {
            this._inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _topology = topology ?? throw new ArgumentNullException(nameof(topology));
        }

        public TransitionResult Execute(TransitionRequest request) => ExecuteAsync(request).GetAwaiter().GetResult();

        public async Awaitable<TransitionResult> ExecuteAsync(TransitionRequest request)
        {
            if (request.Phase == TransitionPhase.OperationClosed)
            {
                if (!TryApplyAllOutputs(false, out string diagnostic))
                {
                    return Blocked(request, "Fallback camera release blocked transition opening.", diagnostic);
                }

                return await _inner.ExecuteAsync(request);
            }

            TransitionResult result = await _inner.ExecuteAsync(request);
            if (!result.Completed || request.Phase != TransitionPhase.OperationOpened)
            {
                return result;
            }

            return TryApplyAllOutputs(true, out string outputDiagnostic)
                ? result
                : Blocked(request, "Fallback camera coverage blocked transition after the visual surface closed.", outputDiagnostic);
        }

        private bool TryApplyAllOutputs(
            bool coverWithFallback,
            out string diagnostic)
        {
            CameraOutputTopologySnapshot snapshot = _topology.CaptureSnapshot();
            var applied = new List<CameraOutputSession>();
            for (int index = 0; index < snapshot.Outputs.Count; index++)
            {
                CameraOutputId outputId = snapshot.Outputs[index].OutputId;
                if (!_topology.TryGetOutput(outputId, out CameraOutputAuthoring output, out diagnostic) ||
                    !output.TryGetSession(out CameraOutputSession session, out diagnostic))
                {
                    Rollback(applied, coverWithFallback);
                    return false;
                }
                CameraOutputApplyResult mutation = coverWithFallback
                    ? session.CoverWithFallback(FallbackCoverageOwnerId)
                    : session.ReleaseFallbackCoverage(FallbackCoverageOwnerId);
                if (!mutation.Succeeded)
                {
                    Rollback(applied, coverWithFallback);
                    diagnostic = $"Output '{outputId}' rejected transition Fallback mutation. {mutation.DiagnosticSummary}";
                    return false;
                }
                applied.Add(session);
            }

            for (int index = 0;
                 index < applied.Count;
                 index++)
            {
                CameraOutputSession session = applied[index];
                CameraOutputState state = session.OutputState;
                _logger.Debug(
                    coverWithFallback
                        ? "Camera transition Fallback coverage applied."
                        : "Camera transition Fallback coverage released.",
                    LogFields.Field(
                        "output",
                        session.OutputId.Value),
                    LogFields.Field(
                        "owner",
                        FallbackCoverageOwnerId.Value),
                    LogFields.Field(
                        "fallbackCoverageActive",
                        session.IsFallbackCoverageActive),
                    LogFields.Field(
                        "fallbackCoverageOwnerCount",
                        session.FallbackCoverageOwnerCount),
                    LogFields.Field(
                        "activeAssignment",
                        state.HasActiveAssignment
                            ? state.ActiveAssignmentId.Value
                            : "<none>"),
                    LogFields.Field(
                        "normalOccurrence",
                        state.HasRetainedNormalOccurrence
                            ? state.RetainedNormalOccurrence.ToString()
                            : "<none>"));
            }

            diagnostic = string.Empty;
            return true;
        }

        private static void Rollback(
            IReadOnlyList<CameraOutputSession> applied,
            bool covered)
        {
            for (int index = applied.Count - 1; index >= 0; index--)
            {
                if (covered) applied[index].ReleaseFallbackCoverage(FallbackCoverageOwnerId);
                else applied[index].CoverWithFallback(FallbackCoverageOwnerId);
            }
        }

        private static TransitionResult Blocked(TransitionRequest request, string message, string diagnostic)
        {
            return TransitionResult.FailedResult(request.OperationId, request.Kind, request.Source, request.Reason,
                message, Array.Empty<TransitionStep>(), new List<string> { diagnostic });
        }
    }
}
