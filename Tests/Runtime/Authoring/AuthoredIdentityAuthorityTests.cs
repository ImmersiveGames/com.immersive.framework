using System.Reflection;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.Transition;
using NUnit.Framework;
using UnityEngine;

namespace Immersive.Framework.Authoring.Tests
{
    /// <summary>
    /// IF-ID-02 / IF-ID-03 / IF-ID-04 baseline: stable-ID equality vs authored-definition reference equality.
    /// </summary>
    public sealed class AuthoredIdentityAuthorityTests
    {
        [Test]
        public void Route_SameReference_SameStableIdAndSameDefinition()
        {
            RouteAsset route = CreateRoute("demo.route.a");
            try
            {
                Assert.That(route.HasSameStableId(route), Is.True);
                Assert.That(ReferenceEquals(route, route), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(route);
            }
        }

        [Test]
        public void Route_DifferentReferences_DifferentIds_AreDistinctDefinitionsAndStableIds()
        {
            RouteAsset left = CreateRoute("demo.route.a");
            RouteAsset right = CreateRoute("demo.route.b");
            try
            {
                Assert.That(ReferenceEquals(left, right), Is.False);
                Assert.That(left.HasSameStableId(right), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(left);
                Object.DestroyImmediate(right);
            }
        }

        [Test]
        public void Route_DifferentReferences_SameStableId_AreDistinctDefinitionsWithCollision()
        {
            RouteAsset left = CreateRoute("demo.route.shared");
            RouteAsset right = CreateRoute("demo.route.shared");
            try
            {
                Assert.That(ReferenceEquals(left, right), Is.False);
                Assert.That(left.HasSameStableId(right), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(left);
                Object.DestroyImmediate(right);
            }
        }

        [Test]
        public void Activity_DifferentReferences_SameStableId_AreDistinctDefinitionsWithCollision()
        {
            ActivityAsset left = CreateActivity("demo.activity.shared");
            ActivityAsset right = CreateActivity("demo.activity.shared");
            try
            {
                Assert.That(ReferenceEquals(left, right), Is.False);
                Assert.That(left.HasSameStableId(right), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(left);
                Object.DestroyImmediate(right);
            }
        }

        [Test]
        public void ReadinessOperation_OwnsRoute_UsesExactReference_NotStableId()
        {
            RouteAsset assigned = CreateRoute("demo.route.shared");
            RouteAsset colliding = CreateRoute("demo.route.shared");
            ActivityAsset activity = CreateActivity("demo.activity.a");
            try
            {
                var operation = new ActivityEntryReadinessActiveOperation(
                    TransitionOperationId.From("test.identity.route-own"),
                    new ActivityReadinessOccurrence(activity, 1),
                    assigned);

                Assert.That(operation.OwnsRoute(assigned), Is.True);
                Assert.That(operation.OwnsRoute(colliding), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(assigned);
                Object.DestroyImmediate(colliding);
                Object.DestroyImmediate(activity);
            }
        }

        [Test]
        public void ReadinessOperation_OwnsActivity_UsesExactReference_NotStableId()
        {
            RouteAsset route = CreateRoute("demo.route.a");
            ActivityAsset assigned = CreateActivity("demo.activity.shared");
            ActivityAsset colliding = CreateActivity("demo.activity.shared");
            try
            {
                var operation = new ActivityEntryReadinessActiveOperation(
                    TransitionOperationId.From("test.identity.activity-own"),
                    new ActivityReadinessOccurrence(assigned, 1),
                    route);

                Assert.That(operation.OwnsActivity(assigned), Is.True);
                Assert.That(operation.OwnsActivity(colliding), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(route);
                Object.DestroyImmediate(assigned);
                Object.DestroyImmediate(colliding);
            }
        }

        [Test]
        public void RuntimeContentOwner_DifferentDefinitionTokens_DoNotShareReleaseAuthority()
        {
            RouteAsset leftAsset = CreateRoute("demo.route.shared");
            RouteAsset rightAsset = CreateRoute("demo.route.shared");
            try
            {
                RuntimeContentOwner left = RuntimeContentOwner.Route(
                    "demo.route.shared",
                    "Route A",
                    leftAsset.GetEntityId());
                RuntimeContentOwner right = RuntimeContentOwner.Route(
                    "demo.route.shared",
                    "Route B",
                    rightAsset.GetEntityId());

                Assert.That(left.HasSameStableDefinition(right), Is.True);
                Assert.That(left, Is.Not.EqualTo(right));
                Assert.That(left.StableText, Does.Contain("#def-"));
                Assert.That(right.StableText, Does.Contain("#def-"));
            }
            finally
            {
                Object.DestroyImmediate(leftAsset);
                Object.DestroyImmediate(rightAsset);
            }
        }

        [Test]
        public void RuntimeContentOwner_SameAssetToken_IsSameOperationalOwner()
        {
            ActivityAsset activity = CreateActivity("demo.activity.a");
            try
            {
                EntityId token = activity.GetEntityId();
                RuntimeContentOwner first = RuntimeContentOwner.Activity(
                    "demo.activity.a",
                    "Activity",
                    token);
                RuntimeContentOwner second = RuntimeContentOwner.Activity(
                    "demo.activity.a",
                    "Activity",
                    token);

                Assert.That(first, Is.EqualTo(second));
            }
            finally
            {
                Object.DestroyImmediate(activity);
            }
        }

        [Test]
        public void RuntimeContentOwner_FromDistinctAssetsWithSameStableId_UseDistinctTokens()
        {
            RouteAsset left = CreateRoute("demo.route.shared");
            RouteAsset right = CreateRoute("demo.route.shared");
            try
            {
                RuntimeContentOwner leftOwner = RuntimeContentOwner.Route(
                    left.RouteId.StableText,
                    left.RouteName,
                    left.GetEntityId());
                RuntimeContentOwner rightOwner = RuntimeContentOwner.Route(
                    right.RouteId.StableText,
                    right.RouteName,
                    right.GetEntityId());

                Assert.That(left.HasSameStableId(right), Is.True);
                Assert.That(leftOwner, Is.Not.EqualTo(rightOwner));
            }
            finally
            {
                Object.DestroyImmediate(left);
                Object.DestroyImmediate(right);
            }
        }

        private static RouteAsset CreateRoute(string stableId)
        {
            RouteAsset route = ScriptableObject.CreateInstance<RouteAsset>();
            SetPrivateField(route, "routeId", stableId);
            return route;
        }

        private static ActivityAsset CreateActivity(string stableId)
        {
            ActivityAsset activity = ScriptableObject.CreateInstance<ActivityAsset>();
            SetPrivateField(activity, "activityId", stableId);
            return activity;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field '{fieldName}' on {target.GetType().Name}.");
            field.SetValue(target, value);
        }
    }
}
