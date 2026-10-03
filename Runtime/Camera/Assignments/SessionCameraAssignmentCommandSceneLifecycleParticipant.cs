using System;
using System.Collections.Generic;
using Immersive.Framework.SceneLifecycle;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    /// <summary>Composes optional Session Camera command adapters from an explicit root scope.</summary>
    internal sealed class SessionCameraAssignmentCommandSceneLifecycleParticipant :
        ISceneLifecycleParticipant
    {
        private readonly ISessionCameraAssignmentCommandPort _runtime;

        internal SessionCameraAssignmentCommandSceneLifecycleParticipant(
            ISessionCameraAssignmentCommandPort runtime)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

        public SceneCompositionResult OnSceneAvailable(
            SceneCompositionScope scope,
            IReadOnlyList<GameObject> roots)
        {
            SessionCameraAssignmentCommandTriggerBindingResult result =
                SessionCameraAssignmentCommandConsumerBinder.TryBind(roots, _runtime);
            return result.Succeeded
                ? SceneCompositionResult.Completed(scope, SceneCompositionOperation.Available, result.Message)
                : SceneCompositionResult.Rejected(scope, SceneCompositionOperation.Available, result.Message);
        }

        public SceneCompositionResult OnSceneReleasing(
            SceneCompositionScope scope,
            IReadOnlyList<GameObject> roots,
            string reason)
        {
            SessionCameraAssignmentCommandTriggerBindingResult result =
                SessionCameraAssignmentCommandConsumerBinder.TryRelease(roots, _runtime);
            string diagnostic = $"{result.Message} scope='{scope.Label}' reason='{reason}'.";
            return result.Succeeded
                ? SceneCompositionResult.Completed(scope, SceneCompositionOperation.Releasing, diagnostic)
                : SceneCompositionResult.Rejected(scope, SceneCompositionOperation.Releasing, diagnostic);
        }
    }
}
