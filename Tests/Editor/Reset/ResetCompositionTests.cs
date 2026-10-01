using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.Reset;
using Immersive.Framework.Reset.Unity;
using Immersive.Framework.RuntimeContent;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Reset.Tests
{
    public sealed class ResetCompositionTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();
        private ResetRegistry _registry;
        private ResettableOwnerRegistrationRuntime _registration;
        private RuntimeContentOwner _route;

        [SetUp]
        public void SetUp()
        {
            _registry = new ResetRegistry();
            _registration = new ResettableOwnerRegistrationRuntime(_registry);
            _route = RuntimeContentOwner.Route(
                "reset-035-d.route",
                "Route",
                RuntimeDefinitionToken.MintAnonymous());
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
        public void Descendants_CollectsResettableAndStopsAtNestedComposition()
        {
            GameObject root = CreateObject("Root");
            ResetComposition outer = root.AddComponent<ResetComposition>();
            Resettable directMember = AddResettable(root.transform, "DirectMember");
            GameObject nestedCompositionObject = CreateChild(root.transform, "NestedBoundary");
            ResetComposition nested = nestedCompositionObject.AddComponent<ResetComposition>();
            Resettable nestedMember = AddResettable(nestedCompositionObject.transform, "NestedMember");

            ResetCompositionResolver.Resolution outerResult = ResetCompositionResolver.Resolve(outer);
            ResetCompositionResolver.Resolution nestedResult = ResetCompositionResolver.Resolve(nested);

            Assert.IsTrue(outerResult.Succeeded, outerResult.Diagnostic);
            CollectionAssert.AreEqual(new[] { directMember }, outerResult.Members);
            Assert.IsTrue(nestedResult.Succeeded, nestedResult.Diagnostic);
            CollectionAssert.AreEqual(new[] { nestedMember }, nestedResult.Members);
        }

        [Test]
        public void NestedResettableMembers_AreDistinctAndRegisterCapabilitiesOnce()
        {
            GameObject root = CreateObject("Root");
            ResetComposition composition = root.AddComponent<ResetComposition>();
            Resettable parent = AddResettable(root.transform, "Parent");
            AddCapability(parent.gameObject);
            GameObject childObject = CreateChild(parent.transform, "Child");
            Resettable child = childObject.AddComponent<Resettable>();
            AddCapability(childObject);

            ResetCompositionResolver.Resolution result = ResetCompositionResolver.Resolve(composition);
            Assert.IsTrue(result.Succeeded, result.Diagnostic);
            CollectionAssert.AreEqual(new[] { parent, child }, result.Members);

            Assert.IsTrue(Register(root, out string diagnostic), diagnostic);
            Assert.AreEqual(2, _registry.SubjectCount);
            Assert.AreEqual(2, _registry.ParticipantCount);
            Assert.AreEqual(1, parent.RegisteredCapabilityCount);
            Assert.AreEqual(1, child.RegisteredCapabilityCount);
        }

        [Test]
        public void ExplicitMembers_ResolvesOnlyTypedReferencesListed()
        {
            GameObject root = CreateObject("Root");
            ResetComposition composition = root.AddComponent<ResetComposition>();
            Resettable listed = AddResettable(root.transform, "Listed");
            Resettable unlisted = AddResettable(root.transform, "Unlisted");
            SetMemberMode(composition, ResetCompositionMemberMode.ExplicitMembers);
            SetExplicitMembers(composition, listed);

            ResetCompositionResolver.Resolution result = ResetCompositionResolver.Resolve(composition);

            Assert.IsTrue(result.Succeeded, result.Diagnostic);
            CollectionAssert.AreEqual(new[] { listed }, result.Members);
            CollectionAssert.DoesNotContain(result.Members, unlisted);
        }

        [Test]
        public void ExplicitMembers_DeduplicatesReferencesInAuthoringOrder()
        {
            GameObject root = CreateObject("Root");
            ResetComposition composition = root.AddComponent<ResetComposition>();
            Resettable first = AddResettable(root.transform, "First");
            Resettable second = AddResettable(root.transform, "Second");
            SetMemberMode(composition, ResetCompositionMemberMode.ExplicitMembers);
            SetExplicitMembers(composition, second, null, first, second, first);

            ResetCompositionResolver.Resolution result = ResetCompositionResolver.Resolve(composition);

            Assert.IsTrue(result.Succeeded, result.Diagnostic);
            CollectionAssert.AreEqual(new[] { second, first }, result.Members);
            Assert.IsTrue(result.Diagnostics.Any(item => item.Contains("reset-composition-member-duplicate")));
            Assert.IsTrue(result.Diagnostics.Any(item => item.Contains("reset-composition-explicit-member-null")));
        }

        [Test]
        public void Descendants_OrderIsDeterministicByHierarchyTraversal()
        {
            GameObject root = CreateObject("Root");
            ResetComposition composition = root.AddComponent<ResetComposition>();
            Resettable first = AddResettable(root.transform, "First");
            GameObject branch = CreateChild(root.transform, "Branch");
            Resettable second = AddResettable(branch.transform, "Second");
            Resettable third = AddResettable(root.transform, "Third");

            Resettable[] firstResolution = ResetCompositionResolver.Resolve(composition).Members.ToArray();
            Resettable[] secondResolution = ResetCompositionResolver.Resolve(composition).Members.ToArray();

            CollectionAssert.AreEqual(new[] { first, second, third }, firstResolution);
            CollectionAssert.AreEqual(firstResolution, secondResolution);
        }

        [Test]
        public void CompositionMembership_OverridesDefaultButLocalExplicitMembershipWins()
        {
            GameObject root = CreateObject("Root");
            ResetComposition composition = root.AddComponent<ResetComposition>();
            Resettable followsComposition = AddResettable(root.transform, "FollowsComposition");
            Resettable localOverride = AddResettable(root.transform, "LocalOverride");
            SetMembership(composition, ResetMembership.Activity);
            SetMembership(localOverride, ResetMembership.Route);

            Assert.AreEqual(ResetMembership.Activity, composition.Membership);
            Assert.IsTrue(Register(root, out string diagnostic), diagnostic);

            Assert.AreEqual(ResetMembership.Activity, followsComposition.Subject.EffectiveMembership);
            Assert.AreEqual(ResetMembership.Route, localOverride.Subject.EffectiveMembership);
            Assert.AreEqual(_route, followsComposition.Owner);
            Assert.AreEqual(_route, localOverride.Owner);
        }

        [Test]
        public void CompositionMembership_DoesNotChangeResettableOwnership()
        {
            GameObject root = CreateObject("Root");
            ResetComposition composition = root.AddComponent<ResetComposition>();
            Resettable resettable = AddResettable(root.transform, "Member");
            SetMembership(composition, ResetMembership.Activity);

            Assert.IsTrue(Register(root, out string diagnostic), diagnostic);

            Assert.AreEqual(_route, resettable.Owner);
            Assert.AreEqual(_route, resettable.Subject.Owner);
            Assert.AreEqual(ResetSubjectScope.Route, resettable.Subject.Scope);
        }

        [Test]
        public void TwoCompositionsReferencingOneResettable_RegisterItOnlyOnce()
        {
            GameObject root = CreateObject("Root");
            ResetComposition firstComposition = root.AddComponent<ResetComposition>();
            GameObject secondCompositionObject = CreateChild(root.transform, "SecondComposition");
            ResetComposition secondComposition = secondCompositionObject.AddComponent<ResetComposition>();
            Resettable shared = AddResettable(root.transform, "Shared");
            SetMemberMode(firstComposition, ResetCompositionMemberMode.ExplicitMembers);
            SetMemberMode(secondComposition, ResetCompositionMemberMode.ExplicitMembers);
            SetExplicitMembers(firstComposition, shared);
            SetExplicitMembers(secondComposition, shared);

            Assert.IsTrue(Register(root, out string diagnostic), diagnostic);

            Assert.AreEqual(1, _registry.SubjectCount);
            Assert.AreEqual(1, _registration.GetRegisteredResettableCount(_route));
            Assert.AreEqual(shared.RuntimeSubjectId, _registry.SnapshotSubjects().Single().SubjectId);
        }

        [Test]
        public void EmptyComposition_SucceedsWithDiagnostic()
        {
            GameObject root = CreateObject("Root");
            ResetComposition composition = root.AddComponent<ResetComposition>();

            ResetCompositionResolver.Resolution result = ResetCompositionResolver.Resolve(composition);

            Assert.IsTrue(result.Succeeded, result.Diagnostic);
            Assert.IsEmpty(result.Members);
            Assert.IsTrue(result.Diagnostics.Any(item => item.Contains("reset-composition-empty")));
            Assert.AreEqual(ResetMembership.FollowOwner, composition.Membership);
            Assert.IsTrue(Register(root, out string registrationDiagnostic), registrationDiagnostic);
            StringAssert.Contains("reset-composition-empty", registrationDiagnostic);
        }

        [Test]
        public void ConflictingCompositionMembership_IsRejectedBeforeRegistration()
        {
            GameObject root = CreateObject("Root");
            ResetComposition first = root.AddComponent<ResetComposition>();
            GameObject secondObject = CreateChild(root.transform, "SecondComposition");
            ResetComposition second = secondObject.AddComponent<ResetComposition>();
            Resettable shared = AddResettable(root.transform, "Shared");
            SetMemberMode(first, ResetCompositionMemberMode.ExplicitMembers);
            SetMemberMode(second, ResetCompositionMemberMode.ExplicitMembers);
            SetMembership(first, ResetMembership.Activity);
            SetMembership(second, ResetMembership.Route);
            SetExplicitMembers(first, shared);
            SetExplicitMembers(second, shared);

            Assert.IsFalse(Register(root, out string diagnostic));

            StringAssert.Contains("reset-composition-membership-conflict", diagnostic);
            Assert.AreEqual(0, _registry.SubjectCount);
            Assert.IsFalse(shared.IsRegistered);
        }

        private GameObject CreateObject(string objectName)
        {
            var gameObject = new GameObject(objectName);
            _created.Add(gameObject);
            return gameObject;
        }

        private static GameObject CreateChild(Transform parent, string objectName)
        {
            var gameObject = new GameObject(objectName);
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static Resettable AddResettable(Transform parent, string objectName)
        {
            var gameObject = new GameObject(objectName);
            gameObject.transform.SetParent(parent, false);
            return gameObject.AddComponent<Resettable>();
        }

        private static void AddCapability(GameObject gameObject)
        {
            UnityTransformResetParticipant participant = gameObject.AddComponent<UnityTransformResetParticipant>();
            participant.CaptureBaseline();
        }

        private bool Register(GameObject root, out string diagnostic)
        {
            return _registration.TryRegisterOwnerContent(_route, new[] { root }, "test", "reset-035-d", out diagnostic);
        }

        private static void SetMemberMode(ResetComposition composition, ResetCompositionMemberMode memberMode)
        {
            SetEnum(composition, "memberMode", (int)memberMode);
        }

        private static void SetMembership(ResetComposition composition, ResetMembership membership)
        {
            SetEnum(composition, "membership", (int)membership);
        }

        private static void SetMembership(Resettable resettable, ResetMembership membership)
        {
            SetEnum(resettable, "membership", (int)membership);
        }

        private static void SetEnum(Object target, string propertyName, int value)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            Assert.IsNotNull(property, $"Expected serialized property '{propertyName}'.");
            property.enumValueIndex = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetExplicitMembers(ResetComposition composition, params Resettable[] members)
        {
            var serializedObject = new SerializedObject(composition);
            SerializedProperty property = serializedObject.FindProperty("explicitMembers");
            Assert.IsNotNull(property, "ResetComposition must expose serialized explicit members.");
            property.arraySize = members.Length;
            for (int index = 0; index < members.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = members[index];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
