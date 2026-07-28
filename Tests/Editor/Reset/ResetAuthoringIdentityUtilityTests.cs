using Immersive.Framework.Editor.Reset;
using Immersive.Framework.Reset.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Reset.Editor.Tests
{
    public sealed class ResetAuthoringIdentityUtilityTests
    {
        private GameObject created;

        [TearDown]
        public void TearDown()
        {
            if (created != null) Object.DestroyImmediate(created);
        }

        [Test]
        public void GenerateMissingSubjectId_FillsOnceAndPreservesExistingValue()
        {
            UnityResetSubjectAdapter adapter = CreateSubject();
            var serialized = new SerializedObject(adapter);
            SerializedProperty mode = serialized.FindProperty("idGeneration");
            SerializedProperty id = serialized.FindProperty("subjectId");

            Assert.That(ResetAuthoringIdentityUtility.GenerateMissingSubjectId(mode, id), Is.True);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            string generated = id.stringValue;

            Assert.That(generated, Does.StartWith("reset.subject."));
            Assert.That(ResetAuthoringIdentityUtility.GenerateMissingSubjectId(mode, id), Is.False);
            Assert.That(id.stringValue, Is.EqualTo(generated));
        }

        [Test]
        public void GenerateMissingParticipantId_GeneratesDistinctStableValues()
        {
            UnityResetSubjectAdapter adapter = CreateSubject();
            UnityTransformResetParticipant first = adapter.gameObject.AddComponent<UnityTransformResetParticipant>();
            UnityTransformResetParticipant second = adapter.gameObject.AddComponent<UnityTransformResetParticipant>();
            var firstSerialized = new SerializedObject(first);
            var secondSerialized = new SerializedObject(second);
            SerializedProperty firstId = firstSerialized.FindProperty("participantId");
            SerializedProperty secondId = secondSerialized.FindProperty("participantId");
            firstId.stringValue = string.Empty;
            secondId.stringValue = string.Empty;

            Assert.That(ResetAuthoringIdentityUtility.GenerateMissingParticipantId(firstId), Is.True);
            Assert.That(ResetAuthoringIdentityUtility.GenerateMissingParticipantId(secondId), Is.True);
            Assert.That(firstId.stringValue, Does.StartWith("reset.participant."));
            Assert.That(secondId.stringValue, Is.Not.EqualTo(firstId.stringValue));
        }

        private UnityResetSubjectAdapter CreateSubject()
        {
            created = new GameObject("Reset Subject Test");
            return created.AddComponent<UnityResetSubjectAdapter>();
        }
    }
}
