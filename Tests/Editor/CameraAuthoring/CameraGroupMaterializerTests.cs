using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Editor.CameraAuthoring;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.Authoring.Editor.Tests
{
    public sealed class CameraGroupMaterializerTests
    {
        private const BindingFlags PrivateInstance =
            BindingFlags.Instance | BindingFlags.NonPublic;

        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = _created.Count - 1; index >= 0; index--)
                if (_created[index] != null) Object.DestroyImmediate(_created[index]);
            _created.Clear();
        }

        [Test]
        public void ApplyOrRebuild_EnablesAndProjectsGroupFramingBehavior()
        {
            var behavior = ScriptableObject.CreateInstance<GroupCameraRigBehaviorDefinition>();
            _created.Add(behavior);
            SetField(behavior, "framingSize", 0.63f);
            SetField(behavior, "damping", 4.25f);
            SetField(behavior, "fovRange", new Vector2(18f, 82f));
            SetField(behavior, "dollyRange", new Vector2(-9f, 37f));
            SetField(behavior, "orthoSizeRange", new Vector2(2.5f, 43f));

            var rigRoot = new GameObject("Group Camera Rig");
            rigRoot.SetActive(false);
            _created.Add(rigRoot);
            CameraRigComposer composer = rigRoot.AddComponent<CameraRigComposer>();
            SetField(composer, "behaviorDefinition", behavior);

            CameraRigComposerApplyRebuildResult firstApply =
                CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false);

            Assert.That(firstApply.Succeeded, Is.True, firstApply.BlockingIssue);
            CinemachineGroupFraming framing = composer.FrameworkOwnedGroupFraming;
            Assert.That(framing, Is.Not.Null);
            Assert.That(framing.enabled, Is.True);
            Assert.That(framing.FramingSize, Is.EqualTo(behavior.FramingSize));
            Assert.That(framing.Damping, Is.EqualTo(behavior.Damping));
            Assert.That(framing.FovRange, Is.EqualTo(behavior.FovRange));
            Assert.That(framing.DollyRange, Is.EqualTo(behavior.DollyRange));
            Assert.That(framing.OrthoSizeRange, Is.EqualTo(behavior.OrthoSizeRange));

            framing.enabled = false;
            framing.FramingSize = 1.4f;
            framing.Damping = 0.25f;
            framing.FovRange = Vector2.one;
            framing.DollyRange = Vector2.zero;
            framing.OrthoSizeRange = Vector2.one;

            CameraRigComposerApplyRebuildResult rebuild =
                CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false);

            Assert.That(rebuild.Succeeded, Is.True, rebuild.BlockingIssue);
            Assert.That(composer.FrameworkOwnedGroupFraming, Is.SameAs(framing));
            Assert.That(framing.enabled, Is.True);
            Assert.That(framing.FramingSize, Is.EqualTo(behavior.FramingSize));
            Assert.That(framing.Damping, Is.EqualTo(behavior.Damping));
            Assert.That(framing.FovRange, Is.EqualTo(behavior.FovRange));
            Assert.That(framing.DollyRange, Is.EqualTo(behavior.DollyRange));
            Assert.That(framing.OrthoSizeRange, Is.EqualTo(behavior.OrthoSizeRange));
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, PrivateInstance).SetValue(target, value);
        }
    }
}
