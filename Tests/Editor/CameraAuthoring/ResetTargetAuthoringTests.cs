using Immersive.Framework.Editor.Authoring;
using Immersive.Framework.Authoring;
using Immersive.Framework.Reset;
using Immersive.Framework.Reset.Unity;
using Immersive.Framework.RuntimeContent;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Authoring.Editor.Tests
{
    public sealed class ResetTargetAuthoringTests
    {
        private GameObject _triggerObject;
        private GameObject _resettableObject;
        private GameObject _compositionObject;
        private RouteAsset _routeAsset;
        private ActivityAsset _activityAsset;

        [TearDown]
        public void TearDown()
        {
            if (_triggerObject != null) Object.DestroyImmediate(_triggerObject);
            if (_resettableObject != null) Object.DestroyImmediate(_resettableObject);
            if (_compositionObject != null) Object.DestroyImmediate(_compositionObject);
            if (_routeAsset != null) Object.DestroyImmediate(_routeAsset);
            if (_activityAsset != null) Object.DestroyImmediate(_activityAsset);
            _routeAsset = null;
            _activityAsset = null;
        }

        [Test]
        public void ChangingReferenceMode_ClearsInactiveDirectOrStablePayload()
        {
            ResetRequestTrigger trigger = CreateTrigger();
            Resettable resettable = CreateResettable();
            var serialized = new SerializedObject(trigger);
            SerializedProperty target = serialized.FindProperty("target");
            SerializedProperty objectTarget = target.FindPropertyRelative("objectTarget");
            SerializedProperty stable = objectTarget.FindPropertyRelative("stableReference");
            objectTarget.FindPropertyRelative("directResettable").objectReferenceValue = resettable;
            Assert.That(objectTarget.FindPropertyRelative("directResettable").objectReferenceValue,
                Is.SameAs(resettable), "Direct mode must start with its Resettable payload.");
            SetStablePayload(stable, includeActivityReference: false);
            objectTarget.FindPropertyRelative("referenceMode").intValue = (int)ResetReferenceMode.Stable;

            ResetTargetDrawerFields.ClearInactiveReferencePayload(
                objectTarget, "directResettable", ResetReferenceMode.Stable);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            serialized.Update();
            target = serialized.FindProperty("target");
            objectTarget = target.FindPropertyRelative("objectTarget");
            stable = objectTarget.FindPropertyRelative("stableReference");

            Assert.IsNull(objectTarget.FindPropertyRelative("directResettable").objectReferenceValue);
            Assert.AreEqual("qa.reset.authoring", stable.FindPropertyRelative("objectEntryIdText").stringValue);
            Assert.AreEqual((int)StableObjectOwnerSelectorKind.Route, stable.FindPropertyRelative("ownerSelectorKind").intValue);
            Assert.That(stable.FindPropertyRelative("routeOwner").objectReferenceValue, Is.Not.Null,
                "Stable mode must retain its active Route owner payload.");

            objectTarget.FindPropertyRelative("directResettable").objectReferenceValue = resettable;
            SetStablePayload(stable, includeActivityReference: true);
            objectTarget.FindPropertyRelative("referenceMode").intValue = (int)ResetReferenceMode.Direct;
            ResetTargetDrawerFields.ClearInactiveReferencePayload(
                objectTarget, "directResettable", ResetReferenceMode.Direct);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            serialized.Update();
            objectTarget = serialized.FindProperty("target").FindPropertyRelative("objectTarget");
            stable = objectTarget.FindPropertyRelative("stableReference");

            Assert.That(objectTarget.FindPropertyRelative("directResettable").objectReferenceValue,
                Is.SameAs(resettable), "Returning to Direct mode must preserve its active Resettable payload.");
            AssertStablePayloadCleared(stable);
        }

        [Test]
        public void ChangingCompositionReferenceMode_ClearsInactiveDirectOrStablePayload()
        {
            ResetRequestTrigger trigger = CreateTrigger();
            _compositionObject = new GameObject("ResetComposition");
            ResetComposition composition = _compositionObject.AddComponent<ResetComposition>();
            var serialized = new SerializedObject(trigger);
            SerializedProperty compositionTarget = serialized.FindProperty("target")
                .FindPropertyRelative("compositionTarget");
            SerializedProperty stable = compositionTarget.FindPropertyRelative("stableReference");
            compositionTarget.FindPropertyRelative("directComposition").objectReferenceValue = composition;
            Assert.That(compositionTarget.FindPropertyRelative("directComposition").objectReferenceValue,
                Is.SameAs(composition), "Direct mode must start with its ResetComposition payload.");
            SetStablePayload(stable, includeActivityReference: false);
            compositionTarget.FindPropertyRelative("referenceMode").intValue = (int)ResetReferenceMode.Stable;

            ResetTargetDrawerFields.ClearInactiveReferencePayload(
                compositionTarget, "directComposition", ResetReferenceMode.Stable);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            serialized.Update();
            compositionTarget = serialized.FindProperty("target").FindPropertyRelative("compositionTarget");
            stable = compositionTarget.FindPropertyRelative("stableReference");

            Assert.IsNull(compositionTarget.FindPropertyRelative("directComposition").objectReferenceValue);
            Assert.AreEqual("qa.reset.authoring", stable.FindPropertyRelative("objectEntryIdText").stringValue);
            Assert.That(stable.FindPropertyRelative("routeOwner").objectReferenceValue, Is.Not.Null,
                "Stable mode must retain its active Route owner payload.");

            compositionTarget.FindPropertyRelative("directComposition").objectReferenceValue = composition;
            SetStablePayload(stable, includeActivityReference: true);
            compositionTarget.FindPropertyRelative("referenceMode").intValue = (int)ResetReferenceMode.Direct;
            ResetTargetDrawerFields.ClearInactiveReferencePayload(
                compositionTarget, "directComposition", ResetReferenceMode.Direct);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            serialized.Update();
            compositionTarget = serialized.FindProperty("target").FindPropertyRelative("compositionTarget");
            stable = compositionTarget.FindPropertyRelative("stableReference");

            Assert.That(compositionTarget.FindPropertyRelative("directComposition").objectReferenceValue,
                Is.SameAs(composition), "Returning to Direct mode must preserve its active ResetComposition payload.");
            AssertStablePayloadCleared(stable);
        }

        [Test]
        public void ChangingSemanticKindToCurrentActivity_ClearsObjectPayload()
        {
            ResetRequestTrigger trigger = CreateTrigger();
            Resettable resettable = CreateResettable();
            var serialized = new SerializedObject(trigger);
            SerializedProperty target = serialized.FindProperty("target");
            SerializedProperty objectTarget = target.FindPropertyRelative("objectTarget");
            objectTarget.FindPropertyRelative("directResettable").objectReferenceValue = resettable;
            objectTarget.FindPropertyRelative("referenceMode").intValue = (int)ResetReferenceMode.Direct;

            ResetTargetPropertyDrawer.ClearInactiveSemanticTarget(
                target, ResetTargetKind.CurrentActivity);
            target.FindPropertyRelative("kind").intValue = (int)ResetTargetKind.CurrentActivity;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            serialized.Update();
            objectTarget = serialized.FindProperty("target").FindPropertyRelative("objectTarget");

            Assert.IsNull(objectTarget.FindPropertyRelative("directResettable").objectReferenceValue);
            Assert.AreEqual(string.Empty, objectTarget.FindPropertyRelative("stableReference")
                .FindPropertyRelative("objectEntryIdText").stringValue);
        }

        private ResetRequestTrigger CreateTrigger()
        {
            _triggerObject = new GameObject("ResetRequestTrigger");
            return _triggerObject.AddComponent<ResetRequestTrigger>();
        }

        private Resettable CreateResettable()
        {
            _resettableObject = new GameObject("Resettable");
            return _resettableObject.AddComponent<Resettable>();
        }

        private void SetStablePayload(SerializedProperty stable, bool includeActivityReference)
        {
            _routeAsset ??= ScriptableObject.CreateInstance<RouteAsset>();
            stable.FindPropertyRelative("objectEntryIdText").stringValue = "qa.reset.authoring";
            stable.FindPropertyRelative("ownerSelectorKind").intValue = (int)StableObjectOwnerSelectorKind.Route;
            stable.FindPropertyRelative("routeOwner").objectReferenceValue = _routeAsset;
            stable.FindPropertyRelative("activityOwner").objectReferenceValue = null;
            if (includeActivityReference)
            {
                _activityAsset ??= ScriptableObject.CreateInstance<ActivityAsset>();
                stable.FindPropertyRelative("activityOwner").objectReferenceValue = _activityAsset;
            }
        }

        private static void AssertStablePayloadCleared(SerializedProperty stable)
        {
            Assert.AreEqual(string.Empty, stable.FindPropertyRelative("objectEntryIdText").stringValue);
            Assert.AreEqual((int)StableObjectOwnerSelectorKind.Unspecified,
                stable.FindPropertyRelative("ownerSelectorKind").intValue);
            Assert.IsNull(stable.FindPropertyRelative("routeOwner").objectReferenceValue);
            Assert.IsNull(stable.FindPropertyRelative("activityOwner").objectReferenceValue);
        }
    }
}
