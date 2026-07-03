using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Lifecycle-owned evidence that one runtime content identity was materialized and belongs to one lifecycle owner.
    /// The entry stores typed identity and logical handle evidence only. It does not store GameObject names as identity, instantiate objects, destroy objects, bind anchors or call Route/Activity lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R-N lifecycle-owned materialized entry evidence; typed identity only, no physical authority.")]
    public sealed class LifecycleMaterializedEntry
    {
        private LifecycleMaterializationEntryState _state;
        private string _source;
        private string _reason;
        private string _message;

        public LifecycleMaterializedEntry(
            RuntimeContentHandle handle,
            string source,
            string reason)
        {
            if (handle == null)
            {
                throw new ArgumentNullException(nameof(handle));
            }

            if (!handle.Identity.IsValid)
            {
                throw new ArgumentException("Lifecycle materialized entry handle identity must be valid.", nameof(handle));
            }

            if (!handle.IsMaterialized)
            {
                throw new ArgumentException("Lifecycle materialized entry requires a materialized runtime content handle.", nameof(handle));
            }

            Handle = handle;
            _state = LifecycleMaterializationEntryState.Active;
            _source = Normalize(source);
            _reason = Normalize(reason);
            _message = "Lifecycle-owned materialization entry registered.";
        }

        public RuntimeContentHandle Handle { get; }

        public RuntimeContentIdentity Identity => Handle.Identity;

        public RuntimeContentOwner Owner => Identity.Owner;

        public RuntimeContentScope Scope => Identity.Scope;

        public RuntimeContentId ContentId => Identity.ContentId;

        public LifecycleMaterializationEntryState State => _state;

        public string Source => _source ?? string.Empty;

        public string Reason => _reason ?? string.Empty;

        public string Message => _message ?? string.Empty;

        public bool IsActive => _state == LifecycleMaterializationEntryState.Active;

        public bool IsReleaseRequested => _state == LifecycleMaterializationEntryState.ReleaseRequested;

        public bool IsReleased => _state == LifecycleMaterializationEntryState.Released;

        public bool IsReleaseFailed => _state == LifecycleMaterializationEntryState.ReleaseFailed;

        internal LifecycleMaterializationRegistryOperationResult RequestRelease(string source, string reason)
        {
            if (_state == LifecycleMaterializationEntryState.ReleaseRequested)
            {
                return LifecycleMaterializationRegistryOperationResult.Success(
                    Identity,
                    LifecycleMaterializationRegistryOperationStatus.SucceededReleaseRequested,
                    this,
                    source,
                    reason,
                    "Lifecycle materialization release was already requested.");
            }

            if (_state != LifecycleMaterializationEntryState.Active && _state != LifecycleMaterializationEntryState.ReleaseFailed)
            {
                return LifecycleMaterializationRegistryOperationResult.Failure(
                    Identity,
                    LifecycleMaterializationRegistryOperationStatus.RejectedInvalidTransition,
                    this,
                    source,
                    reason,
                    $"Lifecycle materialization entry cannot request release from state '{_state}'.");
            }

            return ApplyState(
                LifecycleMaterializationEntryState.ReleaseRequested,
                LifecycleMaterializationRegistryOperationStatus.SucceededReleaseRequested,
                source,
                reason,
                "Lifecycle materialization release requested.");
        }

        internal LifecycleMaterializationRegistryOperationResult MarkReleased(string source, string reason)
        {
            if (_state == LifecycleMaterializationEntryState.Released)
            {
                return LifecycleMaterializationRegistryOperationResult.Success(
                    Identity,
                    LifecycleMaterializationRegistryOperationStatus.SucceededReleased,
                    this,
                    source,
                    reason,
                    "Lifecycle materialization entry was already released.");
            }

            if (_state != LifecycleMaterializationEntryState.ReleaseRequested)
            {
                return LifecycleMaterializationRegistryOperationResult.Failure(
                    Identity,
                    LifecycleMaterializationRegistryOperationStatus.RejectedInvalidTransition,
                    this,
                    source,
                    reason,
                    $"Lifecycle materialization entry cannot be marked released from state '{_state}'.");
            }

            return ApplyState(
                LifecycleMaterializationEntryState.Released,
                LifecycleMaterializationRegistryOperationStatus.SucceededReleased,
                source,
                reason,
                "Lifecycle materialization release completed.");
        }

        internal LifecycleMaterializationRegistryOperationResult MarkReleaseFailed(string source, string reason, string message)
        {
            if (_state != LifecycleMaterializationEntryState.ReleaseRequested)
            {
                return LifecycleMaterializationRegistryOperationResult.Failure(
                    Identity,
                    LifecycleMaterializationRegistryOperationStatus.RejectedInvalidTransition,
                    this,
                    source,
                    reason,
                    $"Lifecycle materialization entry cannot record release failure from state '{_state}'.");
            }

            return ApplyState(
                LifecycleMaterializationEntryState.ReleaseFailed,
                LifecycleMaterializationRegistryOperationStatus.SucceededReleaseFailedRecorded,
                source,
                reason,
                string.IsNullOrWhiteSpace(message)
                    ? "Lifecycle materialization release failed."
                    : message);
        }

        public string ToDiagnosticString()
        {
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            string messageText = Message.ToDiagnosticText();
            return $"identity='{Identity.StableText}' owner='{Owner.StableText}' scope='{Scope}' contentId='{ContentId.StableText}' state='{_state}' source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }

        private LifecycleMaterializationRegistryOperationResult ApplyState(
            LifecycleMaterializationEntryState state,
            LifecycleMaterializationRegistryOperationStatus status,
            string source,
            string reason,
            string message)
        {
            _state = state;
            _source = Normalize(source);
            _reason = Normalize(reason);
            _message = Normalize(message);

            return LifecycleMaterializationRegistryOperationResult.Success(
                Identity,
                status,
                this,
                source,
                reason,
                message);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
