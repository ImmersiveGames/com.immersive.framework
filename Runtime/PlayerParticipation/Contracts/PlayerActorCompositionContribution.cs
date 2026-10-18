using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Immersive.Composition;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.PlayerSlots;
using Immersive.Logging.Records;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>One owner-scoped intent for keyed parts on an exact live Actor occurrence.</summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-ADR-050 D05 owner-scoped keyed Actor composition contribution.")]
    public sealed class PlayerActorCompositionContribution
    {
        private readonly PlayerActorCompositionContributionRequest _request;
        private readonly int _priority;
        private readonly Func<long> _revision;
        private readonly Func<PartDefinition, CancellationToken, Task<CompositionResult>> _setPart;
        private readonly Func<IReadOnlyList<PartDefinition>, CancellationToken, Task<CompositionResult>> _setParts;
        private readonly Func<PlayerActorCompositionPart, PartDefinition> _createDefinition;
        private readonly Func<ContentKey, CancellationToken, Task<CompositionResult>> _removePart;
        private readonly Func<CancellationToken, Task<CompositionResult>> _close;
        private readonly Action<PlayerActorCompositionContribution> _onClosed;
        private bool _isClosed;
        private bool _isClosing;

        internal PlayerActorCompositionContribution(
            PlayerActorCompositionContributionRequest request,
            int priority,
            Func<long> revision,
            Func<PartDefinition, CancellationToken, Task<CompositionResult>> setPart,
            Func<IReadOnlyList<PartDefinition>, CancellationToken, Task<CompositionResult>> setParts,
            Func<PlayerActorCompositionPart, PartDefinition> createDefinition,
            Func<ContentKey, CancellationToken, Task<CompositionResult>> removePart,
            Func<CancellationToken, Task<CompositionResult>> close,
            Action<PlayerActorCompositionContribution> onClosed)
        {
            _request = request ?? throw new ArgumentNullException(nameof(request));
            _priority = priority;
            _revision = revision ?? throw new ArgumentNullException(nameof(revision));
            _setPart = setPart ?? throw new ArgumentNullException(nameof(setPart));
            _setParts = setParts ?? throw new ArgumentNullException(nameof(setParts));
            _createDefinition = createDefinition ?? throw new ArgumentNullException(nameof(createDefinition));
            _removePart = removePart ?? throw new ArgumentNullException(nameof(removePart));
            _close = close ?? throw new ArgumentNullException(nameof(close));
            _onClosed = onClosed;
        }

        public int Priority => _priority;
        public PlayerSlotId PlayerSlotId => _request.PlayerSlotId;
        public PlayerOccurrenceId PlayerOccurrenceId => _request.PlayerOccurrenceId;
        public Immersive.Framework.Actors.ActorId ActorId => _request.ActorId;
        public Immersive.Framework.Actors.ActorOccurrenceId ActorOccurrenceId => _request.ActorOccurrenceId;
        public bool IsClosed => _isClosed;

        /// <summary>
        /// Reports every committed composition delta for this exact Actor occurrence,
        /// including changes made by other owner contributions.
        /// </summary>
        public event Action<CompositionDelta> CompositionCommitted;

        /// <summary>Sets or replaces one keyed part while preserving this contribution's other keys.</summary>
        public async Task<PlayerActorCompositionOperationResult> SetPartAsync(
            PlayerActorCompositionPart part,
            string source,
            string reason,
            CancellationToken cancellationToken = default)
        {
            if (part == null)
            {
                throw new ArgumentNullException(nameof(part));
            }

            string normalizedSource = Normalize(source, _request.Source);
            string normalizedReason = Normalize(reason, _request.Reason);
            long revisionBefore = _revision();
            if (_isClosed)
            {
                return Complete(null, normalizedSource, normalizedReason,
                    "Actor composition contribution is already closed.", revisionBefore,
                    PlayerActorCompositionOperationStatus.Rejected);
            }
            if (!part.IsValid)
            {
                return Complete(null, normalizedSource, normalizedReason,
                    "Actor composition parts require a non-empty key, a prefab and valid dependency keys.", revisionBefore,
                    PlayerActorCompositionOperationStatus.Rejected);
            }

            try
            {
                return Complete(
                    await _setPart(_createDefinition(part), cancellationToken),
                    normalizedSource,
                    normalizedReason,
                    null,
                    revisionBefore);
            }
            catch (OperationCanceledException)
            {
                return Complete(null, normalizedSource, normalizedReason,
                    "Actor composition update was cancelled before a result was returned.", revisionBefore,
                    PlayerActorCompositionOperationStatus.Cancelled);
            }
            catch (Exception exception)
            {
                return Complete(null, normalizedSource, normalizedReason,
                    $"Actor composition update failed. {exception.GetType().Name}: {exception.Message}", revisionBefore,
                    PlayerActorCompositionOperationStatus.Faulted);
            }
        }

        /// <summary>Replaces the complete keyed intent set owned by this contribution.</summary>
        public async Task<PlayerActorCompositionOperationResult> SetPartsAsync(
            IEnumerable<PlayerActorCompositionPart> parts,
            string source,
            string reason,
            CancellationToken cancellationToken = default)
        {
            if (parts == null)
            {
                throw new ArgumentNullException(nameof(parts));
            }

            string normalizedSource = Normalize(source, _request.Source);
            string normalizedReason = Normalize(reason, _request.Reason);
            long revisionBefore = _revision();
            if (_isClosed)
            {
                return Complete(null, normalizedSource, normalizedReason,
                    "Actor composition contribution is already closed.", revisionBefore,
                    PlayerActorCompositionOperationStatus.Rejected);
            }

            var definitions = new List<PartDefinition>();
            try
            {
                foreach (PlayerActorCompositionPart part in parts)
                {
                    if (part == null || !part.IsValid)
                    {
                        return Complete(null, normalizedSource, normalizedReason,
                            "Actor composition parts require a non-empty key, a prefab and valid dependency keys.", revisionBefore,
                            PlayerActorCompositionOperationStatus.Rejected);
                    }

                    definitions.Add(_createDefinition(part));
                }
            }
            catch (Exception exception)
            {
                return Complete(null, normalizedSource, normalizedReason,
                    $"Actor composition definitions could not be created. {exception.GetType().Name}: {exception.Message}", revisionBefore,
                    PlayerActorCompositionOperationStatus.Rejected);
            }

            if (definitions.Count == 0)
            {
                return Complete(null, normalizedSource, normalizedReason,
                    "At least one Actor composition part is required.", revisionBefore,
                    PlayerActorCompositionOperationStatus.Rejected);
            }

            CompositionResult result;
            try
            {
                result = await _setParts(definitions, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return Complete(null, normalizedSource, normalizedReason,
                    "Actor composition update was cancelled before a result was returned.", revisionBefore,
                    PlayerActorCompositionOperationStatus.Cancelled);
            }
            catch (Exception exception)
            {
                return Complete(null, normalizedSource, normalizedReason,
                    $"Actor composition update failed. {exception.GetType().Name}: {exception.Message}", revisionBefore,
                    PlayerActorCompositionOperationStatus.Faulted);
            }

            return Complete(result, normalizedSource, normalizedReason, null, revisionBefore);
        }

        public async Task<PlayerActorCompositionOperationResult> RemovePartAsync(
            string key,
            string source,
            string reason,
            CancellationToken cancellationToken = default)
        {
            string normalizedSource = Normalize(source, _request.Source);
            string normalizedReason = Normalize(reason, _request.Reason);
            long revisionBefore = _revision();
            if (_isClosed)
            {
                return Complete(null, normalizedSource, normalizedReason,
                    "Actor composition contribution is already closed.", revisionBefore,
                    PlayerActorCompositionOperationStatus.Rejected);
            }

            try
            {
                ContentKey contentKey = ContentKey.Create(key);
                return Complete(
                    await _removePart(contentKey, cancellationToken),
                    normalizedSource,
                    normalizedReason,
                    null,
                    revisionBefore);
            }
            catch (OperationCanceledException)
            {
                return Complete(null, normalizedSource, normalizedReason,
                    "Actor composition part removal was cancelled before a result was returned.", revisionBefore,
                    PlayerActorCompositionOperationStatus.Cancelled);
            }
            catch (Exception exception)
            {
                return Complete(null, normalizedSource, normalizedReason,
                    $"Actor composition part removal failed. {exception.GetType().Name}: {exception.Message}", revisionBefore,
                    PlayerActorCompositionOperationStatus.Rejected);
            }
        }

        public async Task<PlayerActorCompositionOperationResult> CloseAsync(
            string source,
            string reason,
            CancellationToken cancellationToken = default)
        {
            string normalizedSource = Normalize(source, _request.Source);
            string normalizedReason = Normalize(reason, _request.Reason);
            long revisionBefore = _revision();
            if (_isClosed)
            {
                return Complete(null, normalizedSource, normalizedReason,
                    null, revisionBefore,
                    PlayerActorCompositionOperationStatus.NoChange);
            }

            if (_isClosing)
            {
                return Complete(null, normalizedSource, normalizedReason,
                    "Actor composition contribution close is already in progress.", revisionBefore,
                    PlayerActorCompositionOperationStatus.Rejected);
            }

            _isClosing = true;
            try
            {
                CompositionResult result = await _close(cancellationToken);
                if (result == null || result.Status is CompositionResultStatus.Cancelled or
                    CompositionResultStatus.Stale)
                {
                    return Complete(
                        result,
                        normalizedSource,
                        normalizedReason,
                        null,
                        revisionBefore);
                }

                _isClosed = true;
                _onClosed?.Invoke(this);
                return Complete(
                    result,
                    normalizedSource,
                    normalizedReason,
                    null,
                    revisionBefore);
            }
            catch (OperationCanceledException)
            {
                return Complete(null, normalizedSource, normalizedReason,
                    "Actor composition contribution close was cancelled before a result was returned.", revisionBefore,
                    PlayerActorCompositionOperationStatus.Cancelled);
            }
            catch (Exception exception)
            {
                return Complete(null, normalizedSource, normalizedReason,
                    $"Actor composition contribution close failed. {exception.GetType().Name}: {exception.Message}", revisionBefore,
                    PlayerActorCompositionOperationStatus.Faulted);
            }
            finally
            {
                _isClosing = false;
                if (_isClosed)
                {
                    CompositionCommitted = null;
                }
            }
        }

        internal void Publish(CompositionDelta delta) => CompositionCommitted?.Invoke(delta);

        private PlayerActorCompositionOperationResult Complete(
            CompositionResult result,
            string source,
            string reason,
            string issue,
            long revisionBefore,
            PlayerActorCompositionOperationStatus? localStatus = null)
        {
            PlayerActorCompositionOperationResult operation =
                PlayerActorCompositionOperationResult.From(
                    _request, result, source, reason, issue, localStatus);
            var fields = LogFields.Of(
                LogFields.Field("status", operation.Status),
                LogFields.Field("playerSlot", operation.PlayerSlotId.StableText),
                LogFields.Field("playerOccurrence", operation.PlayerOccurrenceId.Value),
                LogFields.Field("actor", operation.ActorId.StableText),
                LogFields.Field("actorOccurrence", operation.ActorOccurrenceId.Value),
                LogFields.Field("priority", Priority),
                LogFields.Field("revisionBefore", revisionBefore),
                LogFields.Field("revisionAfter", operation.CompositionRevision),
                LogFields.Field("changedKeys", result == null ? string.Empty : string.Join(",", result.ChangedKeys)),
                LogFields.Field("source", source),
                LogFields.Field("reason", reason),
                LogFields.Field("issue", operation.Issue));
            FrameworkLogger logger = FrameworkLogger.Create<PlayerActorCompositionContribution>();
            if (operation.Status is PlayerActorCompositionOperationStatus.Committed or
                PlayerActorCompositionOperationStatus.NoChange)
            {
                logger.Info("Actor composition contribution operation completed.", fields);
            }
            else
            {
                logger.Warning("Actor composition contribution operation did not commit.", fields);
            }

            return operation;
        }

        private static string Normalize(string value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
