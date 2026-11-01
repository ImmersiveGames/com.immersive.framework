using System.Collections;
using System.Collections.Generic;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.Identity;
using Immersive.Framework.ObjectEntry;
using Immersive.Framework.RuntimeContent;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Immersive.Framework.Reset.Tests
{
    public sealed class StableObjectBindingHostTeardownTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private StableObjectBindingRegistry _bindings;
        private RuntimeContentOwner _owner;
        private bool _originalEnterPlayModeOptionsEnabled;
        private EnterPlayModeOptions _originalEnterPlayModeOptions;
        private int _originalPlayModeStartup;
        private bool _changedPlayModeStartup;

        [SetUp]
        public void SetUp()
        {
            _originalEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            _originalEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions =
                EnterPlayModeOptions.DisableDomainReload |
                EnterPlayModeOptions.DisableSceneReload;

            ImmersiveFrameworkSettingsAsset settings =
                Resources.Load<ImmersiveFrameworkSettingsAsset>(
                    ImmersiveFrameworkSettingsAsset.ResourcesPath);
            if (settings == null)
            {
                return;
            }

            var serializedSettings = new SerializedObject(settings);
            SerializedProperty playModeStartup =
                serializedSettings.FindProperty("editorPlayModeStartup");
            _originalPlayModeStartup = playModeStartup.enumValueIndex;
            playModeStartup.enumValueIndex =
                (int)FrameworkEditorPlayModeStartup.CurrentSceneOnly;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            _changedPlayModeStartup = true;
        }

        [UnityTest]
        public IEnumerator OccurrenceQuery_FrameworkHostDestroyUnbindsPreviouslyHandedOffInterface()
        {
            yield return new EnterPlayMode(expectDomainReload: false);
            Assert.IsTrue(EditorApplication.isPlaying, "Host teardown must run in Unity Play Mode.");

            const string id = "qa.stable.public-query-host-stop";
            GameObject target = Create("query-target");
            ObjectEntryDeclaration declaration = target.AddComponent<ObjectEntryDeclaration>();
            declaration.ConfigureForQa(
                ObjectEntryId.From(id),
                ObjectEntryRequiredness.Required);

            GameObject queryRoot = Create("query-anchor");
            ObjectEntryOccurrenceQueryBinding anchor = queryRoot.AddComponent<ObjectEntryOccurrenceQueryBinding>();
            GameObject hostRoot = Create("framework-runtime-host");
            FrameworkRuntimeHost host = hostRoot.AddComponent<FrameworkRuntimeHost>();
            _bindings = host.StableObjectBindings;
            _owner = RuntimeContentOwner.Route(
                "qa.stable.public-query-host-stop-route",
                "host teardown test",
                RuntimeDefinitionToken.FromUnityObject(hostRoot));

            Assert.IsTrue(
                _bindings.TryRegisterOwnerContent(_owner, new[] { target, queryRoot }, out string diagnostic),
                diagnostic);
            Assert.IsTrue(_bindings.TryCommitOwner(_owner, out diagnostic), diagnostic);

            IObjectEntryOccurrenceQuery query = anchor;
            StableObjectReference reference = StableObjectReference.ForEntry(ObjectEntryId.From(id));
            ObjectEntryOccurrenceResolution beforeTeardown = query.Resolve(reference);
            Assert.AreEqual(ObjectEntryOccurrenceResolutionStatus.Resolved, beforeTeardown.Status);
            Assert.AreSame(target, beforeTeardown.Occurrence);

            Object.Destroy(hostRoot);
            yield return null;

            Assert.IsTrue(hostRoot == null, "Unity must complete the actual host GameObject destruction.");
            Assert.AreEqual(
                ObjectEntryOccurrenceResolutionStatus.Unavailable,
                query.Resolve(reference).Status);
            Assert.IsTrue(
                _bindings.TryResolve(
                    ObjectEntryId.From(id),
                    null,
                    null,
                    out StableObjectBinding remaining,
                    out _,
                    out string resolveDiagnostic),
                resolveDiagnostic);
            Assert.AreSame(target, remaining.PhysicalObject,
                "Host teardown must invalidate the handed-off query without releasing the occurrence binding.");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_bindings != null && _owner.IsValid)
            {
                _bindings.TryReleaseOwner(_owner, out _);
                _bindings = null;
                _owner = default;
            }

            if (EditorApplication.isPlaying)
            {
                DestroyTrackedObjects();
                yield return null;
                yield return new ExitPlayMode();
            }
            else
            {
                for (int index = 0; index < _objects.Count; index++)
                {
                    GameObject value = _objects[index];
                    if (value != null) Object.DestroyImmediate(value);
                }
                _objects.Clear();
            }

            RestorePlayModeStartupSetting();
            EditorSettings.enterPlayModeOptionsEnabled =
                _originalEnterPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptions = _originalEnterPlayModeOptions;
        }

        private GameObject Create(string name)
        {
            var value = new GameObject(name);
            _objects.Add(value);
            return value;
        }

        private void DestroyTrackedObjects()
        {
            for (int index = 0; index < _objects.Count; index++)
            {
                GameObject value = _objects[index];
                if (value != null) Object.Destroy(value);
            }
            _objects.Clear();
        }

        private void RestorePlayModeStartupSetting()
        {
            if (!_changedPlayModeStartup)
            {
                return;
            }

            ImmersiveFrameworkSettingsAsset settings =
                Resources.Load<ImmersiveFrameworkSettingsAsset>(
                    ImmersiveFrameworkSettingsAsset.ResourcesPath);
            if (settings != null)
            {
                var serializedSettings = new SerializedObject(settings);
                SerializedProperty playModeStartup =
                    serializedSettings.FindProperty("editorPlayModeStartup");
                playModeStartup.enumValueIndex = _originalPlayModeStartup;
                serializedSettings.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }

            _changedPlayModeStartup = false;
        }
    }
}
