using System;
using Immersive.Framework.ApiStatus;
using UnityEngine;
using Immersive.Framework.TransitionEffects;

namespace Immersive.Framework.Transition
{
    /// <summary>
    /// API status: Experimental. Immediate no-visual Transition orchestrator.
    /// It records explicit diagnostics and never waits, creates GameObjects, loads scenes or changes Route/Activity state.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F24B no-op Transition orchestrator; no visual, no waiting, no lifecycle ownership.")]
    public sealed class NoOpTransitionOrchestrator : ITransitionOrchestrator
    {
        public static readonly NoOpTransitionOrchestrator Instance = new NoOpTransitionOrchestrator();

        public TransitionResult Execute(TransitionRequest request)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("NoOp Transition requires a valid request.", nameof(request));
            }

            var step = TransitionStep.Succeeded(
                0,
                request.Phase,
                BuildStepLabel(request),
                "NoOp Transition completed immediately without visual, wait, scene mutation or lifecycle ownership.");

            return TransitionResult.SucceededResult(
                request.OperationId,
                request.Kind,
                request.Source,
                request.Reason,
                "SucceededNoVisual",
                new[] { step },
                TransitionEffectKind.Unknown,
                TransitionEffectStatus.Skipped,
                0,
                "NoneConfigured",
                0);
        }

#pragma warning disable CS1998 // NoOp async contract intentionally completes synchronously without yielding.
        public async Awaitable<TransitionResult> ExecuteAsync(TransitionRequest request)
        {
            return Execute(request);
        }
#pragma warning restore CS1998

        private static string BuildStepLabel(TransitionRequest request)
        {
            string phase = request.Phase == TransitionPhase.OperationOpened ? "before" : "after";
            return $"{request.Scope.ToString().ToLowerInvariant()}-{phase}";
        }
    }
}
