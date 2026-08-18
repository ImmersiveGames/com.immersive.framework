using System;
using System.Collections.Generic;
using Immersive.Framework.Transition;
using Immersive.Framework.ApiStatus;
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
        private const string ForceDefaultOwner = "SessionCameraTransitionOrchestrator";

        private readonly ITransitionOrchestrator inner;
        private readonly CameraOutputSessionBinding outputSessionBinding;

        internal SessionCameraTransitionOrchestrator(
            ITransitionOrchestrator inner,
            CameraOutputSessionBinding outputSessionBinding)
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
            this.outputSessionBinding = outputSessionBinding ?? throw new ArgumentNullException(nameof(outputSessionBinding));
        }

        public TransitionResult Execute(TransitionRequest request) => ExecuteAsync(request).GetAwaiter().GetResult();

        public async Awaitable<TransitionResult> ExecuteAsync(TransitionRequest request)
        {
            if (request.Phase == TransitionPhase.OperationClosed)
            {
                if (!TryResolveOutputSession(out CameraOutputSession session, out string diagnostic))
                {
                    return Blocked(request, "Default camera release could not resolve the Camera Output Session.", diagnostic);
                }

                CameraOutputApplyResult release = session.ReleaseForceDefault(ForceDefaultOwner);
                if (!release.Succeeded)
                {
                    return Blocked(request, "Default camera release blocked transition opening.", release.DiagnosticSummary);
                }

                return await inner.ExecuteAsync(request);
            }

            TransitionResult result = await inner.ExecuteAsync(request);
            if (!result.Completed || request.Phase != TransitionPhase.OperationOpened)
            {
                return result;
            }

            if (!TryResolveOutputSession(out CameraOutputSession outputSession, out string outputDiagnostic))
            {
                return Blocked(request, "Default camera forcing could not resolve the Camera Output Session.", outputDiagnostic);
            }

            CameraOutputApplyResult force = outputSession.ForceDefault(ForceDefaultOwner);
            return force.Succeeded
                ? result
                : Blocked(request, "Default camera forcing blocked transition after the visual surface closed.", force.DiagnosticSummary);
        }

        private bool TryResolveOutputSession(
            out CameraOutputSession session,
            out string diagnostic)
        {
            if (outputSessionBinding == null)
            {
                session = null;
                diagnostic = "Session Camera Transition Orchestrator has no explicit Camera Output Session Binding.";
                return false;
            }

            return outputSessionBinding.TryGetSession(
                out session,
                out diagnostic);
        }

        private static TransitionResult Blocked(TransitionRequest request, string message, string diagnostic)
        {
            return TransitionResult.FailedResult(request.OperationId, request.Kind, request.Source, request.Reason,
                message, Array.Empty<TransitionStep>(), new List<string> { diagnostic });
        }
    }
}
