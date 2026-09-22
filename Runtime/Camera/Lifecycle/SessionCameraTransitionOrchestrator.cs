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
    /// Wraps the visual transition boundary so the output presents its explicit Default Camera Rig while the curtain is closed.
    /// Normal camera-request arbitration remains untouched.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    internal sealed class SessionCameraTransitionOrchestrator : ITransitionOrchestrator
    {
        private static readonly CameraOutputForceDefaultOwnerId ForceDefaultOwnerId =
            new CameraOutputForceDefaultOwnerId("SessionCameraTransitionOrchestrator");

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
                    return Blocked(request, "Default camera release blocked transition opening.", diagnostic);
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
                : Blocked(request, "Default camera forcing blocked transition after the visual surface closed.", outputDiagnostic);
        }

        private bool TryApplyAllOutputs(
            bool forceDefault,
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
                    Rollback(applied, forceDefault);
                    return false;
                }
                CameraOutputApplyResult mutation = forceDefault
                    ? session.ForceDefault(ForceDefaultOwnerId)
                    : session.ReleaseForceDefault(ForceDefaultOwnerId);
                if (!mutation.Succeeded)
                {
                    Rollback(applied, forceDefault);
                    diagnostic = $"Output '{outputId}' rejected transition default mutation. {mutation.DiagnosticSummary}";
                    return false;
                }
                applied.Add(session);

                CameraOutputContextSnapshot context =
                    session.Context.CaptureSnapshot();
                _logger.Debug(
                    forceDefault
                        ? "Camera transition force-default applied."
                        : "Camera transition force-default released.",
                    LogFields.Field(
                        "output",
                        outputId.Value),
                    LogFields.Field(
                        "owner",
                        ForceDefaultOwnerId.Value),
                    LogFields.Field(
                        "forceDefaultActive",
                        session.IsDefaultForced),
                    LogFields.Field(
                        "forceDefaultOwnerCount",
                        session.ForceDefaultOwnerCount),
                    LogFields.Field(
                        "normalRequestCount",
                        context.AdmittedRequestCount),
                    LogFields.Field(
                        "normalWinner",
                        context.HasWinner
                            ? context.Winner.RequestId.Value
                            : "<none>"),
                    LogFields.Field(
                        "applyKind",
                        mutation.Kind),
                    LogFields.Field(
                        "diagnostic",
                        mutation.DiagnosticSummary));
            }
            diagnostic = string.Empty;
            return true;
        }

        private static void Rollback(
            IReadOnlyList<CameraOutputSession> applied,
            bool forced)
        {
            for (int index = applied.Count - 1; index >= 0; index--)
            {
                if (forced) applied[index].ReleaseForceDefault(ForceDefaultOwnerId);
                else applied[index].ForceDefault(ForceDefaultOwnerId);
            }
        }

        private static TransitionResult Blocked(TransitionRequest request, string message, string diagnostic)
        {
            return TransitionResult.FailedResult(request.OperationId, request.Kind, request.Source, request.Reason,
                message, Array.Empty<TransitionStep>(), new List<string> { diagnostic });
        }
    }
}
