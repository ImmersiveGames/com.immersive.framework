using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ActivityRestart;
using Immersive.Framework.Authoring;
using Immersive.Framework.CycleReset;
using Immersive.Framework.GameFlow;
using Immersive.Framework.Reset;
using Immersive.Framework.SceneLifecycle;
using NUnit.Framework;
using UnityEngine;

namespace Immersive.Framework.Camera.Editor.Tests
{
    public sealed class FlowTriggerCompositionTests
    {
        private readonly List<GameObject> _objects = new();

        [TearDown]
        public void TearDown()
        {
            for (int index = _objects.Count - 1; index >= 0; index--)
                if (_objects[index] != null) Object.DestroyImmediate(_objects[index]);
            _objects.Clear();
        }

        [Test]
        public void RouteActivityCycleResetAndRestartTriggers_ShareScopeBindAndRelease()
        {
            GameObject owner = Create("session-owner");
            GameObject root = Create("flow-trigger-root");
            RouteRequestTrigger route = root.AddComponent<RouteRequestTrigger>();
            ActivityRequestTrigger activity = root.AddComponent<ActivityRequestTrigger>();
            RouteCycleResetTrigger routeReset = root.AddComponent<RouteCycleResetTrigger>();
            ActivityCycleResetTrigger activityReset = root.AddComponent<ActivityCycleResetTrigger>();
            ActivityRestartTrigger restart = root.AddComponent<ActivityRestartTrigger>();
            var authority = new FakeFlowAuthority();
            var lifecycle = new SceneLifecycleRuntime(
                new RouteRequestSceneLifecycleParticipant(authority),
                new ActivityRequestSceneLifecycleParticipant(authority),
                new RouteCycleResetSceneLifecycleParticipant(authority),
                new ActivityCycleResetSceneLifecycleParticipant(authority),
                new ActivityRestartSceneLifecycleParticipant(authority));

            Assert.That(lifecycle.ComposeSessionScope(owner, new[] { root }).Succeeded, Is.True);
            Assert.That(lifecycle.ComposeSessionScope(owner, new[] { root }).Succeeded, Is.True);
            Assert.That(route.HasRouteRuntimeBinding, Is.True);
            Assert.That(activity.HasActivityRuntimeBinding, Is.True);
            Assert.That(routeReset.HasRouteCycleResetRuntimeBinding, Is.True);
            Assert.That(activityReset.HasActivityCycleResetRuntimeBinding, Is.True);
            Assert.That(restart.HasActivityRestartRuntimeBinding, Is.True);

            Assert.That(lifecycle.ReleaseSessionScope(owner, new[] { root }, "session-shutdown").Succeeded, Is.True);
            Assert.That(route.HasRouteRuntimeBinding, Is.False);
            Assert.That(activity.HasActivityRuntimeBinding, Is.False);
            Assert.That(routeReset.HasRouteCycleResetRuntimeBinding, Is.False);
            Assert.That(activityReset.HasActivityCycleResetRuntimeBinding, Is.False);
            Assert.That(restart.HasActivityRestartRuntimeBinding, Is.False);
        }

        private GameObject Create(string name)
        {
            var value = new GameObject(name);
            _objects.Add(value);
            return value;
        }

        private sealed class FakeFlowAuthority : IRouteRuntimePort, IActivityRuntimePort,
            IRouteCycleResetRuntimePort, IActivityCycleResetRuntimePort, IActivityRestartRuntimePort
        {
            public Task<FrameworkRouteRequestResult> RequestRouteAsync(RouteAsset targetRoute, string source, string reason) =>
                Task.FromResult(default(FrameworkRouteRequestResult));

            public Task<FrameworkActivityRequestResult> RequestActivityAsync(ActivityAsset targetActivity, string source, string reason) =>
                Task.FromResult(default(FrameworkActivityRequestResult));

            public Task<FrameworkActivityRequestResult> ClearActivityAsync(string source, string reason) =>
                Task.FromResult(default(FrameworkActivityRequestResult));

            public Task<CycleResetResult> RequestRouteCycleResetAsync(string source, string reason) =>
                Task.FromResult(default(CycleResetResult));

            public Task<CycleResetResult> RequestActivityCycleResetAsync(string source, string reason) =>
                Task.FromResult(default(CycleResetResult));

            public Task<ActivityRestartRuntimeResult> RequestActivityRestartAsync(
                ActivityAsset targetActivity, bool useCurrentActivityWhenTargetMissing,
                bool requireTargetActivityIsCurrent, ResetTarget resetTarget, string source, string reason) =>
                Task.FromResult(default(ActivityRestartRuntimeResult));
        }
    }
}
