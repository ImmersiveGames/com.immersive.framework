using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Immersive.Composition;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.RuntimeContent;
using Immersive.Logging.Records;

namespace Immersive.Framework.Actors
{
    /// <summary>One owner-scoped content intent for an exact published Actor occurrence.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-050 D07 owner-bound NonPlayer composition contribution.")]
    public sealed class ActorOccurrenceCompositionContribution
    {
        private readonly ActorId _actorId;
        private readonly ActorOccurrenceId _actorOccurrenceId;
        private readonly RuntimeContentOwner _owner;
        private readonly int _priority;
        private readonly Func<long> _revision;
        private readonly Func<PartDefinition, CancellationToken, Task<CompositionResult>> _setPart;
        private readonly Func<IReadOnlyList<PartDefinition>, CancellationToken, Task<CompositionResult>> _setParts;
        private readonly Func<ActorOccurrenceCompositionPart, PartDefinition> _createDefinition;
        private readonly Func<ContentKey, CancellationToken, Task<CompositionResult>> _removePart;
        private readonly Func<CancellationToken, Task<CompositionResult>> _close;
        private readonly Action<ActorOccurrenceCompositionContribution> _onClosed;
        private bool _isClosed;
        private bool _isClosing;

        internal ActorOccurrenceCompositionContribution(
            ActorId actorId,
            ActorOccurrenceId actorOccurrenceId,
            RuntimeContentOwner owner,
            int priority,
            string source,
            string reason,
            Func<long> revision,
            Func<PartDefinition, CancellationToken, Task<CompositionResult>> setPart,
            Func<IReadOnlyList<PartDefinition>, CancellationToken, Task<CompositionResult>> setParts,
            Func<ActorOccurrenceCompositionPart, PartDefinition> createDefinition,
            Func<ContentKey, CancellationToken, Task<CompositionResult>> removePart,
            Func<CancellationToken, Task<CompositionResult>> close,
            Action<ActorOccurrenceCompositionContribution> onClosed)
        {
            _actorId = actorId;
            _actorOccurrenceId = actorOccurrenceId;
            _owner = owner;
            _priority = priority;
            _revision = revision ?? throw new ArgumentNullException(nameof(revision));
            _setPart = setPart ?? throw new ArgumentNullException(nameof(setPart));
            _setParts = setParts ?? throw new ArgumentNullException(nameof(setParts));
            _createDefinition = createDefinition ?? throw new ArgumentNullException(nameof(createDefinition));
            _removePart = removePart ?? throw new ArgumentNullException(nameof(removePart));
            _close = close ?? throw new ArgumentNullException(nameof(close));
            _onClosed = onClosed;
            Source = Normalize(source, string.Empty);
            Reason = Normalize(reason, string.Empty);
        }

        public int Priority => _priority;
        public ActorId ActorId => _actorId;
        public ActorOccurrenceId ActorOccurrenceId => _actorOccurrenceId;
        public RuntimeContentOwner Owner => _owner;
        public string Source { get; }
        public string Reason { get; }
        public bool IsClosed => _isClosed;
        public event Action<CompositionDelta> CompositionCommitted;

        /// <summary>Sets or replaces one keyed part while preserving this contribution's other keys.</summary>
        public async Task<ActorOccurrenceCompositionOperationResult> SetPartAsync(
            ActorOccurrenceCompositionPart part,
            string source,
            string reason,
            CancellationToken cancellationToken = default)
        {
            if (part == null)
                throw new ArgumentNullException(nameof(part));

            long revisionBefore = _revision();
            string normalizedSource = Normalize(source, Source);
            string normalizedReason = Normalize(reason, Reason);
            if (_isClosed)
                return Complete(null, revisionBefore, normalizedSource, normalizedReason,
                    "Actor occurrence composition contribution is already closed.",
                    ActorOccurrenceCompositionOperationStatus.Rejected);
            if (!part.IsValid)
                return Complete(null, revisionBefore, normalizedSource, normalizedReason,
                    "Actor occurrence composition parts require a key, exactly one prefab or adopted instance, and valid dependency keys.",
                    ActorOccurrenceCompositionOperationStatus.Rejected);

            try
            {
                return Complete(
                    await _setPart(_createDefinition(part), cancellationToken),
                    revisionBefore,
                    normalizedSource,
                    normalizedReason,
                    null);
            }
            catch (OperationCanceledException)
            {
                return Complete(null, revisionBefore, normalizedSource, normalizedReason,
                    "Actor occurrence composition update was cancelled before a result was returned.",
                    ActorOccurrenceCompositionOperationStatus.Cancelled);
            }
            catch (Exception exception)
            {
                return Complete(null, revisionBefore, normalizedSource, normalizedReason,
                    $"Actor occurrence composition update failed. {exception.GetType().Name}: {exception.Message}",
                    ActorOccurrenceCompositionOperationStatus.Faulted);
            }
        }

        /// <summary>Replaces the complete keyed intent set owned by this contribution.</summary>
        public async Task<ActorOccurrenceCompositionOperationResult> SetPartsAsync(
            IEnumerable<ActorOccurrenceCompositionPart> parts,
            string source,
            string reason,
            CancellationToken cancellationToken = default)
        {
            if (parts == null)
                throw new ArgumentNullException(nameof(parts));
            long revisionBefore = _revision();
            string normalizedSource = Normalize(source, Source);
            string normalizedReason = Normalize(reason, Reason);
            if (_isClosed)
                return Complete(null, revisionBefore, normalizedSource, normalizedReason,
                    "Actor occurrence composition contribution is already closed.",
                    ActorOccurrenceCompositionOperationStatus.Rejected);

            var definitions = new List<PartDefinition>();
            try
            {
                foreach (ActorOccurrenceCompositionPart part in parts)
                {
                    if (part == null || !part.IsValid)
                        return Complete(null, revisionBefore, normalizedSource, normalizedReason,
                            "Actor occurrence composition parts require a key, exactly one prefab or adopted instance, and valid dependency keys.",
                            ActorOccurrenceCompositionOperationStatus.Rejected);
                    definitions.Add(_createDefinition(part));
                }
            }
            catch (Exception exception)
            {
                return Complete(null, revisionBefore, normalizedSource, normalizedReason,
                    $"Actor occurrence composition definitions could not be created. {exception.GetType().Name}: {exception.Message}",
                    ActorOccurrenceCompositionOperationStatus.Rejected);
            }

            if (definitions.Count == 0)
                return Complete(null, revisionBefore, normalizedSource, normalizedReason,
                    "At least one Actor occurrence composition part is required.",
                    ActorOccurrenceCompositionOperationStatus.Rejected);

            try
            {
                CompositionResult result = await _setParts(definitions, cancellationToken);
                return Complete(result, revisionBefore, normalizedSource, normalizedReason, null);
            }
            catch (OperationCanceledException)
            {
                return Complete(null, revisionBefore, normalizedSource, normalizedReason,
                    "Actor occurrence composition update was cancelled before a result was returned.",
                    ActorOccurrenceCompositionOperationStatus.Cancelled);
            }
            catch (Exception exception)
            {
                return Complete(null, revisionBefore, normalizedSource, normalizedReason,
                    $"Actor occurrence composition update failed. {exception.GetType().Name}: {exception.Message}",
                    ActorOccurrenceCompositionOperationStatus.Faulted);
            }
        }

        public async Task<ActorOccurrenceCompositionOperationResult> RemovePartAsync(
            string key,
            string source,
            string reason,
            CancellationToken cancellationToken = default)
        {
            long revisionBefore = _revision();
            string normalizedSource = Normalize(source, Source);
            string normalizedReason = Normalize(reason, Reason);
            if (_isClosed)
                return Complete(null, revisionBefore, normalizedSource, normalizedReason,
                    "Actor occurrence composition contribution is already closed.",
                    ActorOccurrenceCompositionOperationStatus.Rejected);
            try
            {
                return Complete(await _removePart(ContentKey.Create(key), cancellationToken),
                    revisionBefore, normalizedSource, normalizedReason, null);
            }
            catch (OperationCanceledException)
            {
                return Complete(null, revisionBefore, normalizedSource, normalizedReason,
                    "Actor occurrence composition part removal was cancelled before a result was returned.",
                    ActorOccurrenceCompositionOperationStatus.Cancelled);
            }
            catch (Exception exception)
            {
                return Complete(null, revisionBefore, normalizedSource, normalizedReason,
                    $"Actor occurrence composition part removal failed. {exception.GetType().Name}: {exception.Message}",
                    ActorOccurrenceCompositionOperationStatus.Rejected);
            }
        }

        public async Task<ActorOccurrenceCompositionOperationResult> CloseAsync(
            string source,
            string reason,
            CancellationToken cancellationToken = default)
        {
            long revisionBefore = _revision();
            string normalizedSource = Normalize(source, Source);
            string normalizedReason = Normalize(reason, Reason);
            if (_isClosed)
                return Complete(null, revisionBefore, normalizedSource, normalizedReason, null,
                    ActorOccurrenceCompositionOperationStatus.NoChange);
            if (_isClosing)
                return Complete(null, revisionBefore, normalizedSource, normalizedReason,
                    "Actor occurrence composition contribution close is already in progress.",
                    ActorOccurrenceCompositionOperationStatus.Rejected);

            _isClosing = true;
            try
            {
                CompositionResult result = await _close(cancellationToken);
                ActorOccurrenceCompositionOperationResult operation =
                    Complete(result, revisionBefore, normalizedSource, normalizedReason, null);
                if (result != null &&
                    (result.Status is CompositionResultStatus.Committed or CompositionResultStatus.NoChange))
                {
                    _isClosed = true;
                    _onClosed?.Invoke(this);
                }
                return operation;
            }
            catch (OperationCanceledException)
            {
                return Complete(null, revisionBefore, normalizedSource, normalizedReason,
                    "Actor occurrence composition contribution close was cancelled before a result was returned.",
                    ActorOccurrenceCompositionOperationStatus.Cancelled);
            }
            catch (Exception exception)
            {
                return Complete(null, revisionBefore, normalizedSource, normalizedReason,
                    $"Actor occurrence composition contribution close failed. {exception.GetType().Name}: {exception.Message}",
                    ActorOccurrenceCompositionOperationStatus.Faulted);
            }
            finally
            {
                _isClosing = false;
                if (_isClosed)
                    CompositionCommitted = null;
            }
        }

        internal void Publish(CompositionDelta delta) => CompositionCommitted?.Invoke(delta);

        private ActorOccurrenceCompositionOperationResult Complete(
            CompositionResult result,
            long revisionBefore,
            string source,
            string reason,
            string localIssue,
            ActorOccurrenceCompositionOperationStatus? localStatus = null)
        {
            ActorOccurrenceCompositionOperationStatus status = localStatus ?? (result == null
                ? ActorOccurrenceCompositionOperationStatus.Stale
                : (ActorOccurrenceCompositionOperationStatus)result.Status);
            string issue = localIssue ?? (result != null && result.Issues.Count > 0
                ? string.Join(" | ", result.Issues.Select(item => item.ToString()))
                : string.Empty);
            var operation = new ActorOccurrenceCompositionOperationResult(
                status, _actorId, _actorOccurrenceId, _owner, result, revisionBefore, source, reason, issue);
            var fields = LogFields.Of(
                LogFields.Field("status", operation.Status),
                LogFields.Field("owner", operation.Owner.StableText),
                LogFields.Field("actor", operation.ActorId.StableText),
                LogFields.Field("actorOccurrence", operation.ActorOccurrenceId.Value),
                LogFields.Field("priority", Priority),
                LogFields.Field("revisionBefore", revisionBefore),
                LogFields.Field("revisionAfter", operation.CompositionRevision),
                LogFields.Field("changedKeys", result == null ? string.Empty : string.Join(",", result.ChangedKeys)),
                LogFields.Field("source", source),
                LogFields.Field("reason", reason),
                LogFields.Field("issue", operation.Issue));
            FrameworkLogger logger = FrameworkLogger.Create<ActorOccurrenceCompositionContribution>();
            if (status is ActorOccurrenceCompositionOperationStatus.Committed or ActorOccurrenceCompositionOperationStatus.NoChange)
                logger.Info("Actor occurrence composition operation completed.", fields);
            else
                logger.Warning("Actor occurrence composition operation did not commit.", fields);
            return operation;
        }

        private static string Normalize(string value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
