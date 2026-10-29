using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Common;
using Immersive.Framework.Identity;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Identity.Tests
{
    public sealed class FrameworkIdentityConformanceTests
    {
        private readonly List<UnityEngine.Object> _transientObjects = new();
        private readonly List<UnityEngine.Object> _assets = new();
        private string _assetFolder;

        [SetUp]
        public void SetUp()
        {
            string folderName = "__ImmersiveFrameworkIdentityTests_" + Guid.NewGuid().ToString("N");
            _assetFolder = "Assets/" + folderName;
            AssetDatabase.CreateFolder("Assets", folderName);
        }

        [TearDown]
        public void TearDown()
        {
            for (int index = _transientObjects.Count - 1; index >= 0; index--)
            {
                if (_transientObjects[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_transientObjects[index]);
                }
            }

            _transientObjects.Clear();
            _assets.Clear();
            if (!string.IsNullOrEmpty(_assetFolder))
            {
                AssetDatabase.DeleteAsset(_assetFolder);
            }
        }

        [Test]
        public void FrameworkIdentityValue_NormalizesWhitespaceAndUsesOrdinalEqualityAndHashing()
        {
            var normalized = new FrameworkIdentityValue("  Route.Main  ");
            var same = new FrameworkIdentityValue("Route.Main");
            var differentCase = new FrameworkIdentityValue("route.main");

            Assert.That(normalized.Value, Is.EqualTo("Route.Main"));
            Assert.That(normalized, Is.EqualTo(same));
            Assert.That(normalized.GetHashCode(), Is.EqualTo(same.GetHashCode()));
            Assert.That(normalized, Is.Not.EqualTo(differentCase));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" \t ")]
        public void FrameworkIdentityValue_RejectsMissingText(string value)
        {
            Assert.Throws<ArgumentException>(() => new FrameworkIdentityValue(value));
        }

        [Test]
        public void FrameworkIdentityDomain_PreservesTheCanonicalDomainSetAndValues()
        {
            CollectionAssert.AreEqual(
                new[]
                {
                    nameof(FrameworkIdentityDomain.Unspecified),
                    nameof(FrameworkIdentityDomain.Application),
                    nameof(FrameworkIdentityDomain.Session),
                    nameof(FrameworkIdentityDomain.Route),
                    nameof(FrameworkIdentityDomain.Activity),
                    nameof(FrameworkIdentityDomain.Content),
                    nameof(FrameworkIdentityDomain.Local),
                    nameof(FrameworkIdentityDomain.Runtime),
                    nameof(FrameworkIdentityDomain.Diagnostics),
                    nameof(FrameworkIdentityDomain.Qa),
                    nameof(FrameworkIdentityDomain.CycleReset),
                    nameof(FrameworkIdentityDomain.ObjectEntry),
                    nameof(FrameworkIdentityDomain.ObjectReset),
                    nameof(FrameworkIdentityDomain.Transition),
                    nameof(FrameworkIdentityDomain.TransitionEffect),
                    nameof(FrameworkIdentityDomain.Pause),
                    nameof(FrameworkIdentityDomain.Snapshot),
                    nameof(FrameworkIdentityDomain.Preferences),
                    nameof(FrameworkIdentityDomain.ProgressionSave),
                    nameof(FrameworkIdentityDomain.Loading),
                    nameof(FrameworkIdentityDomain.UnityInput),
                    nameof(FrameworkIdentityDomain.Actor),
                    nameof(FrameworkIdentityDomain.PlayerSlot),
                    nameof(FrameworkIdentityDomain.ActorProfile),
                    nameof(FrameworkIdentityDomain.Group),
                    nameof(FrameworkIdentityDomain.GroupProfile)
                },
                Enum.GetNames(typeof(FrameworkIdentityDomain)));
            CollectionAssert.AreEqual(
                new FrameworkIdentityDomain[]
                {
                    FrameworkIdentityDomain.Unspecified,
                    FrameworkIdentityDomain.Application,
                    FrameworkIdentityDomain.Session,
                    FrameworkIdentityDomain.Route,
                    FrameworkIdentityDomain.Activity,
                    FrameworkIdentityDomain.Content,
                    FrameworkIdentityDomain.Local,
                    FrameworkIdentityDomain.Runtime,
                    FrameworkIdentityDomain.Diagnostics,
                    FrameworkIdentityDomain.Qa,
                    FrameworkIdentityDomain.CycleReset,
                    FrameworkIdentityDomain.ObjectEntry,
                    FrameworkIdentityDomain.ObjectReset,
                    FrameworkIdentityDomain.Transition,
                    FrameworkIdentityDomain.TransitionEffect,
                    FrameworkIdentityDomain.Pause,
                    FrameworkIdentityDomain.Snapshot,
                    FrameworkIdentityDomain.Preferences,
                    FrameworkIdentityDomain.ProgressionSave,
                    FrameworkIdentityDomain.Loading,
                    FrameworkIdentityDomain.UnityInput,
                    FrameworkIdentityDomain.Actor,
                    FrameworkIdentityDomain.PlayerSlot,
                    FrameworkIdentityDomain.ActorProfile,
                    FrameworkIdentityDomain.Group,
                    FrameworkIdentityDomain.GroupProfile
                },
                (FrameworkIdentityDomain[])Enum.GetValues(typeof(FrameworkIdentityDomain)));
            CollectionAssert.AreEqual(
                new[]
                {
                    0, 10, 20, 30, 40, 50, 60, 80, 90, 100, 110, 120,
                    130, 140, 150, 160, 170, 180, 190, 200, 210, 230, 240, 250, 260, 270
                },
                Array.ConvertAll(
                    (FrameworkIdentityDomain[])Enum.GetValues(typeof(FrameworkIdentityDomain)),
                    domain => (int)domain));
        }

        [Test]
        public void DefaultIdentityValuesAndWrappersRemainInvalid()
        {
            FrameworkIdentityValue value = default;
            FrameworkIdentityKey key = default;
            RouteId route = default;
            ActivityId activity = default;
            PlayerSlotId slot = default;

            Assert.That(value.IsValid, Is.False);
            Assert.That(value.Value, Is.Empty);
            Assert.That(key.IsValid, Is.False);
            Assert.That(route.IsValid, Is.False);
            Assert.That(route.StableText, Is.Empty);
            Assert.That(activity.IsValid, Is.False);
            Assert.That(activity.StableText, Is.Empty);
            Assert.That(slot.IsValid, Is.False);
            Assert.Throws<ArgumentException>(() => _ = slot.Key);
        }

        [Test]
        public void RouteAndActivityIdentityText_EnforcesCanonicalGrammar()
        {
            string[] valid = { "main", "chapter-1.part-2", "route9" };
            string[] invalid = { "", "Main", ".main", "main.", "main--extra", "main..extra", "main_extra", "main extra" };

            foreach (string value in valid)
            {
                Assert.That(RouteId.IsValidText(value), Is.True, "RouteId: " + value);
                Assert.That(ActivityId.IsValidText(value), Is.True, "ActivityId: " + value);
                RouteId route = RouteId.From(value);
                ActivityId activity = ActivityId.From(value);
                Assert.That(route.StableText, Is.EqualTo(value));
                Assert.That(activity.StableText, Is.EqualTo(value));
                Assert.That(route, Is.EqualTo(RouteId.From(value)));
                Assert.That(route.GetHashCode(), Is.EqualTo(RouteId.From(value).GetHashCode()));
                Assert.That(activity, Is.EqualTo(ActivityId.From(value)));
                Assert.That(activity.GetHashCode(), Is.EqualTo(ActivityId.From(value).GetHashCode()));
            }

            foreach (string value in invalid)
            {
                Assert.That(RouteId.IsValidText(value), Is.False, "RouteId: " + value);
                Assert.That(ActivityId.IsValidText(value), Is.False, "ActivityId: " + value);
                Assert.Throws<ArgumentException>(() => RouteId.From(value));
                Assert.Throws<ArgumentException>(() => ActivityId.From(value));
            }
        }

        [Test]
        public void PlayerSlotIdentity_UsesItsOwnDomainAndAuthoredNormalization()
        {
            var slot = PlayerSlotId.From("  Custom Seat A  ");

            Assert.That(slot.IsValid, Is.True);
            Assert.That(slot.Value.Value, Is.EqualTo("Custom Seat A"));
            Assert.That(slot.Domain, Is.EqualTo(FrameworkIdentityDomain.PlayerSlot));
            Assert.That(slot, Is.EqualTo(PlayerSlotId.From("Custom Seat A")));
            Assert.That(slot.GetHashCode(), Is.EqualTo(PlayerSlotId.From("Custom Seat A").GetHashCode()));
            Assert.That(slot, Is.Not.EqualTo(PlayerSlotId.From("custom seat a")));
            Assert.That(slot.StableText, Is.EqualTo("PlayerSlot:Custom Seat A"));
            Assert.That(PlayerSlotId.Player1.Value.Value, Is.EqualTo("player.1"));
            Assert.That(PlayerSlotId.Player2.Value.Value, Is.EqualTo("player.2"));
            Assert.That(PlayerSlotId.Player3.Value.Value, Is.EqualTo("player.3"));
            Assert.That(PlayerSlotId.Player4.Value.Value, Is.EqualTo("player.4"));
        }

        [Test]
        public void FrameworkIdentityKey_UsesDomainAndValueForEqualityAndRejectsInvalidDomains()
        {
            FrameworkIdentityKey route = FrameworkIdentityKey.From(FrameworkIdentityDomain.Route, "shared.id");
            FrameworkIdentityKey sameRoute = FrameworkIdentityKey.From(FrameworkIdentityDomain.Route, "shared.id");
            FrameworkIdentityKey activity = FrameworkIdentityKey.From(FrameworkIdentityDomain.Activity, "shared.id");

            Assert.That(route, Is.EqualTo(sameRoute));
            Assert.That(route.GetHashCode(), Is.EqualTo(sameRoute.GetHashCode()));
            Assert.That(route, Is.Not.EqualTo(activity));
            Assert.That(new HashSet<FrameworkIdentityKey> { route, sameRoute, activity }.Count, Is.EqualTo(2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new FrameworkIdentityKey(FrameworkIdentityDomain.Unspecified, new FrameworkIdentityValue("valid")));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new FrameworkIdentityKey((FrameworkIdentityDomain)int.MaxValue, new FrameworkIdentityValue("valid")));
            Assert.Throws<ArgumentException>(() =>
                new FrameworkIdentityKey(FrameworkIdentityDomain.Route, default));
        }

        [Test]
        public void FrameworkIdentityKey_FromIdentityRevalidatesDomainAndValue()
        {
            Assert.Throws<ArgumentNullException>(() => FrameworkIdentityKey.From((IFrameworkIdentity)null));
            Assert.Throws<ArgumentException>(() => FrameworkIdentityKey.From(new TestIdentity(
                FrameworkIdentityDomain.Route,
                default)));
            Assert.Throws<ArgumentOutOfRangeException>(() => FrameworkIdentityKey.From(new TestIdentity(
                (FrameworkIdentityDomain)int.MaxValue,
                new FrameworkIdentityValue("value"))));

            RouteId route = RouteId.From("shared.id");
            ActivityId activity = ActivityId.From("shared.id");
            PlayerSlotId slot = PlayerSlotId.From("shared.id");
            FrameworkIdentityKey routeKey = FrameworkIdentityKey.From(route);
            FrameworkIdentityKey activityKey = FrameworkIdentityKey.From(activity);
            FrameworkIdentityKey slotKey = FrameworkIdentityKey.From(slot);

            Assert.That((routeKey.Domain, routeKey.Value.Value), Is.EqualTo((FrameworkIdentityDomain.Route, "shared.id")));
            Assert.That((activityKey.Domain, activityKey.Value.Value), Is.EqualTo((FrameworkIdentityDomain.Activity, "shared.id")));
            Assert.That((slotKey.Domain, slotKey.Value.Value), Is.EqualTo((FrameworkIdentityDomain.PlayerSlot, "shared.id")));
            Assert.That(new HashSet<FrameworkIdentityKey> { routeKey, activityKey, slotKey }.Count, Is.EqualTo(3));
        }

        [Test]
        public void IdentityStableText_IsNotAParseFormat()
        {
            FrameworkIdentityKey key = FrameworkIdentityKey.From(FrameworkIdentityDomain.Route, "main.route");
            Assert.That(key.StableText, Is.EqualTo("Route:main.route"));
            Assert.That(RouteId.From("main.route").StableText, Is.EqualTo("main.route"));
            Assert.That(HasParser(typeof(FrameworkIdentityKey)), Is.False);
            Assert.That(HasParser(typeof(RouteId)), Is.False);
            Assert.That(HasParser(typeof(ActivityId)), Is.False);
            Assert.That(HasParser(typeof(PlayerSlotId)), Is.False);
        }

        [Test]
        public void AuthoredReferencesAndIdentityText_SurviveAssetDatabaseSaveAndReload()
        {
            const string routeId = "chapter-1.start";
            const string activityId = "encounter.main";
            const string authoredSlotText = "  Custom Seat A  ";

            RouteAsset route = CreateAsset<RouteAsset>("Route.asset");
            ActivityAsset activity = CreateAsset<ActivityAsset>("Activity.asset");
            RouteAsset duplicateRoute = CreateAsset<RouteAsset>("DuplicateRoute.asset");
            ActivityAsset duplicateActivity = CreateAsset<ActivityAsset>("DuplicateActivity.asset");
            PlayerSlotProfile slot = CreateAsset<PlayerSlotProfile>("PlayerSlot.asset");
            PlayerSessionProfile playerSession = CreateAsset<PlayerSessionProfile>("PlayerSession.asset");
            GameApplicationAsset application = CreateAsset<GameApplicationAsset>("GameApplication.asset");
            CameraOutputDefinition cameraOutput = CreateAsset<CameraOutputDefinition>("CameraOutput.asset");
            SessionCameraAssignmentAsset cameraAssignment = CreateAsset<SessionCameraAssignmentAsset>("CameraAssignment.asset");

            SetString(route, "routeId", routeId);
            SetString(activity, "activityId", activityId);
            SetString(duplicateRoute, "routeId", routeId);
            SetString(duplicateActivity, "activityId", activityId);
            SetString(slot, "playerSlotId", authoredSlotText);
            SetString(cameraOutput, "stableId", Guid.NewGuid().ToString("N"));
            SetObjectReference(route, "startupActivity", activity);
            SetObjectReference(application, "startupRoute", route);
            SetObjectReference(application, "defaultPlayerSessionProfile", playerSession);
            SetBool(application, "playerSessionEnabled", true);
            SetString(application, "applicationName", "Identity Round Trip");
            SetObjectArrayElement(playerSession, "supportedSlots", 0, slot);
            ConfigureCameraAssignmentMapping(cameraAssignment, slot, cameraOutput);
            SaveAssets();

            RouteAsset loadedRoute = LoadAsset<RouteAsset>("Route.asset");
            ActivityAsset loadedActivity = LoadAsset<ActivityAsset>("Activity.asset");
            RouteAsset loadedDuplicateRoute = LoadAsset<RouteAsset>("DuplicateRoute.asset");
            ActivityAsset loadedDuplicateActivity = LoadAsset<ActivityAsset>("DuplicateActivity.asset");
            PlayerSlotProfile loadedSlot = LoadAsset<PlayerSlotProfile>("PlayerSlot.asset");
            PlayerSessionProfile loadedPlayerSession = LoadAsset<PlayerSessionProfile>("PlayerSession.asset");
            GameApplicationAsset loadedApplication = LoadAsset<GameApplicationAsset>("GameApplication.asset");
            CameraOutputDefinition loadedCameraOutput = LoadAsset<CameraOutputDefinition>("CameraOutput.asset");
            SessionCameraAssignmentAsset loadedCameraAssignment = LoadAsset<SessionCameraAssignmentAsset>("CameraAssignment.asset");

            Assert.That(loadedApplication.StartupRoute, Is.SameAs(loadedRoute));
            Assert.That(loadedApplication.DefaultPlayerSessionProfile, Is.SameAs(loadedPlayerSession));
            Assert.That(loadedRoute.StartupActivity, Is.SameAs(loadedActivity));
            Assert.That(loadedRoute.RouteId, Is.EqualTo(RouteId.From(routeId)));
            Assert.That(loadedActivity.ActivityId, Is.EqualTo(ActivityId.From(activityId)));
            Assert.That(loadedSlot.PlayerSlotId, Is.EqualTo(PlayerSlotId.From("Custom Seat A")));
            Assert.That(loadedSlot.PlayerSlotIdText, Is.EqualTo("Custom Seat A"));
            Assert.That(loadedPlayerSession.SupportedSlots[0], Is.SameAs(loadedSlot));
            Assert.That(loadedApplication.ApplicationName, Is.EqualTo("Identity Round Trip"));

            Assert.That(loadedRoute, Is.Not.SameAs(loadedDuplicateRoute));
            Assert.That(loadedRoute.HasSameStableId(loadedDuplicateRoute), Is.True);
            Assert.That(
                RuntimeDefinitionToken.FromUnityObject(loadedRoute),
                Is.Not.EqualTo(RuntimeDefinitionToken.FromUnityObject(loadedDuplicateRoute)));
            Assert.That(loadedActivity, Is.Not.SameAs(loadedDuplicateActivity));
            Assert.That(loadedActivity.HasSameStableId(loadedDuplicateActivity), Is.True);
            Assert.That(
                RuntimeDefinitionToken.FromUnityObject(loadedActivity),
                Is.Not.EqualTo(RuntimeDefinitionToken.FromUnityObject(loadedDuplicateActivity)));

            SerializedObject loadedAssignment = new SerializedObject(loadedCameraAssignment);
            SerializedProperty authoredMapping = loadedAssignment
                .FindProperty("individualMemberOutputMappings")
                .GetArrayElementAtIndex(0);
            Assert.That(
                authoredMapping.FindPropertyRelative("playerSlotProfile").objectReferenceValue,
                Is.SameAs(loadedSlot));
            Assert.That(
                authoredMapping.FindPropertyRelative("outputDefinition").objectReferenceValue,
                Is.SameAs(loadedCameraOutput));

            PlayerSlotId typedSlotId = loadedSlot.PlayerSlotId;
            CameraOutputId typedOutputId = loadedCameraOutput.OutputId;
            var playerOutputMapping = new CameraPlayerOutputMapping(typedSlotId, typedOutputId);
            Assert.That(playerOutputMapping.PlayerSlotId, Is.EqualTo(typedSlotId));
            Assert.That(playerOutputMapping.OutputId, Is.EqualTo(typedOutputId));
        }

        [Test]
        public void InvalidSerializedIds_FailTypedAccessWithoutSilentRegeneration()
        {
            RouteAsset route = CreateAsset<RouteAsset>("InvalidRoute.asset");
            ActivityAsset activity = CreateAsset<ActivityAsset>("InvalidActivity.asset");
            PlayerSlotProfile slot = CreateAsset<PlayerSlotProfile>("InvalidPlayerSlot.asset");
            SetString(route, "routeId", "Route with spaces");
            SetString(activity, "activityId", "Activity with spaces");
            SetString(slot, "playerSlotId", "  ");
            SaveAssets();

            RouteAsset loadedRoute = LoadAsset<RouteAsset>("InvalidRoute.asset");
            ActivityAsset loadedActivity = LoadAsset<ActivityAsset>("InvalidActivity.asset");
            PlayerSlotProfile loadedSlot = LoadAsset<PlayerSlotProfile>("InvalidPlayerSlot.asset");

            Assert.That(loadedRoute.HasValidRouteId, Is.False);
            Assert.Throws<InvalidOperationException>(() => _ = loadedRoute.RouteId);
            Assert.That(new SerializedObject(loadedRoute).FindProperty("routeId").stringValue, Is.EqualTo("Route with spaces"));
            Assert.That(loadedActivity.HasValidActivityId, Is.False);
            Assert.Throws<InvalidOperationException>(() => _ = loadedActivity.ActivityId);
            Assert.That(new SerializedObject(loadedActivity).FindProperty("activityId").stringValue, Is.EqualTo("Activity with spaces"));
            Assert.That(loadedSlot.TryGetPlayerSlotId(out PlayerSlotId invalidSlot, out _), Is.False);
            Assert.That(invalidSlot.IsValid, Is.False);
            Assert.That(new SerializedObject(loadedSlot).FindProperty("playerSlotId").stringValue, Is.EqualTo("  "));
        }

        [Test]
        public void SessionContentOwner_UsesExactHostEntityIdWhenApplicationNameChanges()
        {
            var hostObject = new GameObject("identity-session-owner-host");
            _transientObjects.Add(hostObject);
            FrameworkRuntimeHost host = hostObject.AddComponent<FrameworkRuntimeHost>();
            var application = ScriptableObject.CreateInstance<GameApplicationAsset>();
            _transientObjects.Add(application);
            SetString(application, "applicationName", "First Display Name");

            typeof(FrameworkRuntimeHost)
                .GetField("_runtimeContentRuntime", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(host, new RuntimeContentRuntime());
            MethodInfo createSessionScopeRoot = typeof(FrameworkRuntimeHost).GetMethod(
                "CreateSessionScopeRoot",
                BindingFlags.Instance | BindingFlags.NonPublic);

            RuntimeScopeLifecycleResult first = (RuntimeScopeLifecycleResult)createSessionScopeRoot.Invoke(
                host,
                new object[] { application, "identity-test", "first-name" });
            SetString(application, "applicationName", "Renamed Display Name");
            RuntimeScopeLifecycleResult renamed = (RuntimeScopeLifecycleResult)createSessionScopeRoot.Invoke(
                host,
                new object[] { application, "identity-test", "renamed" });

            Assert.That(first.Owner.OwnerIdentity.Domain, Is.EqualTo(FrameworkIdentityDomain.Session));
            Assert.That(first.Owner.OwnerIdentity.Value.Value, Is.EqualTo(host.GetEntityId().ToString()));
            Assert.That(renamed.Owner.OwnerIdentity, Is.EqualTo(first.Owner.OwnerIdentity));
            Assert.That(renamed.Owner, Is.EqualTo(first.Owner));
            Assert.That(first.Owner.OwnerName, Is.EqualTo("First Display Name"));
            Assert.That(renamed.Owner.OwnerName, Is.EqualTo("Renamed Display Name"));
        }

        private T CreateAsset<T>(string fileName) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            string path = _assetFolder + "/" + fileName;
            AssetDatabase.CreateAsset(asset, path);
            _assets.Add(asset);
            return asset;
        }

        private T LoadAsset<T>(string fileName) where T : ScriptableObject =>
            AssetDatabase.LoadAssetAtPath<T>(_assetFolder + "/" + fileName);

        private void SaveAssets()
        {
            foreach (UnityEngine.Object asset in _assets)
            {
                AssetDatabase.SaveAssetIfDirty(asset);
            }
        }

        private static void SetString(UnityEngine.Object target, string propertyName, string value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetBool(UnityEngine.Object target, string propertyName, bool value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetObjectReference(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetObjectArrayElement(
            UnityEngine.Object target,
            string propertyName,
            int index,
            UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty array = serialized.FindProperty(propertyName);
            array.arraySize = index + 1;
            array.GetArrayElementAtIndex(index).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void ConfigureCameraAssignmentMapping(
            SessionCameraAssignmentAsset assignment,
            PlayerSlotProfile slot,
            CameraOutputDefinition output)
        {
            var serialized = new SerializedObject(assignment);
            SerializedProperty outputs = serialized.FindProperty("outputDefinitions");
            outputs.arraySize = 1;
            outputs.GetArrayElementAtIndex(0).objectReferenceValue = output;

            SerializedProperty mappings = serialized.FindProperty("individualMemberOutputMappings");
            mappings.arraySize = 1;
            SerializedProperty mapping = mappings.GetArrayElementAtIndex(0);
            mapping.FindPropertyRelative("playerSlotProfile").objectReferenceValue = slot;
            mapping.FindPropertyRelative("outputDefinition").objectReferenceValue = output;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(assignment);
        }

        private static bool HasParser(Type type)
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (string.Equals(method.Name, "Parse", StringComparison.Ordinal) ||
                    string.Equals(method.Name, "TryParse", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class TestIdentity : IFrameworkIdentity
        {
            public TestIdentity(FrameworkIdentityDomain domain, FrameworkIdentityValue value)
            {
                Domain = domain;
                Value = value;
            }

            public FrameworkIdentityDomain Domain { get; }

            public FrameworkIdentityValue Value { get; }
        }
    }
}
