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
    public sealed class ActivityCommitAuthorityProjectionTests
    {
        private RouteAsset _route;
        private ActivityAsset _activity;

        [SetUp]
        public void SetUp()
        {
            _route = ScriptableObject.CreateInstance<RouteAsset>();
            _activity = ScriptableObject.CreateInstance<ActivityAsset>();

            SetString(_route, "routeId", "projection.route");
            SetString(_activity, "activityId", "projection.activity");

            Assert.That(_route.HasValidRouteId, Is.True);
            Assert.That(_activity.HasValidActivityId, Is.True);
        }

        [TearDown]
        public void TearDown()
        {
            if (_activity != null)
            {
                Object.DestroyImmediate(_activity);
            }

            if (_route != null)
            {
                Object.DestroyImmediate(_route);
            }
        }

        [Test]
        public void PostCommitActivityClearFailure_ProjectsNoActivityAndPreservesRouteSession()
        {
            var activeFlow = new ActivityFlowStartResult(
                started: true,
                skipped: false,
                keptActive: false,
                cleared: false,
                message: "Activity is active.",
                activityState: new ActivityRuntimeState(
                    ActivityRuntimeStatus.Active,
                    _activity,
                    default,
                    null,
                    "test",
                    "before-clear"),
                previousActivity: null,
                activityContentResult: ActivityContentApplyResult.Empty(_activity),
                activityReadinessState: default);
            var previousState = new FrameworkRuntimeState(
                null,
                _route,
                default,
                default,
                default,
                activeFlow,
                gameFlowStarted: true);
            var committedClearFlow = new ActivityFlowStartResult(
                started: false,
                skipped: false,
                keptActive: false,
                cleared: true,
                message: "Activity authority cleared before reveal failed.",
                activityState: ActivityRuntimeState.None(_activity, "test", "clear"),
                previousActivity: _activity,
                activityContentResult: ActivityContentApplyResult.Empty(),
                activityReadinessState: default);
            FrameworkActivityRequestResult result =
                FrameworkActivityRequestResult.FailedCommittedTargetReveal(
                    "Activity clear committed, but Transition After failed.",
                    targetActivity: null,
                    source: "test",
                    reason: "post-commit-reveal-failure",
                    activityFlowResult: committedClearFlow,
                    operationKind: GameFlowRequestOperationKind.ActivityClear);

            FrameworkRuntimeState projected =
                FrameworkRuntimeState.FromActivityRequestResult(previousState, result);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.DestinationAuthoritative, Is.True);
            Assert.That(projected.CurrentRoute, Is.SameAs(_route));
            Assert.That(projected.GameFlowStarted, Is.True);
            Assert.That(projected.CurrentActivity, Is.Null);
            Assert.That(projected.ActivityFlowResult.Cleared, Is.True);
        }

        [Test]
        public void PreCommitActivityClearFailure_DoesNotClaimDestinationAuthority()
        {
            FrameworkActivityRequestResult result =
                FrameworkActivityRequestResult.FailedPreCommitTransition(
                    "Transition Before failed before Activity clear.",
                    targetActivity: _activity,
                    source: "test",
                    reason: "pre-commit-failure",
                    operationKind: GameFlowRequestOperationKind.ActivityClear);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.DestinationAuthoritative, Is.False);
            Assert.That(result.TargetActivity, Is.SameAs(_activity));
        }

        private static void SetString(UnityEngine.Object target, string propertyName, string value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, "Missing serialized field '" + propertyName + "'.");
            property.stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
