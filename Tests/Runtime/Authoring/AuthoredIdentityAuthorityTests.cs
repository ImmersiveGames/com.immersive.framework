using System;
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
    /// IF-ID package baseline: stable-ID vs reference equality, required definition tokens, ownership.
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
        public void RouteOwner_WithoutDefinitionToken_Throws()
        {
            Assert.That(
                () => RuntimeContentOwner.Route(
                    "demo.route.a",
                    "Route A",
                    default(RuntimeDefinitionToken)),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void ActivityOwner_WithoutDefinitionToken_Throws()
        {
            Assert.That(
                () => RuntimeContentOwner.Activity(
                    "demo.activity.a",
                    "Activity A",
                    default(RuntimeDefinitionToken)),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void RuntimeContentOwner_SameStableId_DifferentTokens_AreNotEqual()
        {
            RuntimeDefinitionToken leftToken = RuntimeDefinitionToken.MintAnonymous();
            RuntimeDefinitionToken rightToken = RuntimeDefinitionToken.MintAnonymous();

            RuntimeContentOwner left = RuntimeContentOwner.Route(
                "demo.route.shared",
                "Route A",
                leftToken);
            RuntimeContentOwner right = RuntimeContentOwner.Route(
                "demo.route.shared",
                "Route B",
                rightToken);

            Assert.That(left.HasSameStableDefinition(right), Is.True);
            Assert.That(left, Is.Not.EqualTo(right));
            Assert.That(left.GetHashCode(), Is.Not.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void RuntimeContentOwner_EquivalentOwners_HaveCompatibleHashes()
        {
            RuntimeDefinitionToken token = RuntimeDefinitionToken.MintAnonymous();
            RuntimeContentOwner first = RuntimeContentOwner.Activity(
                "demo.activity.a",
                "Activity",
                token);
            RuntimeContentOwner second = RuntimeContentOwner.Activity(
                "demo.activity.a",
                "Activity",
                token);

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        }

        [Test]
        public void RuntimeDefinitionToken_FromUnityObject_IsStableForSameAsset()
        {
            RouteAsset route = CreateRoute("demo.route.a");
            try
            {
                RuntimeDefinitionToken first =
                    RuntimeDefinitionToken.FromUnityObject(route);
                RuntimeDefinitionToken second =
                    RuntimeDefinitionToken.FromUnityObject(route);

                Assert.That(first.IsValid, Is.True);
                Assert.That(first, Is.EqualTo(second));
            }
            finally
            {
                Object.DestroyImmediate(route);
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
                    RuntimeDefinitionToken.FromUnityObject(left));
                RuntimeContentOwner rightOwner = RuntimeContentOwner.Route(
                    right.RouteId.StableText,
                    right.RouteName,
                    RuntimeDefinitionToken.FromUnityObject(right));

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
