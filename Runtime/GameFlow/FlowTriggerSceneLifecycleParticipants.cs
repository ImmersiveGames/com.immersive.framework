using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityRestart;
using Immersive.Framework.CycleReset;
using Immersive.Framework.SceneLifecycle;
using Immersive.Framework.GameFlow;
using UnityEngine;

namespace Immersive.Framework.GameFlow
{
    internal sealed class RouteRequestSceneLifecycleParticipant : ISceneLifecycleParticipant
    {
        private readonly IRouteRuntimePort _runtime;
        internal RouteRequestSceneLifecycleParticipant(IRouteRuntimePort runtime) =>
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

        public SceneCompositionResult OnSceneAvailable(SceneCompositionScope scope, IReadOnlyList<GameObject> roots)
        {
            RouteRequestTriggerBinderResult result = RouteRequestTriggerBinder.TryBind(roots, _runtime);
            return result.Succeeded
                ? SceneCompositionResult.Completed(scope, SceneCompositionOperation.Available, result.Message)
                : SceneCompositionResult.Rejected(scope, SceneCompositionOperation.Available, result.Message);
        }

        public SceneCompositionResult OnSceneReleasing(SceneCompositionScope scope, IReadOnlyList<GameObject> roots, string reason)
        {
            bool succeeded = RouteRequestTriggerBinder.TryRelease(roots, _runtime, out int count, out string diagnostic);
            diagnostic = $"{diagnostic} scope='{scope.Label}' triggers='{count}' reason='{reason}'.";
            return succeeded
                ? SceneCompositionResult.Completed(scope, SceneCompositionOperation.Releasing, diagnostic)
                : SceneCompositionResult.Rejected(scope, SceneCompositionOperation.Releasing, diagnostic);
        }
    }

    internal sealed class ActivityRequestSceneLifecycleParticipant : ISceneLifecycleParticipant
    {
        private readonly IActivityRuntimePort _runtime;
        internal ActivityRequestSceneLifecycleParticipant(IActivityRuntimePort runtime) =>
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

        public SceneCompositionResult OnSceneAvailable(SceneCompositionScope scope, IReadOnlyList<GameObject> roots)
        {
            ActivityRequestTriggerBinderResult result = ActivityRequestTriggerBinder.TryBind(roots, _runtime);
            return result.Succeeded
                ? SceneCompositionResult.Completed(scope, SceneCompositionOperation.Available, result.Message)
                : SceneCompositionResult.Rejected(scope, SceneCompositionOperation.Available, result.Message);
        }

        public SceneCompositionResult OnSceneReleasing(SceneCompositionScope scope, IReadOnlyList<GameObject> roots, string reason)
        {
            bool succeeded = ActivityRequestTriggerBinder.TryRelease(roots, _runtime, out int count, out string diagnostic);
            diagnostic = $"{diagnostic} scope='{scope.Label}' triggers='{count}' reason='{reason}'.";
            return succeeded
                ? SceneCompositionResult.Completed(scope, SceneCompositionOperation.Releasing, diagnostic)
                : SceneCompositionResult.Rejected(scope, SceneCompositionOperation.Releasing, diagnostic);
        }
    }

    internal sealed class RouteCycleResetSceneLifecycleParticipant : ISceneLifecycleParticipant
    {
        private readonly IRouteCycleResetRuntimePort _runtime;
        internal RouteCycleResetSceneLifecycleParticipant(IRouteCycleResetRuntimePort runtime) =>
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

        public SceneCompositionResult OnSceneAvailable(SceneCompositionScope scope, IReadOnlyList<GameObject> roots)
        {
            RouteCycleResetTriggerBindingResult result = RouteCycleResetTriggerBinding.TryBind(roots, _runtime);
            return result.Succeeded
                ? SceneCompositionResult.Completed(scope, SceneCompositionOperation.Available, result.Message)
                : SceneCompositionResult.Rejected(scope, SceneCompositionOperation.Available, result.Message);
        }

        public SceneCompositionResult OnSceneReleasing(SceneCompositionScope scope, IReadOnlyList<GameObject> roots, string reason)
        {
            bool succeeded = RouteCycleResetTriggerBinding.TryRelease(roots, _runtime, out int count, out string diagnostic);
            diagnostic = $"{diagnostic} scope='{scope.Label}' triggers='{count}' reason='{reason}'.";
            return succeeded
                ? SceneCompositionResult.Completed(scope, SceneCompositionOperation.Releasing, diagnostic)
                : SceneCompositionResult.Rejected(scope, SceneCompositionOperation.Releasing, diagnostic);
        }
    }

    internal sealed class ActivityCycleResetSceneLifecycleParticipant : ISceneLifecycleParticipant
    {
        private readonly IActivityCycleResetRuntimePort _runtime;
        internal ActivityCycleResetSceneLifecycleParticipant(IActivityCycleResetRuntimePort runtime) =>
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

        public SceneCompositionResult OnSceneAvailable(SceneCompositionScope scope, IReadOnlyList<GameObject> roots)
        {
            ActivityCycleResetTriggerBinderResult result = ActivityCycleResetTriggerBinder.TryBind(roots, _runtime);
            return result.Succeeded
                ? SceneCompositionResult.Completed(scope, SceneCompositionOperation.Available, result.Message)
                : SceneCompositionResult.Rejected(scope, SceneCompositionOperation.Available, result.Message);
        }

        public SceneCompositionResult OnSceneReleasing(SceneCompositionScope scope, IReadOnlyList<GameObject> roots, string reason)
        {
            bool succeeded = ActivityCycleResetTriggerBinder.TryRelease(roots, _runtime, out int count, out string diagnostic);
            diagnostic = $"{diagnostic} scope='{scope.Label}' triggers='{count}' reason='{reason}'.";
            return succeeded
                ? SceneCompositionResult.Completed(scope, SceneCompositionOperation.Releasing, diagnostic)
                : SceneCompositionResult.Rejected(scope, SceneCompositionOperation.Releasing, diagnostic);
        }
    }

    internal sealed class ActivityRestartSceneLifecycleParticipant : ISceneLifecycleParticipant
    {
        private readonly IActivityRestartRuntimePort _runtime;
        internal ActivityRestartSceneLifecycleParticipant(IActivityRestartRuntimePort runtime) =>
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

        public SceneCompositionResult OnSceneAvailable(SceneCompositionScope scope, IReadOnlyList<GameObject> roots)
        {
            ActivityRestartTriggerBinderResult result = ActivityRestartTriggerBinder.TryBind(roots, _runtime);
            return result.Succeeded
                ? SceneCompositionResult.Completed(scope, SceneCompositionOperation.Available, result.Message)
                : SceneCompositionResult.Rejected(scope, SceneCompositionOperation.Available, result.Message);
        }

        public SceneCompositionResult OnSceneReleasing(SceneCompositionScope scope, IReadOnlyList<GameObject> roots, string reason)
        {
            bool succeeded = ActivityRestartTriggerBinder.TryRelease(roots, _runtime, out int count, out string diagnostic);
            diagnostic = $"{diagnostic} scope='{scope.Label}' triggers='{count}' reason='{reason}'.";
            return succeeded
                ? SceneCompositionResult.Completed(scope, SceneCompositionOperation.Releasing, diagnostic)
                : SceneCompositionResult.Rejected(scope, SceneCompositionOperation.Releasing, diagnostic);
        }
    }
}
