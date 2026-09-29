using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Editor.CameraAuthoring;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.Authoring.Editor.Tests
{
    public sealed class CameraOutputAuthoringValidationTests
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
        public void ValidateChecksOnlyTheSelectedOutputAuthoring()
        {
            var definition = ScriptableObject.CreateInstance<CameraOutputDefinition>();
            _created.Add(definition);
            SetField(definition, "stableId", "11111111111111111111111111111111");

            var outputObject = new GameObject("Camera Output");
            outputObject.SetActive(false);
            _created.Add(outputObject);
            UnityEngine.Camera unityCamera = outputObject.AddComponent<UnityEngine.Camera>();
            CinemachineBrain brain = outputObject.AddComponent<CinemachineBrain>();
            CameraRigComposer fallback = outputObject.AddComponent<CameraRigComposer>();
            CameraOutputAuthoring authoring = outputObject.AddComponent<CameraOutputAuthoring>();
            SetField(authoring, "outputDefinition", definition);
            SetField(authoring, "unityCamera", unityCamera);
            SetField(authoring, "cinemachineBrain", brain);
            SetField(authoring, "fallbackCameraRig", fallback);
            SetField(authoring, "initializeOnAwake", false);
            outputObject.SetActive(true);

            CameraOutputAuthoringValidationResult result =
                CameraOutputAuthoringValidator.Validate(authoring);

            Assert.That(result.IsValid, Is.True,
                string.Join("\n", result.BlockingIssues));
            Assert.That(result.BlockingIssues, Is.Empty);
        }

        [Test]
        public void SessionValidationRequiresExplicitOutputPrefabs()
        {
            var configuration = new CameraSessionConfiguration();

            Assert.That(configuration.TryValidate(out string issue), Is.False);
            Assert.That(issue, Does.Contain("explicit Camera Output prefab"));
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, PrivateInstance).SetValue(target, value);
        }
    }
}
