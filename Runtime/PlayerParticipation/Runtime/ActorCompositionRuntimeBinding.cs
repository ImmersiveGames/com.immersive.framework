using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Immersive.Composition;
using Immersive.Framework.Actors;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed class ActorCompositionRuntimeBinding
    {
        private const int BaseContributionPriority = int.MinValue;

        private readonly CompositionComposer _composer;
        private readonly CompositionContribution _baseContribution;
        private readonly ActorCompositionPartMaterializer _materializer;
        private readonly Transform _mount;
        private event Action<CompositionDelta> _committed;
        private Task<CompositionResult> _closeTask;
        private string _closeFailure;
        private bool _closed;

        private ActorCompositionRuntimeBinding(
            CompositionComposer composer,
            CompositionContribution baseContribution,
            ActorCompositionPartMaterializer materializer,
            Transform mount)
        {
            _composer = composer;
            _baseContribution = baseContribution;
            _materializer = materializer;
            _mount = mount;
            _composer.Committed += ForwardCommitted;
        }

        internal event Action<CompositionDelta> Committed
        {
            add => _committed += value;
            remove => _committed -= value;
        }

        internal long Revision => _composer != null ? _composer.Revision : 0;

        internal Transform CompositionMount => _mount;

        internal GameObject VisualContent => _materializer != null
            ? _materializer.VisualContent
            : null;

        internal bool TrySetPartsActive(bool active, out string issue)
        {
            return _materializer != null
                ? _materializer.TrySetPartsActive(active, out issue)
                : EmptySuccess(out issue);
        }

        internal bool TryOpenContribution(
            PlayerActorCompositionContributionRequest request,
            Func<bool> isTargetCurrent,
            out PlayerActorCompositionContribution contribution,
            out string issue)
        {
            contribution = null;
            issue = string.Empty;
            if (request == null || !request.IsValid || isTargetCurrent == null || !isTargetCurrent())
            {
                issue = "Actor composition contribution requires a valid request for the exact current occurrence.";
                return false;
            }

            CompositionContribution owner;
            try
            {
                owner = _composer.CreateContribution(
                    CompositionOwnerId.Create("actor-composition-owner:" + Guid.NewGuid().ToString("N")),
                    request.Priority);
            }
            catch (Exception exception)
            {
                issue = $"Actor composition contribution owner could not be created. {exception.GetType().Name}: {exception.Message}";
                return false;
            }

            PlayerActorCompositionContribution opened = null;
            opened = new PlayerActorCompositionContribution(
                request,
                request.Priority,
                () => Revision,
                (definition, cancellationToken) => owner.SetPartWhileCurrentAsync(
                    definition, isTargetCurrent, cancellationToken),
                (definitions, cancellationToken) => owner.SetPartsWhileCurrentAsync(
                    definitions, isTargetCurrent, cancellationToken),
                part => CreateDefinition(part, owner.OwnerId.Value),
                (key, cancellationToken) => owner.RemovePartWhileCurrentAsync(
                    key, isTargetCurrent, cancellationToken),
                cancellationToken => owner.CloseAsync(cancellationToken),
                closed => Committed -= closed.Publish);
            Committed += opened.Publish;
            contribution = opened;
            issue = string.Empty;
            return true;
        }

        internal bool TryOpenContribution(
            ActorId actorId,
            ActorOccurrenceId actorOccurrenceId,
            RuntimeContentOwner ownerIdentity,
            int priority,
            string source,
            string reason,
            Func<bool> isTargetCurrent,
            out ActorOccurrenceCompositionContribution contribution,
            out string issue)
        {
            contribution = null;
            issue = string.Empty;
            if (!actorId.IsValid || !actorOccurrenceId.IsValid || !ownerIdentity.IsValid ||
                string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(reason) ||
                isTargetCurrent == null || !isTargetCurrent())
            {
                issue = "Actor occurrence composition requires exact occurrence, owner, source, reason, and current-target evidence.";
                return false;
            }

            CompositionContribution owner;
            try
            {
                owner = _composer.CreateContribution(
                    CompositionOwnerId.Create("actor-occurrence-composition-owner:" + Guid.NewGuid().ToString("N")),
                    priority);
            }
            catch (Exception exception)
            {
                issue = $"Actor occurrence composition owner could not be created. {exception.GetType().Name}: {exception.Message}";
                return false;
            }

            ActorOccurrenceCompositionContribution opened = null;
            opened = new ActorOccurrenceCompositionContribution(
                actorId,
                actorOccurrenceId,
                ownerIdentity,
                priority,
                source,
                reason,
                () => Revision,
                (definition, cancellationToken) => owner.SetPartWhileCurrentAsync(
                    definition, isTargetCurrent, cancellationToken),
                (definitions, cancellationToken) => owner.SetPartsWhileCurrentAsync(
                    definitions, isTargetCurrent, cancellationToken),
                part => CreateDefinition(part, owner.OwnerId.Value),
                (key, cancellationToken) => owner.RemovePartWhileCurrentAsync(
                    key, isTargetCurrent, cancellationToken),
                cancellationToken => owner.CloseAsync(cancellationToken),
                closed => Committed -= closed.Publish);
            Committed += opened.Publish;
            contribution = opened;
            return true;
        }

        private PartDefinition CreateDefinition(
            PlayerActorCompositionPart part,
            string operationIdentity)
        {
            ContentKey key = ContentKey.Create(part.Key);
            ContentKey[] dependencies = part.Dependencies
                .Select(ContentKey.Create)
                .ToArray();
            var descriptor = new ActorCompositionPart(
                part.Key,
                part.Prefab,
                part.Dependencies);
            var binding = new ActorCompositionPartMaterializer.PartBinding(
                null,
                descriptor,
                _mount,
                null,
                operationIdentity);
            return new PartDefinition(key, binding, _materializer, dependencies);
        }

        private PartDefinition CreateDefinition(
            ActorOccurrenceCompositionPart part,
            string operationIdentity)
        {
            ContentKey key = ContentKey.Create(part.Key);
            ContentKey[] dependencies = part.Dependencies
                .Select(ContentKey.Create)
                .ToArray();
            var descriptor = new ActorCompositionPart(
                part.Key,
                part.Prefab,
                part.Dependencies);
            var binding = new ActorCompositionPartMaterializer.PartBinding(
                null,
                descriptor,
                _mount,
                part.AdoptedInstance,
                operationIdentity);
            return new PartDefinition(key, binding, _materializer, dependencies);
        }

        private void ForwardCommitted(CompositionDelta delta)
        {
            _committed?.Invoke(delta);
        }

        private static bool EmptySuccess(out string issue)
        {
            issue = string.Empty;
            return true;
        }

        internal static bool TryCreate(
            ActorProfile profile,
            Transform mount,
            GameObject adoptedVisual,
            PlayerActorMaterializationOperationId operationId,
            out ActorCompositionRuntimeBinding binding,
            out string issue,
            bool createWhenEmpty = false)
        {
            binding = null;
            issue = string.Empty;
            if (!ActorCompositionProfileContract.TryValidate(profile, out issue))
            {
                return false;
            }

            IReadOnlyList<ActorCompositionPart> authoredParts = profile.CompositionParts;
            if (authoredParts.Count == 0)
            {
                if (adoptedVisual != null)
                {
                    issue = "Scene-Provided Actor has an adopted visual instance, but its Actor Profile has no Base 'visual' composition part.";
                    return false;
                }

                if (!createWhenEmpty)
                {
                    return true;
                }

                if (mount == null)
                {
                    issue = "Dynamic Actor composition requires the Player Actor Runtime Host's exact Visual Content Mount.";
                    return false;
                }

                if (!operationId.IsValid)
                {
                    issue = "Dynamic Actor composition requires the exact Player Actor materialization operation identity.";
                    return false;
                }

                var emptyComposer = new CompositionComposer();
                CompositionContribution emptyBase;
                try
                {
                    emptyBase = emptyComposer.CreateContribution(
                        CompositionOwnerId.Create("actor-profile-base:" + operationId.StableText),
                        BaseContributionPriority);
                }
                catch (Exception exception)
                {
                    issue = $"Actor Profile Base contribution could not be created. {exception.Message}";
                    return false;
                }

                binding = new ActorCompositionRuntimeBinding(
                    emptyComposer,
                    emptyBase,
                    new ActorCompositionPartMaterializer(),
                    mount);
                return true;
            }

            if (mount == null)
            {
                issue = $"Actor Profile '{profile.name}' declares composition parts, but the Player Actor Runtime Host has no Visual Content Mount.";
                return false;
            }

            if (!operationId.IsValid)
            {
                issue = "Actor composition requires the exact Player Actor materialization operation identity.";
                return false;
            }

            if (adoptedVisual != null &&
                (authoredParts.Count != 1 ||
                 !string.Equals(authoredParts[0].Key, "visual", StringComparison.Ordinal)))
            {
                issue = "Scene-Provided adoption currently supports one exact Base 'visual' part; keyed additional scene-part bindings are not authored yet.";
                return false;
            }

            var composer = new CompositionComposer();
            CompositionContribution contribution;
            try
            {
                contribution = composer.CreateContribution(
                    CompositionOwnerId.Create("actor-profile-base:" + operationId.StableText),
                    BaseContributionPriority);
            }
            catch (Exception exception)
            {
                issue = $"Actor Profile Base contribution could not be created. {exception.Message}";
                return false;
            }

            var materializer = new ActorCompositionPartMaterializer();
            var definitions = new List<PartDefinition>(authoredParts.Count);
            try
            {
                for (int index = 0; index < authoredParts.Count; index++)
                {
                    ActorCompositionPart authoredPart = authoredParts[index];
                    ContentKey key = ContentKey.Create(authoredPart.Key);
                    ContentKey[] dependencies = authoredPart.Dependencies
                        .Select(ContentKey.Create)
                        .ToArray();
                    GameObject instance = string.Equals(authoredPart.Key, "visual", StringComparison.Ordinal)
                        ? adoptedVisual
                        : null;
                    var part = new ActorCompositionPartMaterializer.PartBinding(
                        profile,
                        authoredPart,
                        mount,
                        instance,
                        operationId.StableText);
                    definitions.Add(new PartDefinition(key, part, materializer, dependencies));
                }
            }
            catch (Exception exception)
            {
                CloseComposer(composer, out string closeIssue);
                issue = string.IsNullOrEmpty(closeIssue)
                    ? $"Actor Profile composition definitions could not be created. {exception.Message}"
                    : $"Actor Profile composition definitions could not be created. {exception.Message} Cleanup: {closeIssue}";
                return false;
            }

            CompositionResult applyResult;
            try
            {
                Task<CompositionResult> applyTask = contribution.SetPartsAsync(definitions);
                if (!applyTask.IsCompleted)
                {
                    CloseComposer(composer, out string closeIssue);
                    issue = string.IsNullOrEmpty(closeIssue)
                        ? "Initial Actor visual materialization unexpectedly became asynchronous; this Framework boundary accepts only the synchronous Unity prefab provider in D04."
                        : $"Initial Actor visual materialization unexpectedly became asynchronous. Cleanup: {closeIssue}";
                    return false;
                }

                applyResult = applyTask.GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                CloseComposer(composer, out string closeIssue);
                issue = string.IsNullOrEmpty(closeIssue)
                    ? $"Actor Profile composition failed. {exception.GetType().Name}: {exception.Message}"
                    : $"Actor Profile composition failed. {exception.GetType().Name}: {exception.Message} Cleanup: {closeIssue}";
                return false;
            }

            if (applyResult == null || applyResult.Status != CompositionResultStatus.Committed)
            {
                CloseComposer(composer, out string closeIssue);
                issue = BuildIssue(
                    "Actor Profile Base composition did not commit.",
                    applyResult,
                    closeIssue);
                return false;
            }

            binding = new ActorCompositionRuntimeBinding(
                composer, contribution, materializer, mount);
            return true;
        }

        internal static bool TryCreateForPublishedOccurrence(
            Transform compositionMount,
            out ActorCompositionRuntimeBinding binding,
            out string issue)
        {
            binding = null;
            issue = string.Empty;
            if (compositionMount == null)
            {
                issue = "Published Actor composition requires its exact composition mount.";
                return false;
            }

            var composer = new CompositionComposer();
            CompositionContribution baseContribution;
            try
            {
                baseContribution = composer.CreateContribution(
                    CompositionOwnerId.Create("published-actor-base:" + Guid.NewGuid().ToString("N")),
                    BaseContributionPriority);
            }
            catch (Exception exception)
            {
                issue = $"Published Actor base contribution could not be created. {exception.GetType().Name}: {exception.Message}";
                return false;
            }

            binding = new ActorCompositionRuntimeBinding(
                composer,
                baseContribution,
                new ActorCompositionPartMaterializer(true),
                compositionMount);
            return true;
        }

        internal bool TryClose(out string issue)
        {
            issue = string.Empty;
            if (_closed || _composer == null || _baseContribution == null)
            {
                return true;
            }

            try
            {
                _closeTask ??= _composer.CloseAsync();
                if (!_closeTask.IsCompleted)
                {
                    issue = "Actor composition cleanup unexpectedly became asynchronous at the Framework's synchronous release boundary.";
                    return false;
                }

                CompositionResult result = _closeTask.GetAwaiter().GetResult();
                if (result == null ||
                    result.Status is not (CompositionResultStatus.Committed or CompositionResultStatus.NoChange))
                {
                    _closeFailure ??= BuildIssue(
                        "Actor composition cleanup did not complete. The Composer context is closed and does not expose a retry after a failed close.",
                        result,
                        string.Empty);
                    issue = _closeFailure;
                    return false;
                }

                _closed = true;
                _composer.Committed -= ForwardCommitted;
                _committed = null;
                return true;
            }
            catch (Exception exception)
            {
                _closeFailure ??= $"Actor composition cleanup failed. The Composer context close cannot be retried after the attempt. {exception.GetType().Name}: {exception.Message}";
                issue = _closeFailure;
                return false;
            }
        }

        private static bool CloseComposer(CompositionComposer composer, out string issue)
        {
            issue = string.Empty;
            if (composer == null)
            {
                return true;
            }

            try
            {
                Task<CompositionResult> closeTask = composer.CloseAsync();
                if (!closeTask.IsCompleted)
                {
                    issue = "Composition rollback unexpectedly became asynchronous.";
                    return false;
                }

                CompositionResult result = closeTask.GetAwaiter().GetResult();
                if (result == null ||
                    result.Status is not (CompositionResultStatus.Committed or CompositionResultStatus.NoChange))
                {
                    issue = BuildIssue("Composition rollback did not complete.", result, string.Empty);
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                issue = $"Composition rollback failed. {exception.GetType().Name}: {exception.Message}";
                return false;
            }
        }

        private static string BuildIssue(
            string prefix,
            CompositionResult result,
            string cleanupIssue)
        {
            string issues = result != null
                ? string.Join(" | ", result.Issues.Select(item => item.ToString()))
                : "No composition result was returned.";
            string message = $"{prefix} status='{result?.Status}' issues='{issues}'.";
            return string.IsNullOrEmpty(cleanupIssue)
                ? message
                : $"{message} Cleanup: {cleanupIssue}";
        }
    }

    internal sealed class ActorCompositionPartMaterializer : IPartMaterializer
    {
        private readonly object _sync = new object();
        private readonly Dictionary<ContentKey, PartBinding> _currentParts =
            new Dictionary<ContentKey, PartBinding>();
        private readonly Dictionary<ContentKey, PartBinding> _adoptedParts =
            new Dictionary<ContentKey, PartBinding>();
        private bool _active;
        private bool _hasActivityState;

        internal ActorCompositionPartMaterializer(bool initialActive = false)
        {
            _active = initialActive;
        }

        internal GameObject VisualContent
        {
            get
            {
                lock (_sync)
                {
                    return _currentParts.TryGetValue(ContentKey.Create("visual"), out PartBinding part)
                        ? part.Instance
                        : null;
                }
            }
        }

        internal bool TrySetPartsActive(bool active, out string issue)
        {
            issue = string.Empty;
            PartBinding[] parts;
            lock (_sync)
            {
                parts = _currentParts.Values.ToArray();
            }

            foreach (PartBinding part in parts)
            {
                GameObject instance = part.Instance;
                if (instance == null || part.Mount == null || instance.transform.parent != part.Mount)
                {
                    issue = $"Actor composition part '{part.Definition.Key}' no longer matches its exact materialized object and mount.";
                    return false;
                }

                try
                {
                    instance.SetActive(active);
                }
                catch (Exception exception)
                {
                    issue = $"Actor composition part '{part.Definition.Key}' active-state update failed. {exception.GetType().Name}: {exception.Message}";
                    return false;
                }
            }

            lock (_sync)
            {
                _active = active;
                _hasActivityState = true;
            }

            return true;
        }
        internal sealed class PartBinding
        {
            internal PartBinding(
                ActorProfile profile,
                ActorCompositionPart definition,
                Transform mount,
                GameObject adoptedInstance,
                string operationIdentity)
            {
                Profile = profile;
                Definition = definition;
                Mount = mount;
                Instance = adoptedInstance;
                IsAdopted = adoptedInstance != null;
                OperationIdentity = operationIdentity ?? string.Empty;
            }

            internal ActorProfile Profile { get; }
            internal ActorCompositionPart Definition { get; }
            internal Transform Mount { get; }
            internal GameObject Instance { get; set; }
            internal bool IsAdopted { get; }
            internal string OperationIdentity { get; }
        }

        public Task<PartPreparationResult> PrepareAsync(
            PartDefinition definition,
            PartMaterializationContext context,
            CancellationToken cancellationToken)
        {
            if (definition?.Descriptor is not PartBinding binding || binding.Mount == null)
            {
                return Task.FromResult(PartPreparationResult.Failed(
                    "Actor composition preparation requires its exact authored part and mount."));
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromResult(PartPreparationResult.Failed(
                    "Actor composition preparation was cancelled before materialization."));
            }

            if (binding.IsAdopted)
            {
                if (binding.Instance == null || binding.Instance.transform.parent != binding.Mount ||
                    ContainsForbiddenActorRuntimeComponent(binding.Instance))
                {
                    return Task.FromResult(PartPreparationResult.Failed(
                        $"Adopted Actor part '{definition.Key.Value}' is missing, outside its exact composition mount, or contains a forbidden Actor runtime component."));
                }

                lock (_sync)
                {
                    _adoptedParts[definition.Key] = binding;
                }

                return Task.FromResult(PartPreparationResult.Prepared(
                    new PartMaterialization(
                        binding.Instance,
                        PartOwnership.Adopted,
                        new ActorCompositionPartProvenance(
                            binding.Profile,
                            binding,
                            definition.Key,
                            binding.Mount,
                            binding.Instance,
                            binding.OperationIdentity))));
            }

            GameObject instance = null;
            GameObject stagingRoot = null;
            try
            {
                stagingRoot = new GameObject("Actor Composition Preparation");
                stagingRoot.SetActive(false);
                instance = UnityEngine.Object.Instantiate(
                    binding.Definition.Prefab,
                    stagingRoot.transform,
                    false);
                if (instance == null)
                {
                    DestroyObject(stagingRoot);
                    return Task.FromResult(PartPreparationResult.Failed(
                        $"Actor composition prefab for key '{definition.Key.Value}' returned null."));
                }

                instance.SetActive(false);
                instance.transform.SetParent(binding.Mount, false);
                DestroyObject(stagingRoot);
                stagingRoot = null;
                if (!ReferenceEquals(instance.transform.parent, binding.Mount) ||
                    ContainsForbiddenActorRuntimeComponent(instance))
                {
                    DestroyObject(instance);
                    instance = null;
                    if (stagingRoot != null)
                    {
                        DestroyObject(stagingRoot);
                        stagingRoot = null;
                    }
                    return Task.FromResult(PartPreparationResult.Failed(
                        $"Actor composition prefab '{binding.Definition.Prefab.name}' violated the replaceable-part mount or component boundary."));
                }

                instance.name = string.Equals(definition.Key.Value, "visual", StringComparison.Ordinal)
                    ? (binding.Profile != null ? binding.Profile.DisplayName : binding.Definition.Prefab.name)
                    : binding.Definition.Prefab.name;
                binding.Instance = instance;
                return Task.FromResult(PartPreparationResult.Prepared(
                    new PartMaterialization(
                        instance,
                        PartOwnership.Acquired,
                        new ActorCompositionPartProvenance(
                            binding.Profile,
                            binding,
                            definition.Key,
                            binding.Mount,
                            instance,
                            binding.OperationIdentity))));
            }
            catch (Exception exception)
            {
                if (instance != null)
                {
                    DestroyObject(instance);
                }

                if (stagingRoot != null)
                {
                    DestroyObject(stagingRoot);
                }

                binding.Instance = null;
                return Task.FromResult(PartPreparationResult.Failed(
                    $"Actor composition prefab materialization failed. {exception.GetType().Name}: {exception.Message}"));
            }
        }

        public PartEffectResult Apply(
            PartMaterialization candidate,
            PartMaterialization previous,
            PartMaterializationContext context)
        {
            ActorCompositionPartProvenance provenance =
                candidate?.Provenance as ActorCompositionPartProvenance;
            if (candidate?.Value is not GameObject instance || instance == null ||
                provenance == null ||
                instance.transform.parent != provenance.Mount)
            {
                return PartEffectResult.Failed(
                    "Actor composition Apply requires the exact prepared GameObject still attached to its mount.",
                    false);
            }

            if (candidate.Ownership == PartOwnership.Acquired || _active || _hasActivityState)
            {
                instance.SetActive(_active);
            }

            lock (_sync)
            {
                _currentParts[provenance.Key] = provenance.Binding;
            }

            return PartEffectResult.Applied();
        }

        public PartEffectResult Detach(
            PartMaterialization current,
            PartMaterializationContext context)
        {
            ActorCompositionPartProvenance provenance =
                current?.Provenance as ActorCompositionPartProvenance;
            if (current?.Value is not GameObject instance || instance == null || provenance == null)
            {
                return PartEffectResult.Failed(
                    "Actor composition Detach requires the exact current GameObject.",
                    false);
            }

            if (current.Ownership == PartOwnership.Adopted && context.IsClosingContext)
            {
                return PartEffectResult.Applied();
            }

            try
            {
                if (instance.activeSelf)
                {
                    instance.SetActive(false);
                }

                if (current.Ownership == PartOwnership.Acquired && instance.transform.parent != null)
                {
                    instance.transform.SetParent(null, true);
                }

                lock (_sync)
                {
                    if (provenance != null &&
                        _currentParts.TryGetValue(provenance.Key, out PartBinding active) &&
                        ReferenceEquals(active, provenance.Binding))
                    {
                        _currentParts.Remove(provenance.Key);
                    }
                }

                return PartEffectResult.Applied();
            }
            catch (Exception exception)
            {
                return PartEffectResult.Failed(
                    $"Actor composition Detach failed. {exception.GetType().Name}: {exception.Message}",
                    true);
            }
        }

        public Task<PartReleaseResult> ReleaseAsync(
            PartMaterialization materialization,
            PartMaterializationContext context,
            CancellationToken cancellationToken)
        {
            if (materialization?.Value is not GameObject instance)
            {
                return Task.FromResult(PartReleaseResult.Failed(
                    "Actor composition Release requires the exact materialized GameObject."));
            }

            if (materialization.Ownership == PartOwnership.Adopted)
            {
                return Task.FromResult(PartReleaseResult.Retained());
            }

            if (instance == null)
            {
                return Task.FromResult(PartReleaseResult.Released());
            }

            try
            {
                if (instance.activeSelf)
                {
                    instance.SetActive(false);
                }

                if (instance.transform.parent != null)
                {
                    instance.transform.SetParent(null, true);
                }

                DestroyObject(instance);
                if (context.IsClosingContext)
                {
                    RestoreAdoptedPart(context.Key);
                }
                return Task.FromResult(PartReleaseResult.Released());
            }
            catch (Exception exception)
            {
                return Task.FromResult(PartReleaseResult.Failed(
                    $"Actor composition release failed. {exception.GetType().Name}: {exception.Message}"));
            }
        }

        private static void DestroyObject(UnityEngine.Object value)
        {
            if (value == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(value);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(value);
            }
        }

        private static bool ContainsForbiddenActorRuntimeComponent(GameObject instance)
        {
            return instance != null &&
                (instance.GetComponentInChildren<ActorDeclaration>(true) != null ||
                 instance.GetComponentInChildren<PlayerActorRuntimeHost>(true) != null ||
                 instance.GetComponentInChildren<UnityEngine.InputSystem.PlayerInput>(true) != null);
        }

        private void RestoreAdoptedPart(ContentKey key)
        {
            PartBinding adopted;
            lock (_sync)
            {
                if (!_adoptedParts.TryGetValue(key, out adopted))
                {
                    return;
                }
            }

            GameObject instance = adopted.Instance;
            if (instance == null || adopted.Mount == null || instance.transform.parent != adopted.Mount)
            {
                return;
            }

            instance.SetActive(_active);
            lock (_sync)
            {
                _currentParts[key] = adopted;
            }
        }
    }

    internal sealed class ActorCompositionPartProvenance
    {
        internal ActorCompositionPartProvenance(
            ActorProfile profile,
            ActorCompositionPartMaterializer.PartBinding binding,
            ContentKey key,
            Transform mount,
            GameObject instance,
            string operationIdentity)
        {
            Profile = profile;
            Binding = binding;
            Key = key;
            Mount = mount;
            Instance = instance;
            OperationIdentity = operationIdentity ?? string.Empty;
        }

        internal ActorProfile Profile { get; }
        internal ActorCompositionPartMaterializer.PartBinding Binding { get; }
        internal ContentKey Key { get; }
        internal Transform Mount { get; }
        internal GameObject Instance { get; }
        internal string OperationIdentity { get; }
    }
}
