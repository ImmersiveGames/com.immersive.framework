using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.RouteLifecycle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.RouteLifecycle.Tests
{
    public sealed class RouteCommitAuthorityProjectionTests
    {
        private RouteAsset _currentRoute;
        private RouteAsset _targetRoute;
        private ActivityAsset _currentActivity;
        private ActivityAsset _targetActivity;

        [SetUp]
        public void SetUp()
        {
            _currentRoute = CreateRoute("route-projection.current");
            _targetRoute = CreateRoute("route-projection.target");
            _currentActivity = CreateActivity("route-projection.current-activity");
            _targetActivity = CreateActivity("route-projection.target-activity");
        }

        [TearDown]
        public void TearDown()
        {
            Destroy(_targetActivity);
            Destroy(_currentActivity);
            Destroy(_targetRoute);
            Destroy(_currentRoute);
        }

        [Test]
        public void PreCommitRouteFailure_DoesNotClaimDestinationAuthority()
        {
            FrameworkRuntimeState previous = CreateCurrentState();
            FrameworkRouteRequestResult result =
                FrameworkRouteRequestResult.FailedPreCommitTransition(
                    "Transition Before failed before Route replacement.",
                    _targetRoute,
                    "test",
                    "pre-commit-failure");

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.DestinationAuthoritative, Is.False);
            Assert.That(result.TargetRoute, Is.SameAs(_targetRoute));
            Assert.That(previous.CurrentRoute, Is.SameAs(_currentRoute));
            Assert.That(previous.CurrentActivity, Is.SameAs(_currentActivity));
            Assert.That(previous.GameFlowStarted, Is.True);
        }

        [Test]
        public void PostCommitRouteRevealFailure_ProjectsCommittedRouteAndActivity()
        {
            FrameworkRuntimeState previous = CreateCurrentState();
            ActivityFlowStartResult targetActivity = CreateActiveActivityFlow(_targetActivity);
            var committedRoute = new RouteLifecycleStartResult(
                started: true,
                message: "Target Route committed before reveal failed.",
                route: _targetRoute,
                previousRoute: _currentRoute,
                routeExitResult: default,
                routeState: default,
                sceneLifecycleResult: default,
                routeSceneCompositionResult: default,
                routeContentSet: default,
                routeContentEnterResult: default,
                routeContentExitResult: default,
                contentReleaseResult: default,
                activityFlowResult: targetActivity);
            FrameworkRouteRequestResult result =
                FrameworkRouteRequestResult.FailedCommittedTargetReveal(
                    "Target Route committed, but Transition After failed.",
                    _targetRoute,
                    "test",
                    "post-commit-reveal-failure",
                    committedRoute);

            FrameworkRuntimeState projected =
                FrameworkRuntimeState.FromRouteRequestResult(previous, result, true);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.DestinationAuthoritative, Is.True);
            Assert.That(projected.CurrentRoute, Is.SameAs(_targetRoute));
            Assert.That(projected.CurrentActivity, Is.SameAs(_targetActivity));
            Assert.That(projected.GameFlowStarted, Is.True);
        }

        private FrameworkRuntimeState CreateCurrentState()
        {
            return new FrameworkRuntimeState(
                gameApplication: null,
                currentRoute: _currentRoute,
                routeLifecycleResult: default,
                routeContentSet: default,
                primarySceneResult: default,
                activityFlowResult: CreateActiveActivityFlow(_currentActivity),
                gameFlowStarted: true);
        }

        private static ActivityFlowStartResult CreateActiveActivityFlow(
            ActivityAsset activity)
        {
            return new ActivityFlowStartResult(
                started: true,
                skipped: false,
                keptActive: false,
                cleared: false,
                message: "Activity is active.",
                activityState: new ActivityRuntimeState(
                    ActivityRuntimeStatus.Active,
                    activity,
                    default,
                    null,
                    "test",
                    "route-projection"),
                previousActivity: null,
                activityContentResult: ActivityContentApplyResult.Empty(activity),
                activityReadinessState: default);
        }

        private static RouteAsset CreateRoute(string routeId)
        {
            RouteAsset route = ScriptableObject.CreateInstance<RouteAsset>();
            SetString(route, "routeId", routeId);
            Assert.That(route.HasValidRouteId, Is.True);
            return route;
        }

        private static ActivityAsset CreateActivity(string activityId)
        {
            ActivityAsset activity = ScriptableObject.CreateInstance<ActivityAsset>();
            SetString(activity, "activityId", activityId);
            Assert.That(activity.HasValidActivityId, Is.True);
            return activity;
        }

        private static void Destroy(Object asset)
        {
            if (asset != null)
            {
                Object.DestroyImmediate(asset);
            }
        }

        private static void SetString(
            Object target,
            string propertyName,
            string value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null,
                "Missing serialized field '" + propertyName + "'.");
            property.stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
