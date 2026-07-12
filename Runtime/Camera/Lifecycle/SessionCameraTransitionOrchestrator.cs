using System;
using System.Collections.Generic;
using Immersive.Framework.Transition;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Wraps the visual transition boundary so the persistent Session camera is active only while the curtain is closed.
    /// </summary>
    internal sealed class SessionCameraTransitionOrchestrator : ITransitionOrchestrator
    {
        private readonly ITransitionOrchestrator inner;
        private readonly SessionCameraOverrideBinding sessionOverride;

        internal SessionCameraTransitionOrchestrator(ITransitionOrchestrator inner, SessionCameraOverrideBinding sessionOverride)
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
            this.sessionOverride = sessionOverride ?? throw new ArgumentNullException(nameof(sessionOverride));
        }

        public TransitionResult Execute(TransitionRequest request) => ExecuteAsync(request).GetAwaiter().GetResult();

        public async Awaitable<TransitionResult> ExecuteAsync(TransitionRequest request)
        {
            if (request.Phase == TransitionPhase.OperationClosed)
            {
                CameraOverrideResult release = sessionOverride.ReleaseOverride();
                if (!release.Succeeded) return Blocked(request, "Session camera release blocked transition opening.", release.Diagnostic);
                return await inner.ExecuteAsync(request);
            }

            TransitionResult result = await inner.ExecuteAsync(request);
            if (!result.Completed || request.Phase != TransitionPhase.OperationOpened) return result;

            CameraOverrideResult requestResult = sessionOverride.RequestOverride();
            return requestResult.Succeeded
                ? result
                : Blocked(request, "Session camera request blocked transition after the visual surface closed.", requestResult.Diagnostic);
        }

        private static TransitionResult Blocked(TransitionRequest request, string message, string diagnostic)
        {
            return TransitionResult.FailedResult(request.OperationId, request.Kind, request.Source, request.Reason,
                message, Array.Empty<TransitionStep>(), new List<string> { diagnostic });
        }
    }
}
