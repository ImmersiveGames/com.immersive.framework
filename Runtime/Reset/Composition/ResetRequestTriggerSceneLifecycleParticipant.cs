using System;
using System.Collections.Generic;
using Immersive.Framework.Reset;
using Immersive.Framework.Reset.Unity;
using Immersive.Framework.SceneLifecycle;
using UnityEngine;

namespace Immersive.Framework.Reset.Composition
{
    internal sealed class ResetRequestTriggerSceneLifecycleParticipant : ISceneLifecycleParticipant
    {
        private readonly IResetTargetExecutionRuntimePort _runtime;

        internal ResetRequestTriggerSceneLifecycleParticipant(IResetTargetExecutionRuntimePort runtime) =>
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

        public SceneCompositionResult OnSceneAvailable(
            SceneCompositionScope scope,
            IReadOnlyList<GameObject> roots)
        {
            bool succeeded = ResetRequestTriggerBinder.TryBind(
                roots, _runtime, out int count, out string diagnostic);
            diagnostic = $"{diagnostic} scope='{scope.Label}'.";
            return succeeded
                ? SceneCompositionResult.Completed(scope, SceneCompositionOperation.Available, diagnostic)
                : SceneCompositionResult.Rejected(scope, SceneCompositionOperation.Available, diagnostic);
        }

        public SceneCompositionResult OnSceneReleasing(
            SceneCompositionScope scope,
            IReadOnlyList<GameObject> roots,
            string reason)
        {
            bool succeeded = ResetRequestTriggerBinder.TryRelease(
                roots, _runtime, out int count, out string diagnostic);
            diagnostic = $"{diagnostic} scope='{scope.Label}' reason='{reason}'.";
            return succeeded
                ? SceneCompositionResult.Completed(scope, SceneCompositionOperation.Releasing, diagnostic)
                : SceneCompositionResult.Rejected(scope, SceneCompositionOperation.Releasing, diagnostic);
        }
    }
}
