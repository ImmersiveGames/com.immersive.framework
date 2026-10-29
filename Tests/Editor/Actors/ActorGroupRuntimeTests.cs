using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.SceneLifecycle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Actors.Tests
{
    public sealed class ActorGroupRuntimeTests
    {
        private readonly List<Object> _created = new();

        [Test]
        public void RuntimeAccessContracts_AreExperimental()
        {
            Type[] contracts = { typeof(ActorGroupSnapshot), typeof(ActorGroupAccessBinding) };
            foreach (Type contract in contracts)
            {
                FrameworkApiStatusAttribute metadata = contract.GetCustomAttribute<FrameworkApiStatusAttribute>(false);
                Assert.That(metadata, Is.Not.Null, contract.FullName + " is missing its maturity attribute.");
                Assert.That(metadata.Status, Is.EqualTo(FrameworkApiStatus.Experimental), contract.FullName);
            }
        }

        [TearDown]
        public void TearDown()
        {
            for (int index = _created.Count - 1; index >= 0; index--)
            {
                if (_created[index] != null)
                {
                    Object.DestroyImmediate(_created[index]);
                }
            }

            _created.Clear();
        }

        [Test]
        public void CreateGroup_FromDefinition_UsesDefinitionGroupIdWithoutInstantiatingOnAssetLoad()
        {
            var runtime = new ActorGroupRuntime();
            RuntimeContentOwner owner = RuntimeContentOwner.Session("session.groups.definition", "Session");
            GroupDefinitionAsset definition = CreateDefinition("group.definition");
            GameObject root = CreateAccessRoot(out ActorGroupAccessBinding access);
            GroupId groupId = definition.GroupId;

            Assert.That(runtime.TryGetGroup(groupId, out _), Is.False);
            Assert.That(access.TryCreateGroup(definition, out _, out string unboundIssue), Is.False);
            StringAssert.Contains("bound", unboundIssue);

            RegisterOwner(runtime, owner, root);

            Assert.That(access.TryCreateGroup(definition, out ActorGroupSnapshot group, out string issue), Is.True, issue);
            Assert.That(group.IsValid, Is.True);
            Assert.That(group.GroupId, Is.EqualTo(groupId));
            Assert.That(group.Profile, Is.Null);
            Assert.That(runtime.TryGetGroup(groupId, out ActorGroupSnapshot registered), Is.True);
            Assert.That(registered.GroupId, Is.EqualTo(groupId));
        }

        [Test]
        public void CreateGroup_FromDynamicIdentity_UsesExplicitIdAndOptionalProfile()
        {
            var runtime = new ActorGroupRuntime();
            RuntimeContentOwner owner = RuntimeContentOwner.Session("session.groups.dynamic", "Session");
            GroupProfile profile = CreateProfile("profile.dynamic");
            GameObject root = CreateAccessRoot(out ActorGroupAccessBinding access);
            RegisterOwner(runtime, owner, root);

            GroupId id = GroupId.From("group.dynamic");
            Assert.That(access.TryCreateGroup(id, profile, out ActorGroupSnapshot group, out string issue), Is.True, issue);
            Assert.That(group.GroupId, Is.EqualTo(id));
            Assert.That(group.Profile, Is.SameAs(profile));
            Assert.That(group.HasProfile, Is.True);
        }

        [Test]
        public void CreateGroup_RejectsInvalidIdentityAndInvalidProfile()
        {
            var runtime = new ActorGroupRuntime();
            RuntimeContentOwner owner = RuntimeContentOwner.Session("session.groups.invalid", "Session");
            GameObject root = CreateAccessRoot(out ActorGroupAccessBinding access);
            GroupProfile profile = Track(ScriptableObject.CreateInstance<GroupProfile>());
            RegisterOwner(runtime, owner, root);

            Assert.That(access.TryCreateGroup(default, null, out _, out string identityIssue), Is.False);
            StringAssert.Contains("GroupId", identityIssue);
            Assert.That(access.TryCreateGroup(GroupId.From("group.invalid-profile"), profile, out _, out string profileIssue), Is.False);
            StringAssert.Contains("Profile", profileIssue);
        }

        [Test]
        public void DuplicateLiveGroupId_IsRejectedAcrossOwnersWithoutChangingTheFirstGroup()
        {
            var runtime = new ActorGroupRuntime();
            RuntimeContentOwner firstOwner = RuntimeContentOwner.Session("session.groups.duplicate", "Session");
            RuntimeContentOwner secondOwner = CreateActivityOwner("activity.groups.duplicate");
            GameObject firstRoot = CreateAccessRoot(out ActorGroupAccessBinding firstAccess);
            GameObject secondRoot = CreateAccessRoot(out ActorGroupAccessBinding secondAccess);
            RegisterOwner(runtime, firstOwner, firstRoot);
            RegisterOwner(runtime, secondOwner, secondRoot);
            GroupId sharedId = GroupId.From("group.shared");

            Assert.That(firstAccess.TryCreateGroup(sharedId, null, out ActorGroupSnapshot first, out string firstIssue), Is.True, firstIssue);
            Assert.That(secondAccess.TryCreateGroup(sharedId, null, out _, out string duplicateIssue), Is.False);
            StringAssert.Contains(sharedId.StableText, duplicateIssue);
            Assert.That(runtime.TryGetGroup(sharedId, out ActorGroupSnapshot current), Is.True);
            Assert.That(current.GroupId, Is.EqualTo(first.GroupId));
        }

        [Test]
        public void CloseOwner_ReleasesOnlyGroupsBoundToItsExactOwner()
        {
            var runtime = new ActorGroupRuntime();
            RouteAsset firstRoute = CreateRoute("route.same");
            RouteAsset secondRoute = CreateRoute("route.same");
            RuntimeContentOwner firstOwner = CreateRouteOwner(firstRoute);
            RuntimeContentOwner secondOwner = CreateRouteOwner(secondRoute);
            GameObject firstRoot = CreateAccessRoot(out ActorGroupAccessBinding firstAccess);
            GameObject secondRoot = CreateAccessRoot(out ActorGroupAccessBinding secondAccess);
            RegisterOwner(runtime, firstOwner, firstRoot);
            RegisterOwner(runtime, secondOwner, secondRoot);
            GroupId firstId = GroupId.From("group.first");
            GroupId secondId = GroupId.From("group.second");
            Assert.That(firstAccess.TryCreateGroup(firstId, null, out _, out string firstIssue), Is.True, firstIssue);
            Assert.That(secondAccess.TryCreateGroup(secondId, null, out _, out string secondIssue), Is.True, secondIssue);

            Assert.That(runtime.TryReleaseOwner(firstOwner, out string releaseIssue), Is.True, releaseIssue);

            Assert.That(runtime.TryGetGroup(firstId, out _), Is.False);
            Assert.That(runtime.TryGetGroup(secondId, out ActorGroupSnapshot stillLive), Is.True);
            Assert.That(stillLive.GroupId, Is.EqualTo(secondId));
            Assert.That(firstAccess.IsAccessAvailable, Is.False);
            Assert.That(secondAccess.IsAccessAvailable, Is.True);
        }

        [Test]
        public void EmptyGroup_RemainsRegisteredUntilItsOwnerCloses()
        {
            var runtime = new ActorGroupRuntime();
            RuntimeContentOwner owner = RuntimeContentOwner.Session("session.groups.empty", "Session");
            GameObject root = CreateAccessRoot(out ActorGroupAccessBinding access);
            RegisterOwner(runtime, owner, root);
            GroupId groupId = GroupId.From("group.empty");

            Assert.That(access.TryCreateGroup(groupId, null, out _, out string issue), Is.True, issue);
            Assert.That(runtime.TryGetGroup(groupId, out ActorGroupSnapshot group), Is.True);
            Assert.That(group.GroupId, Is.EqualTo(groupId));

            Assert.That(runtime.TryReleaseOwner(owner, out issue), Is.True, issue);
            Assert.That(runtime.TryGetGroup(groupId, out _), Is.False);
        }

        [Test]
        public void OneOwner_CanCreateAndQueryMultipleGroups()
        {
            var runtime = new ActorGroupRuntime();
            RuntimeContentOwner owner = RuntimeContentOwner.Session("session.groups.multiple", "Session");
            GameObject root = CreateAccessRoot(out ActorGroupAccessBinding access);
            RegisterOwner(runtime, owner, root);
            GroupId firstId = GroupId.From("group.multiple.first");
            GroupId secondId = GroupId.From("group.multiple.second");

            Assert.That(access.TryCreateGroup(firstId, null, out _, out string firstIssue), Is.True, firstIssue);
            Assert.That(access.TryCreateGroup(secondId, null, out _, out string secondIssue), Is.True, secondIssue);
            Assert.That(access.TryGetGroup(firstId, out ActorGroupSnapshot first, out string lookupIssue), Is.True, lookupIssue);
            Assert.That(first.GroupId, Is.EqualTo(firstId));
            Assert.That(runtime.TryGetGroup(secondId, out ActorGroupSnapshot second), Is.True);
            Assert.That(second.GroupId, Is.EqualTo(secondId));
        }

        [Test]
        public void OwnerRegistration_RejectsUnsupportedScopeAndMismatchedBindingOwner()
        {
            var runtime = new ActorGroupRuntime();
            RuntimeContentOwner transientOwner = RuntimeContentOwner.Transient("runtime.groups", "Transient");
            GameObject root = CreateAccessRoot(out _);

            Assert.That(runtime.TryRegisterOwnerContent(transientOwner, new[] { root }, out string unsupportedIssue), Is.False);
            StringAssert.Contains("Session", unsupportedIssue);
            Assert.That(runtime.TryReleaseOwner(transientOwner, out string unsupportedReleaseIssue), Is.False);
            StringAssert.Contains("scope", unsupportedReleaseIssue.ToLowerInvariant());
            RuntimeContentOwner firstOwner = RuntimeContentOwner.Session("session.groups.binding", "Session");
            RuntimeContentOwner secondOwner = CreateActivityOwner("activity.groups.binding");
            Assert.That(runtime.TryRegisterOwnerContent(firstOwner, new[] { root }, out string firstIssue), Is.True, firstIssue);
            Assert.That(runtime.TryRegisterOwnerContent(secondOwner, new[] { root }, out string mismatchIssue), Is.False);
            StringAssert.Contains("owner", mismatchIssue.ToLowerInvariant());
        }

        [Test]
        public void RouteClosePath_ReleasesGroupsForTheExactRouteOwner()
        {
            var runtime = new ActorGroupRuntime();
            RouteAsset route = CreateRoute("route.close");
            RuntimeContentOwner owner = CreateRouteOwner(route);
            GameObject root = CreateAccessRoot(out ActorGroupAccessBinding access);
            RegisterOwner(runtime, owner, root);
            GroupId groupId = GroupId.From("group.route-close");
            Assert.That(access.TryCreateGroup(groupId, null, out _, out string issue), Is.True, issue);

            var routeRuntime = new RouteLifecycleRuntime(new RuntimeContentRuntime());
            routeRuntime.SetActorGroupRuntime(runtime);
            MethodInfo release = typeof(RouteLifecycleRuntime).GetMethod(
                "TryReleasePreviousRouteResettableRegistration",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(release, Is.Not.Null);
            object[] arguments = { route, "test", "route-exit", null };

            bool released = (bool)release.Invoke(routeRuntime, arguments);

            Assert.That(released, Is.True, arguments[3] as string);
            Assert.That(runtime.TryGetGroup(groupId, out _), Is.False);
        }

        [Test]
        public void ActivityClosePath_ReleasesGroupsForTheExactActivityOwner()
        {
            var runtime = new ActorGroupRuntime();
            ActivityAsset activity = CreateActivity("activity.close");
            RuntimeContentOwner owner = CreateActivityOwner(activity);
            GameObject root = CreateAccessRoot(out ActorGroupAccessBinding access);
            RegisterOwner(runtime, owner, root);
            GroupId groupId = GroupId.From("group.activity-close");
            Assert.That(access.TryCreateGroup(groupId, null, out _, out string issue), Is.True, issue);

            var activityRuntime = new ActivityFlowRuntime(new RuntimeContentRuntime(), new SceneLifecycleRuntime());
            activityRuntime.SetActorGroupRuntime(runtime);
            MethodInfo release = typeof(ActivityFlowRuntime).GetMethod(
                "ReleaseStableObjectBindingsForPreviousActivity",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(release, Is.Not.Null);

            release.Invoke(activityRuntime, new object[] { activity });

            Assert.That(runtime.TryGetGroup(groupId, out _), Is.False);
        }

        [Test]
        public void SessionClosePath_ReleasesGroupsForTheExactSessionOwner()
        {
            var runtimeObject = CreateObject("Framework Runtime Host");
            FrameworkRuntimeHost host = runtimeObject.AddComponent<FrameworkRuntimeHost>();
            RuntimeContentOwner owner = RuntimeContentOwner.Session("session.groups.close", "Session");
            var scopeResult = new RuntimeScopeLifecycleResult(
                RuntimeContentScope.Session,
                owner,
                null,
                null,
                default,
                0,
                "test",
                "session-close");
            SetField(host, "_runtimeSessionScopeResult", scopeResult);
            ActorGroupRuntime runtime = host.ActorGroups;
            GameObject accessRoot = CreateAccessRoot(out ActorGroupAccessBinding access);
            RegisterOwner(runtime, owner, accessRoot);
            GroupId groupId = GroupId.From("group.session-close");
            Assert.That(access.TryCreateGroup(groupId, null, out _, out string createIssue), Is.True, createIssue);

            Assert.That(host.TryReleaseSessionActorGroups(out string issue), Is.True, issue);
            Assert.That(host.TryReleaseSessionActorGroups(out issue), Is.True, issue);

            Assert.That(runtime.TryGetGroup(groupId, out _), Is.False);
            Assert.That(access.IsAccessAvailable, Is.False);
        }

        private GroupDefinitionAsset CreateDefinition(string id)
        {
            GroupDefinitionAsset definition = Track(ScriptableObject.CreateInstance<GroupDefinitionAsset>());
            SetString(definition, "groupId", id);
            return definition;
        }

        private GroupProfile CreateProfile(string id)
        {
            GroupProfile profile = Track(ScriptableObject.CreateInstance<GroupProfile>());
            SetString(profile, "groupProfileId", id);
            return profile;
        }

        private RouteAsset CreateRoute(string id)
        {
            RouteAsset route = Track(ScriptableObject.CreateInstance<RouteAsset>());
            SetString(route, "routeId", id);
            return route;
        }

        private ActivityAsset CreateActivity(string id)
        {
            ActivityAsset activity = Track(ScriptableObject.CreateInstance<ActivityAsset>());
            SetString(activity, "activityId", id);
            return activity;
        }

        private GameObject CreateAccessRoot(out ActorGroupAccessBinding access)
        {
            GameObject root = Track(new GameObject("Actor Group Access Root"));
            access = root.AddComponent<ActorGroupAccessBinding>();
            return root;
        }

        private GameObject CreateObject(string name) => Track(new GameObject(name));

        private T Track<T>(T value) where T : Object
        {
            _created.Add(value);
            return value;
        }

        private static void RegisterOwner(
            ActorGroupRuntime runtime,
            RuntimeContentOwner owner,
            GameObject root)
        {
            Assert.That(runtime.TryRegisterOwnerContent(owner, new[] { root }, out string issue), Is.True, issue);
            Assert.That(runtime.TryCommitOwner(owner, out issue), Is.True, issue);
        }

        private static RuntimeContentOwner CreateRouteOwner(RouteAsset route) =>
            RuntimeContentOwner.Route(
                route.RouteId.StableText,
                route.RouteName,
                RuntimeDefinitionToken.FromUnityObject(route));

        private static RuntimeContentOwner CreateActivityOwner(string activityId) =>
            RuntimeContentOwner.Activity(
                activityId,
                activityId,
                RuntimeDefinitionToken.MintAnonymous());

        private static RuntimeContentOwner CreateActivityOwner(ActivityAsset activity) =>
            RuntimeContentOwner.Activity(
                activity.ActivityId.StableText,
                activity.ActivityName,
                RuntimeDefinitionToken.FromUnityObject(activity));

        private static void SetString(UnityEngine.Object target, string propertyName, string value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}
