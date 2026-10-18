using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Immersive.Composition;
using Immersive.Framework.Actors;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed class ActorCompositionRuntimeBinding
    {
        private const int BaseContributionPriority = int.MinValue;

        private readonly CompositionComposer _composer;
        private readonly CompositionContribution _baseContribution;
        private readonly Dictionary<ContentKey, ActorCompositionPartMaterializer.PartBinding> _parts;

        private ActorCompositionRuntimeBinding(
            CompositionComposer composer,
            CompositionContribution baseContribution,
            Dictionary<ContentKey, ActorCompositionPartMaterializer.PartBinding> parts)
        {
            _composer = composer;
            _baseContribution = baseContribution;
            _parts = parts;
        }

        internal GameObject VisualContent
        {
            get
            {
                ContentKey visualKey = ContentKey.Create("visual");
                return _parts.TryGetValue(visualKey, out ActorCompositionPartMaterializer.PartBinding part)
                    ? part.Instance
                    : null;
            }
        }

        internal bool TrySetPartsActive(bool active, out string issue)
        {
            issue = string.Empty;
            foreach (KeyValuePair<ContentKey, ActorCompositionPartMaterializer.PartBinding> pair in _parts)
            {
                GameObject instance = pair.Value.Instance;
                if (instance == null || pair.Value.Mount == null ||
                    instance.transform.parent != pair.Value.Mount)
                {
                    issue = $"Actor composition part '{pair.Key.Value}' no longer matches its exact materialized object and mount.";
                    return false;
                }

                try
                {
                    instance.SetActive(active);
                }
                catch (Exception exception)
                {
                    issue = $"Actor composition part '{pair.Key.Value}' active-state update failed. {exception.GetType().Name}: {exception.Message}";
                    return false;
                }
            }

            return true;
        }

        internal static bool TryCreate(
            ActorProfile profile,
            Transform mount,
            GameObject adoptedVisual,
            PlayerActorMaterializationOperationId operationId,
            out ActorCompositionRuntimeBinding binding,
            out string issue)
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
            var parts = new Dictionary<ContentKey, ActorCompositionPartMaterializer.PartBinding>();
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
                    parts.Add(key, part);
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

            binding = new ActorCompositionRuntimeBinding(composer, contribution, parts);
            return true;
        }

        internal bool TryClose(out string issue)
        {
            issue = string.Empty;
            if (_composer == null || _baseContribution == null)
            {
                return true;
            }

            try
            {
                Task<CompositionResult> closeTask = _composer.CloseAsync();
                if (!closeTask.IsCompleted)
                {
                    issue = "Actor composition cleanup unexpectedly became asynchronous at the Framework's synchronous release boundary.";
                    return false;
                }

                CompositionResult result = closeTask.GetAwaiter().GetResult();
                if (result == null ||
                    result.Status is not (CompositionResultStatus.Committed or CompositionResultStatus.NoChange))
                {
                    issue = BuildIssue("Actor composition cleanup did not complete.", result, string.Empty);
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                issue = $"Actor composition cleanup failed. {exception.GetType().Name}: {exception.Message}";
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
                if (binding.Instance == null || binding.Instance.transform.parent != binding.Mount)
                {
                    return Task.FromResult(PartPreparationResult.Failed(
                        $"Adopted Actor part '{definition.Key.Value}' is missing or no longer under its exact Visual Content Mount."));
                }

                return Task.FromResult(PartPreparationResult.Prepared(
                    new PartMaterialization(
                        binding.Instance,
                        PartOwnership.Adopted,
                        new ActorCompositionPartProvenance(
                            binding.Profile,
                            definition.Key,
                            binding.Mount,
                            binding.Instance,
                            binding.OperationIdentity))));
            }

            GameObject instance = null;
            try
            {
                instance = UnityEngine.Object.Instantiate(
                    binding.Definition.Prefab,
                    binding.Mount,
                    false);
                if (instance == null)
                {
                    return Task.FromResult(PartPreparationResult.Failed(
                        $"Actor composition prefab for key '{definition.Key.Value}' returned null."));
                }

                instance.SetActive(false);
                if (!ReferenceEquals(instance.transform.parent, binding.Mount) ||
                    instance.GetComponentInChildren<ActorDeclaration>(true) != null ||
                    instance.GetComponentInChildren<PlayerActorRuntimeHost>(true) != null ||
                    instance.GetComponentInChildren<UnityEngine.InputSystem.PlayerInput>(true) != null)
                {
                    DestroyObject(instance);
                    instance = null;
                    return Task.FromResult(PartPreparationResult.Failed(
                        $"Actor composition prefab '{binding.Definition.Prefab.name}' violated the replaceable-part mount or component boundary."));
                }

                instance.name = string.Equals(definition.Key.Value, "visual", StringComparison.Ordinal)
                    ? binding.Profile.DisplayName
                    : binding.Definition.Prefab.name;
                binding.Instance = instance;
                return Task.FromResult(PartPreparationResult.Prepared(
                    new PartMaterialization(
                        instance,
                        PartOwnership.Acquired,
                        new ActorCompositionPartProvenance(
                            binding.Profile,
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

            if (candidate.Ownership == PartOwnership.Acquired)
            {
                instance.SetActive(false);
            }

            return PartEffectResult.Applied();
        }

        public PartEffectResult Detach(
            PartMaterialization current,
            PartMaterializationContext context)
        {
            if (current?.Value is not GameObject instance || instance == null)
            {
                return PartEffectResult.Failed(
                    "Actor composition Detach requires the exact current GameObject.",
                    false);
            }

            if (current.Ownership == PartOwnership.Adopted)
            {
                return PartEffectResult.Applied();
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
    }

    internal sealed class ActorCompositionPartProvenance
    {
        internal ActorCompositionPartProvenance(
            ActorProfile profile,
            ContentKey key,
            Transform mount,
            GameObject instance,
            string operationIdentity)
        {
            Profile = profile;
            Key = key;
            Mount = mount;
            Instance = instance;
            OperationIdentity = operationIdentity ?? string.Empty;
        }

        internal ActorProfile Profile { get; }
        internal ContentKey Key { get; }
        internal Transform Mount { get; }
        internal GameObject Instance { get; }
        internal string OperationIdentity { get; }
    }
}
