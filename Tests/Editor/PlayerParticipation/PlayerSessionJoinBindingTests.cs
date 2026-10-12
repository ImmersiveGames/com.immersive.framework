using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.Identity;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.SceneLifecycle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation.Tests
{
    public sealed class PlayerSessionJoinBindingTests
    {
        private readonly List<UnityEngine.Object> _objects = new();

        [TearDown]
        public void TearDown()
        {
            for (int index = _objects.Count - 1; index >= 0; index--)
            {
                if (_objects[index] != null)
                    UnityEngine.Object.DestroyImmediate(_objects[index]);
            }

            _objects.Clear();
        }

        [Test]
        public void Capture_UnspecifiedScopeRejectsAsConfigurationBeforeRuntimeLookup()
        {
            PlayerSessionScopedBindingRecipient recipient = CreateRecipient(
                LocalPlayerProvisioningConsumerScope.Activity);

            PlayerSessionJoinBindingLeaseValidation validation =
                PlayerSessionJoinBindingLease.TryCapture(
                    null,
                    null,
                    null,
                    recipient,
                    LocalPlayerProvisioningConsumerScope.Unspecified,
                    default,
                    null,
                    out PlayerSessionJoinBindingLease lease);

            Assert.That(validation, Is.EqualTo(PlayerSessionJoinBindingLeaseValidation.RejectedConfiguration));
            Assert.That(lease, Is.Null);
        }

        [Test]
        public void Capture_OwnerScopeMismatchRejectsAsConfiguration()
        {
            PlayerSessionScopedBindingRecipient recipient = CreateRecipient(
                LocalPlayerProvisioningConsumerScope.Route);
            RuntimeContentOwner sessionOwner = RuntimeContentOwner.Session(
                "session-test",
                "Session test");

            PlayerSessionJoinBindingLeaseValidation validation =
                PlayerSessionJoinBindingLease.TryCapture(
                    null,
                    null,
                    null,
                    recipient,
                    LocalPlayerProvisioningConsumerScope.Route,
                    sessionOwner,
                    null,
                    out PlayerSessionJoinBindingLease lease);

            Assert.That(validation, Is.EqualTo(PlayerSessionJoinBindingLeaseValidation.RejectedConfiguration));
            Assert.That(lease, Is.Null);
        }

        [Test]
        public void Capture_ValidRouteOwnerWithoutLiveRuntimeFailsClosedAsUnavailable()
        {
            PlayerSessionScopedBindingRecipient recipient = CreateRecipient(
                LocalPlayerProvisioningConsumerScope.Route);
            var routeDefinition = ScriptableObject.CreateInstance<RouteAsset>();
            _objects.Add(routeDefinition);
            RuntimeContentOwner owner = RuntimeContentOwner.Route(
                "route-test",
                "Route test",
                RuntimeDefinitionToken.FromUnityObject(routeDefinition));

            PlayerSessionJoinBindingLeaseValidation validation =
                PlayerSessionJoinBindingLease.TryCapture(
                    null,
                    null,
                    null,
                    recipient,
                    LocalPlayerProvisioningConsumerScope.Route,
                    owner,
                    null,
                    out PlayerSessionJoinBindingLease lease);

            Assert.That(validation, Is.EqualTo(PlayerSessionJoinBindingLeaseValidation.RejectedUnavailable));
            Assert.That(lease, Is.Null);
        }

        [Test]
        public void Recipient_ScopeMismatchCannotBind()
        {
            PlayerSessionScopedBindingRecipient recipient = CreateRecipient(
                LocalPlayerProvisioningConsumerScope.Route);
            var binding = new TestBinding();

            bool bound = recipient.TryBind(
                binding,
                LocalPlayerProvisioningConsumerScope.Activity,
                out string issue);

            Assert.That(bound, Is.False);
            Assert.That(issue, Does.Contain("does not match"));
            Assert.That(binding, Is.Not.Null);
        }

        [Test]
        public void Recipient_ReleaseIsIdempotentAndCanReleaseAfterRebind()
        {
            var gameObject = new GameObject("join-binding-recipient-release");
            _objects.Add(gameObject);
            Component component = gameObject.AddComponent<PlayerGameplayAvailabilityBlockTrigger>();
            int releaseCalls = 0;
            var recipient = new PlayerSessionScopedBindingRecipient(
                component,
                LocalPlayerProvisioningConsumerScope.Activity,
                typeof(PlayerGameplayAvailabilityBlockTrigger),
                (out string issue) => { issue = string.Empty; return true; },
                _ => new TestBinding(),
                (IDisposable binding,
                    LocalPlayerProvisioningConsumerScope scope,
                    out string issue) => { issue = string.Empty; return binding != null; },
                (reason, stale) => releaseCalls++);
            var binding = new TestBinding();

            Assert.That(recipient.TryBind(
                binding, LocalPlayerProvisioningConsumerScope.Activity, out _), Is.True);
            recipient.Release("first release", false);
            recipient.Release("duplicate release", false);
            Assert.That(releaseCalls, Is.EqualTo(1));

            Assert.That(recipient.TryBind(
                binding, LocalPlayerProvisioningConsumerScope.Activity, out _), Is.True);
            recipient.Release("release after rebind", true);
            Assert.That(releaseCalls, Is.EqualTo(2));
        }

        [Test]
        public void Capture_DestroyedRecipientFailsClosedAsUnavailable()
        {
            PlayerSessionScopedBindingRecipient recipient = CreateRecipient(
                LocalPlayerProvisioningConsumerScope.Route);
            UnityEngine.Object.DestroyImmediate(recipient.Component.gameObject);
            RouteAsset routeDefinition = CreateRoute();
            RuntimeContentOwner owner = RuntimeContentOwner.Route(
                "route-join-binding-test",
                "Route test",
                RuntimeDefinitionToken.FromUnityObject(routeDefinition));

            PlayerSessionJoinBindingLeaseValidation validation =
                PlayerSessionJoinBindingLease.TryCapture(
                    null,
                    null,
                    null,
                    recipient,
                    LocalPlayerProvisioningConsumerScope.Route,
                    owner,
                    null,
                    out PlayerSessionJoinBindingLease lease);

            Assert.That(validation, Is.EqualTo(PlayerSessionJoinBindingLeaseValidation.RejectedUnavailable));
            Assert.That(lease, Is.Null);
        }

        [Test]
        public async System.Threading.Tasks.Task ActivityReentryOnSameAssetUsesNewReadinessOccurrence()
        {
            RouteAsset route = CreateRoute();
            ActivityAsset activity = CreateActivity();
            SetObjectReference(route, "startupActivity", activity);
            GameObject routeRoot = CreateRouteRoot(route);

            var flow = new ActivityFlowRuntime(
                new RuntimeContentRuntime(),
                new SceneLifecycleRuntime());
            flow.SetRouteContentDiscoveryScope(CreateRouteScope(route, routeRoot));

            ActivityFlowStartResult firstEntry =
                await flow.StartStartupActivityAsync(route, "test", "first-entry");
            Assert.That(firstEntry.Completed, Is.True, firstEntry.Message);
            ActivityReadinessOccurrence firstOccurrence = flow.CurrentOccurrence;
            Assert.That(firstOccurrence.IsValid, Is.True);

            ActivityFlowStartResult clear =
                await flow.ClearActivityAsync(route, "test", "clear-before-reentry");
            Assert.That(clear.Completed, Is.True, clear.Message);

            ActivityFlowStartResult secondEntry =
                await flow.StartStartupActivityAsync(route, "test", "same-asset-reentry");
            Assert.That(secondEntry.Completed, Is.True, secondEntry.Message);
            ActivityReadinessOccurrence secondOccurrence = flow.CurrentOccurrence;

            Assert.That(secondOccurrence.Activity, Is.SameAs(activity));
            Assert.That(secondOccurrence.TransitionSequence, Is.GreaterThan(firstOccurrence.TransitionSequence));
            Assert.That(secondOccurrence.Matches(activity, firstOccurrence.TransitionSequence), Is.False);
        }

        private PlayerSessionScopedBindingRecipient CreateRecipient(
            LocalPlayerProvisioningConsumerScope scope)
        {
            var gameObject = new GameObject("join-binding-recipient");
            _objects.Add(gameObject);
            PlayerGameplayAvailabilityBlockTrigger consumer =
                gameObject.AddComponent<PlayerGameplayAvailabilityBlockTrigger>();
            var serialized = new SerializedObject(consumer);
            serialized.FindProperty("scope").intValue = (int)scope;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return PlayerSessionScopedBindingRecipient.ForLegacyConsumer(
                consumer,
                _ => null);
        }

        private RouteAsset CreateRoute()
        {
            RouteAsset route = ScriptableObject.CreateInstance<RouteAsset>();
            SetString(route, "routeId", "route-join-binding-test");
            _objects.Add(route);
            return route;
        }

        private ActivityAsset CreateActivity()
        {
            ActivityAsset activity = ScriptableObject.CreateInstance<ActivityAsset>();
            SetString(activity, "activityId", "activity-join-binding-test");
            _objects.Add(activity);
            return activity;
        }

        private GameObject CreateRouteRoot(RouteAsset route)
        {
            var root = new GameObject("route-join-binding-content");
            _objects.Add(root);
            RouteContentContribution contribution = root.AddComponent<RouteContentContribution>();
            SetObjectReference(contribution, "route", route);
            return root;
        }

        private static RouteContentDiscoveryScope CreateRouteScope(
            RouteAsset route,
            GameObject root)
        {
            FrameworkIdentityKey owner = FrameworkIdentityKey.From(
                FrameworkIdentityDomain.Route,
                "route-join-binding-test");
            var identity = new FrameworkContentIdentity(
                owner,
                FrameworkContentScope.Route,
                FrameworkContentKind.Scene,
                new FrameworkContentId("primary"));
            var planEntry = new RouteSceneCompositionPlanEntry(
                identity,
                "primary",
                root.scene.name,
                root.scene.path,
                RouteSceneRole.Primary,
                FrameworkContentRequiredness.Required,
                RouteContentOwnership.Owned,
                RouteSceneLoadMode.Single,
                0,
                true);
            RouteSceneCompositionResultEntry resultEntry =
                RouteSceneCompositionResultEntry.LoadedEntry(
                    planEntry,
                    true,
                    true,
                    "join-binding-test");
            var scenes = new[] { new RouteContentDiscoveryScene(resultEntry) };
            return new RouteContentDiscoveryScope(route, scenes, new[] { root });
        }

        private static void SetString(UnityEngine.Object target, string propertyName, string value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectReference(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private sealed class TestBinding : System.IDisposable
        {
            public void Dispose() { }
        }
    }
}
