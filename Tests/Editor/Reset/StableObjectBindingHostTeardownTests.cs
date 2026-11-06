using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        private const string AssetFolder = "Assets/__IF_StableObjectBindingHostTeardownTests";
        private const string PendingPrefabPath = AssetFolder + "/PendingCompositionPart.prefab";
        private readonly List<GameObject> _objects = new List<GameObject>();
        private StableObjectBindingRegistry _bindings;
        private RuntimeContentOwner _owner;
        private GameObject _pendingPrefab;
        private TaskCompletionSource<bool> _allowPendingCleanup;
        private bool _originalEnterPlayModeOptionsEnabled;
        private EnterPlayModeOptions _originalEnterPlayModeOptions;
        private int _originalPlayModeStartup;
        private bool _changedPlayModeStartup;

        [SetUp]
        public void SetUp()
        {
            _originalEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            _originalEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
            if (AssetDatabase.IsValidFolder(AssetFolder))
            {
                AssetDatabase.DeleteAsset(AssetFolder);
            }
            AssetDatabase.CreateFolder("Assets", "__IF_StableObjectBindingHostTeardownTests");
            GameObject prefabSource = new GameObject("PendingCompositionPart");
            _pendingPrefab = PrefabUtility.SaveAsPrefabAsset(prefabSource, PendingPrefabPath);
            Object.DestroyImmediate(prefabSource);
            Assert.IsNotNull(_pendingPrefab,
                "The abrupt-teardown mutation fixture requires a prefab asset.");

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
            ObjectEntryCompositionAuthoring authoring =
                target.AddComponent<ObjectEntryCompositionAuthoring>();
            var serializedAuthoring = new SerializedObject(authoring);
            serializedAuthoring.FindProperty("allowDynamicContributions").boolValue = true;
            serializedAuthoring.ApplyModifiedPropertiesWithoutUndo();

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
            Task<string> activation = _bindings.ActivateOwnerCompositionsAsync(_owner);
            while (!activation.IsCompleted)
            {
                yield return null;
            }
            Assert.IsEmpty(activation.GetAwaiter().GetResult());

            Assert.IsTrue(authoring.TryGetCompositionAccessGrant(
                out ObjectEntryCompositionAccessGrant grant,
                out diagnostic), diagnostic);
            Assert.IsTrue(grant.TryGetBinding(
                out _, out StableObjectBinding binding,
                out ObjectEntryCompositionRuntimeSlot contextSlot));
            var cleanupStarted = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _allowPendingCleanup = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var runtime = new ObjectEntryCompositionRuntimeBinding(
                binding,
                null,
                candidate => _bindings.IsCompositionTargetCurrent(candidate),
                async instance =>
                {
                    if (instance != null && instance.name == "Object Entry Composition Preparation")
                    {
                        cleanupStarted.TrySetResult(true);
                        bool destroy = await _allowPendingCleanup.Task;
                        if (destroy && instance != null) Object.DestroyImmediate(instance);
                        return destroy && instance == null;
                    }

                    if (instance != null) Object.DestroyImmediate(instance);
                    return instance == null;
                },
                allowDynamicContributions: true);
            Assert.IsTrue(contextSlot.TryBind(runtime));
            binding.CompositionRuntime = runtime;
            Assert.IsTrue(grant.TryOpenContribution(
                0, out ObjectEntryCompositionContribution contribution, out diagnostic), diagnostic);
            Task<ObjectEntryCompositionOperationResult> mutation = contribution.SetPartAsync(
                new ObjectEntryCompositionPartDefinition("pending-host-teardown", _pendingPrefab));
            while (!cleanupStarted.Task.IsCompleted && !mutation.IsCompleted)
            {
                yield return null;
            }
            Assert.IsTrue(cleanupStarted.Task.IsCompleted,
                "The dynamic mutation must remain pending at the asynchronous preparation-cleanup boundary.");

            IObjectEntryOccurrenceQuery query = anchor;
            StableObjectReference reference = StableObjectReference.ForEntry(ObjectEntryId.From(id));
            ObjectEntryOccurrenceResolution beforeTeardown = query.Resolve(reference);
            Assert.AreEqual(ObjectEntryOccurrenceResolutionStatus.Resolved, beforeTeardown.Status);
            Assert.AreSame(target, beforeTeardown.Occurrence);

            Object.Destroy(hostRoot);
            yield return null;

            Assert.IsTrue(hostRoot == null, "Unity must complete the actual host GameObject destruction.");
            Assert.IsFalse(mutation.IsCompleted,
                "OnDestroy must leave cleanup unconfirmed while asynchronous preparation cleanup is still gated.");
            Assert.IsFalse(grant.TryOpenContribution(1, out _, out _),
                "Host destruction must synchronously invalidate the old dynamic grant.");
            Assert.AreEqual(1, _bindings.BindingCount,
                "The owner record must remain registered while abrupt teardown cannot await cleanup.");
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

            _allowPendingCleanup.TrySetResult(true);
            while (!mutation.IsCompleted)
            {
                yield return null;
            }
            ObjectEntryCompositionOperationResult mutationResult = mutation.GetAwaiter().GetResult();
            Assert.AreEqual(ObjectEntryCompositionOperationStatus.Stale,
                mutationResult.Status, mutationResult.Diagnostic);
            Assert.IsFalse(mutationResult.EffectsMayHaveApplied);
            StringAssert.Contains("StaleOperation", mutationResult.Diagnostic);
            Task<string> release = _bindings.ReleaseOwnerAsync(_owner);
            while (!release.IsCompleted)
            {
                yield return null;
            }
            Assert.IsEmpty(release.GetAwaiter().GetResult());
            Assert.AreEqual(0, _bindings.BindingCount);
            Assert.IsTrue(target != null, "Owner cleanup must preserve the Object Entry root.");
            Assert.AreEqual(0, target.transform.childCount,
                "Host teardown cancellation and subsequent owner release must remove the pending preparation resources.");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            string releaseDiagnostic = string.Empty;
            _allowPendingCleanup?.TrySetResult(true);
            if (_bindings != null && _owner.IsValid)
            {
                Task<string> release = _bindings.ReleaseOwnerAsync(_owner);
                while (!release.IsCompleted)
                {
                    yield return null;
                }
                try
                {
                    releaseDiagnostic = release.GetAwaiter().GetResult();
                }
                catch (System.Exception exception)
                {
                    releaseDiagnostic = $"Owner release task threw '{exception.GetType().Name}: {exception.Message}'.";
                }
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
            _allowPendingCleanup = null;
            if (AssetDatabase.IsValidFolder(AssetFolder))
            {
                AssetDatabase.DeleteAsset(AssetFolder);
                AssetDatabase.Refresh();
            }
            _pendingPrefab = null;
            Assert.IsEmpty(releaseDiagnostic,
                "The explicit post-teardown owner cleanup must finish before the fixture is discarded.");
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
