using System;
using NUnit.Framework;
using UnityEngine;

namespace Immersive.Framework.Pause.Tests
{
    public sealed class PauseActivityBindingAuthoringTests
    {
        private readonly System.Collections.Generic.List<GameObject> created = new();

        [TearDown]
        public void TearDown()
        {
            for (int index = created.Count - 1; index >= 0; index--)
            {
                if (created[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(created[index]);
                }
            }

            created.Clear();
        }

        [Test]
        public void ValidAuthoring_CreatesRequiredIntent()
        {
            PauseActivityBindingAuthoring authoring = CreateAuthoring("Valid");

            bool createdIntent = authoring.TryCreateIntent(
                out PauseActivityBindingIntent intent,
                out string diagnostic);

            Assert.That(createdIntent, Is.True, diagnostic);
            Assert.That(intent.IsValid, Is.True);
            Assert.That(intent.Requiredness, Is.EqualTo(PauseActivityBindingRequiredness.Required));
            Assert.That(diagnostic, Does.Contain("intent-created"));
        }

        [Test]
        public void InvalidAuthoring_ReturnsActionableDiagnostic()
        {
            PauseActivityBindingAuthoring authoring = CreateAuthoring("Invalid");
            JsonUtility.FromJsonOverwrite("{\"requiredness\":0}", authoring);

            bool createdIntent = authoring.TryCreateIntent(
                out PauseActivityBindingIntent intent,
                out string diagnostic);

            Assert.That(createdIntent, Is.False);
            Assert.That(intent.IsValid, Is.False);
            Assert.That(diagnostic, Does.Contain("invalid-authoring"));
            Assert.That(diagnostic, Does.Contain("Requiredness"));
        }

        [Test]
        public void ResolveDeclarations_ZeroIsValidIntentAbsence()
        {
            PauseActivityBindingIntentResolution result =
                PauseActivityBindingAuthoringValidator.ResolveDeclarations(
                    Array.Empty<PauseActivityBindingAuthoring>(),
                    "test.zero");

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.IsAbsent, Is.True);
            Assert.That(result.HasIntent, Is.False);
            Assert.That(result.Diagnostic, Does.Contain("intent-absent"));
        }

        [Test]
        public void ResolveFromRoots_OneInactiveDeclarationIsIncludedAndAccepted()
        {
            GameObject root = CreateObject("Inactive Root");
            PauseActivityBindingAuthoring authoring = root.AddComponent<PauseActivityBindingAuthoring>();
            root.SetActive(false);

            PauseActivityBindingIntentResolution result =
                PauseActivityBindingAuthoringValidator.ResolveFromRoots(
                    new[] { root },
                    "test.inactive");

            Assert.That(authoring, Is.Not.Null);
            Assert.That(result.Succeeded, Is.True, result.Diagnostic);
            Assert.That(result.HasIntent, Is.True);
            Assert.That(result.DeclarationCount, Is.EqualTo(1));
        }

        [Test]
        public void ResolveFromRoots_DuplicatesFailIndependentlyOfRootOrder()
        {
            GameObject first = CreateObject("First");
            first.AddComponent<PauseActivityBindingAuthoring>();
            GameObject second = CreateObject("Second");
            second.AddComponent<PauseActivityBindingAuthoring>();

            PauseActivityBindingIntentResolution forward =
                PauseActivityBindingAuthoringValidator.ResolveFromRoots(
                    new[] { first, second },
                    "test.duplicate");
            PauseActivityBindingIntentResolution reverse =
                PauseActivityBindingAuthoringValidator.ResolveFromRoots(
                    new[] { second, first },
                    "test.duplicate");

            Assert.That(forward.Status,
                Is.EqualTo(PauseActivityBindingIntentStatus.UnsupportedMultipleDeclarations));
            Assert.That(reverse.Status, Is.EqualTo(forward.Status));
            Assert.That(forward.DeclarationCount, Is.EqualTo(2));
            Assert.That(reverse.DeclarationCount, Is.EqualTo(2));
            Assert.That(forward.Diagnostic, Does.Contain("duplicate-authoring"));
            Assert.That(forward.Diagnostic, Does.Contain("unsupported-multiple-declarations"));
        }

        [Test]
        public void ResolvingIntent_DoesNotMaterializePausePlayerBindingOnAuthoringObject()
        {
            GameObject root = CreateObject("No Materialization");
            int before = root.GetComponents<PausePlayerInputBinding>().Length;
            PauseActivityBindingAuthoring authoring =
                root.AddComponent<PauseActivityBindingAuthoring>();

            PauseActivityBindingIntentResolution result =
                PauseActivityBindingAuthoringValidator.ResolveDeclarations(
                    new[] { authoring },
                    "test.no-materialization");
            int after = root.GetComponents<PausePlayerInputBinding>().Length;

            Assert.That(result.HasIntent, Is.True, result.Diagnostic);
            Assert.That(after, Is.EqualTo(before));
        }

        private PauseActivityBindingAuthoring CreateAuthoring(string name)
        {
            return CreateObject(name).AddComponent<PauseActivityBindingAuthoring>();
        }

        private GameObject CreateObject(string name)
        {
            var value = new GameObject(name);
            created.Add(value);
            return value;
        }
    }
}
