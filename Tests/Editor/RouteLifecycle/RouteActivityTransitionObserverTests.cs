using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.Identity;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.SceneLifecycle;
using NUnit.Framework;
using UnityEngine;

namespace Immersive.Framework.RouteLifecycle.Tests
{
    public sealed class RouteActivityTransitionObserverTests
    {
        private readonly List<Object> _objects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null) Object.DestroyImmediate(_objects[i]);
            }
            _objects.Clear();
        }

        [TestCase("A", "B", "activity-request", "replace")]
        [TestCase("A", null, "route-exit", "clear")]
        [TestCase(null, "A", "route-startup", "startup")]
        public void Dispatch_ReportsCommittedTransitionOnceToRouteScope(
            string previousName,
            string currentName,
            string source,
            string reason)
        {
            RouteAsset route = CreateRoute();
            ActivityAsset previous = previousName != null ? CreateActivity(previousName) : null;
            ActivityAsset current = currentName != null ? CreateActivity(currentName) : null;
            GameObject root = new("route-content-root");
            _objects.Add(root);
            RouteContentContribution contribution = root.AddComponent<RouteContentContribution>();
            SetPrivateField(contribution, "route", route);
            RouteObserver observer = root.AddComponent<RouteObserver>();
            List<RouteActivityTransitionContext> received = observer.Contexts;
            RouteContentDiscoveryScope scope = CreateScope(route, root);

            new RouteActivityTransitionObserverDispatcher().Dispatch(scope, previous, current, source, reason);

            Assert.That(received, Has.Count.EqualTo(1));
            Assert.That(observer.Contexts[0].PreviousActivity, Is.SameAs(previous));
            Assert.That(observer.Contexts[0].CurrentActivity, Is.SameAs(current));
            Assert.That(observer.Contexts[0].Source, Is.EqualTo(source));
            Assert.That(observer.Contexts[0].Reason, Is.EqualTo(reason));
        }

        [Test]
        public void Dispatch_AfterRouteContributionRelease_DoesNotNotifyReleasedObserver()
        {
            RouteAsset route = CreateRoute();
            GameObject root = new("route-content-root");
            _objects.Add(root);
            RouteContentContribution contribution = root.AddComponent<RouteContentContribution>();
            SetPrivateField(contribution, "route", route);
            RouteObserver observer = root.AddComponent<RouteObserver>();
            List<RouteActivityTransitionContext> received = observer.Contexts;
            RouteContentDiscoveryScope scope = CreateScope(route, root);
            var dispatcher = new RouteActivityTransitionObserverDispatcher();
            ActivityAsset activity = CreateActivity("A");

            dispatcher.Dispatch(scope, null, activity, "startup", "route-startup");
            Object.DestroyImmediate(root);
            dispatcher.Dispatch(scope, activity, null, "route-exit", "route-exit");

            Assert.That(received, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task ActivityReplacement_NotifiesOnceAfterActivityContentExitAndEnter()
        {
            RouteAsset route = CreateRoute();
            ActivityAsset activityA = CreateActivity("A", "activity-a");
            ActivityAsset activityB = CreateActivity("B", "activity-b");
            SetPrivateField(route, "startupActivity", activityA);
            GameObject root = CreateRouteRoot(route);
            var callbacks = new List<string>();
            RouteSequenceObserver observer = root.AddComponent<RouteSequenceObserver>();
            observer.Callbacks = callbacks;
            AddActivityContent(root, activityA, callbacks);
            AddActivityContent(root, activityB, callbacks);

            var flow = new ActivityFlowRuntime(new RuntimeContentRuntime(), new SceneLifecycleRuntime());
            flow.SetRouteContentDiscoveryScope(CreateScope(route, root));
            ActivityFlowStartResult startup = await flow.StartStartupActivityAsync(route, "test", "startup");
            Assert.That(startup.Completed, Is.True, startup.Message);
            Assert.That(callbacks, Is.EqualTo(new[] { "Enter:A", "Route:none->A" }));
            callbacks.Clear();

            ActivityFlowStartResult replacement = await flow.StartActivityAsync(activityB, route, "test", "replace");

            Assert.That(replacement.Completed, Is.True, replacement.Message);
            Assert.That(callbacks, Is.EqualTo(new[] { "Exit:A", "Enter:B", "Route:A->B" }));
        }

        [Test]
        public async Task ActivityClear_NotifiesNullAfterExitWhileRouteCompositionIsStillLoaded()
        {
            RouteAsset route = CreateRoute();
            ActivityAsset activityA = CreateActivity("A", "activity-a");
            SetPrivateField(route, "startupActivity", activityA);
            GameObject root = CreateRouteRoot(route);
            var callbacks = new List<string>();
            RouteSequenceObserver observer = root.AddComponent<RouteSequenceObserver>();
            observer.Callbacks = callbacks;
            AddActivityContent(root, activityA, callbacks);

            var flow = new ActivityFlowRuntime(new RuntimeContentRuntime(), new SceneLifecycleRuntime());
            flow.SetRouteContentDiscoveryScope(CreateScope(route, root));
            ActivityFlowStartResult startup = await flow.StartStartupActivityAsync(route, "test", "startup");
            Assert.That(startup.Completed, Is.True, startup.Message);
            Assert.That(callbacks, Is.EqualTo(new[] { "Enter:A", "Route:none->A" }));
            callbacks.Clear();

            ActivityFlowStartResult clear = await flow.ClearActivityAsync(route, "test", "route-exit");

            Assert.That(clear.Completed, Is.True, clear.Message);
            Assert.That(callbacks, Is.EqualTo(new[] { "Exit:A", "Route:A->none" }));
            Assert.That(root != null, Is.True, "The Route composition root is still loaded at Activity clear notification time.");
        }

        private RouteAsset CreateRoute()
        {
            RouteAsset route = ScriptableObject.CreateInstance<RouteAsset>();
            SetPrivateField(route, "routeId", "route-test-identity");
            _objects.Add(route);
            return route;
        }

        private ActivityAsset CreateActivity(string name) => CreateActivity(name, "activity-" + name.ToLowerInvariant());

        private ActivityAsset CreateActivity(string name, string id)
        {
            ActivityAsset activity = ScriptableObject.CreateInstance<ActivityAsset>();
            activity.name = name;
            SetPrivateField(activity, "activityId", id);
            _objects.Add(activity);
            return activity;
        }

        private GameObject CreateRouteRoot(RouteAsset route)
        {
            GameObject root = new("route-content-root");
            _objects.Add(root);
            RouteContentContribution contribution = root.AddComponent<RouteContentContribution>();
            SetPrivateField(contribution, "route", route);
            return root;
        }

        private static void AddActivityContent(GameObject routeRoot, ActivityAsset activity, List<string> callbacks)
        {
            var child = new GameObject("activity-content-" + activity.name);
            child.transform.SetParent(routeRoot.transform);
            ActivityContentContribution contribution = child.AddComponent<ActivityContentContribution>();
            SetPrivateField(contribution, "activity", activity);
            SetPrivateField(contribution, "localContentId", "activity-local-content.test-" + activity.name.ToLowerInvariant());
            ActivitySequenceRecorder recorder = child.AddComponent<ActivitySequenceRecorder>();
            recorder.Callbacks = callbacks;
        }

        private static RouteContentDiscoveryScope CreateScope(RouteAsset route, GameObject root)
        {
            string sceneName = root.scene.name;
            string scenePath = root.scene.path;
            FrameworkIdentityKey owner = FrameworkIdentityKey.From(FrameworkIdentityDomain.Route, "route-test");
            FrameworkContentIdentity identity = new(owner, FrameworkContentScope.Route, FrameworkContentKind.Scene, new FrameworkContentId("primary"));
            var planEntry = new RouteSceneCompositionPlanEntry(
                identity,
                "primary",
                sceneName,
                scenePath,
                RouteSceneRole.Primary,
                FrameworkContentRequiredness.Required,
                RouteContentOwnership.Owned,
                RouteSceneLoadMode.Single,
                0,
                true);
            RouteSceneCompositionResultEntry resultEntry = RouteSceneCompositionResultEntry.LoadedEntry(planEntry, true, true, "test");
            var scenes = new[] { new RouteContentDiscoveryScene(resultEntry) };
            return new RouteContentDiscoveryScope(route, scenes, new[] { root });
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        public sealed class RouteObserver : MonoBehaviour, IRouteActivityTransitionObserver
        {
            public List<RouteActivityTransitionContext> Contexts { get; } = new();

            public void OnActivityTransitionCommitted(RouteActivityTransitionContext context) => Contexts.Add(context);
        }

        public sealed class RouteSequenceObserver : MonoBehaviour, IRouteActivityTransitionObserver
        {
            public List<string> Callbacks { get; set; }

            public void OnActivityTransitionCommitted(RouteActivityTransitionContext context)
            {
                Callbacks.Add("Route:" + Format(context.PreviousActivity) + "->" + Format(context.CurrentActivity));
            }

            private static string Format(ActivityAsset activity) => activity != null ? activity.name : "none";
        }

        public sealed class ActivitySequenceRecorder : ActivityContentBehaviour
        {
            public List<string> Callbacks { get; set; }

            protected override void OnActivityContentEntered(ActivityContentLifecycleContext context) =>
                Callbacks.Add("Enter:" + context.Activity.name);

            protected override void OnActivityContentExited(ActivityContentLifecycleContext context) =>
                Callbacks.Add("Exit:" + context.Activity.name);
        }
    }
}
