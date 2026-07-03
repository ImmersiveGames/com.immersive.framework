using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.Common;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Intent-level declaration that Pause needs a content surface.
    /// It may reference the desired Content Anchor identity, but it does not bind, materialize, instantiate or own it.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F23E Pause content requirement; framework-only intent contract.")]
    public readonly struct PauseContentRequirement : IEquatable<PauseContentRequirement>
    {
        public PauseContentRequirement(
            PauseContentRequirementId requirementId,
            PauseContentRequirementPurpose purpose,
            PauseState pauseState,
            RuntimeContentScope runtimeScope,
            RuntimeContentOwner owner,
            ContentAnchorScope anchorScope,
            ContentAnchorKind anchorKind,
            ContentAnchorId anchorId,
            ContentAnchorRequiredness requiredness,
            string source,
            string reason)
        {
            if (!requirementId.IsValid)
            {
                throw new ArgumentException("Pause content requirement requires a valid requirement id.", nameof(requirementId));
            }

            if (!Enum.IsDefined(typeof(PauseContentRequirementPurpose), purpose) || purpose == PauseContentRequirementPurpose.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(purpose), purpose, "Pause content requirement purpose must be explicit.");
            }

            if (!Enum.IsDefined(typeof(PauseState), pauseState) || pauseState == PauseState.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(pauseState), pauseState, "Pause content requirement must carry an explicit Pause state.");
            }

            if (!Enum.IsDefined(typeof(RuntimeContentScope), runtimeScope) || runtimeScope == RuntimeContentScope.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(runtimeScope), runtimeScope, "Pause content requirement runtime scope must be explicit.");
            }

            if (!owner.IsValid)
            {
                throw new ArgumentException("Pause content requirement owner must be valid.", nameof(owner));
            }

            if (!Enum.IsDefined(typeof(ContentAnchorScope), anchorScope) || anchorScope == ContentAnchorScope.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(anchorScope), anchorScope, "Pause content requirement anchor scope must be explicit.");
            }

            if (!Enum.IsDefined(typeof(ContentAnchorKind), anchorKind) || anchorKind == ContentAnchorKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(anchorKind), anchorKind, "Pause content requirement anchor kind must be explicit.");
            }

            if (!anchorId.IsValid)
            {
                throw new ArgumentException("Pause content requirement anchor id must be valid.", nameof(anchorId));
            }

            if (!Enum.IsDefined(typeof(ContentAnchorRequiredness), requiredness))
            {
                throw new ArgumentOutOfRangeException(nameof(requiredness), requiredness, "Pause content requirement requiredness must be defined.");
            }

            RequirementId = requirementId;
            Purpose = purpose;
            PauseState = pauseState;
            RuntimeScope = runtimeScope;
            Owner = owner;
            AnchorScope = anchorScope;
            AnchorKind = anchorKind;
            AnchorId = anchorId;
            Requiredness = requiredness;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public PauseContentRequirementId RequirementId { get; }

        public PauseContentRequirementPurpose Purpose { get; }

        public PauseState PauseState { get; }

        public RuntimeContentScope RuntimeScope { get; }

        public RuntimeContentOwner Owner { get; }

        public ContentAnchorScope AnchorScope { get; }

        public ContentAnchorKind AnchorKind { get; }

        public ContentAnchorId AnchorId { get; }

        public ContentAnchorRequiredness Requiredness { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool IsValid => RequirementId.IsValid
            && Purpose != PauseContentRequirementPurpose.Unknown
            && PauseState != PauseState.Unknown
            && RuntimeScope != RuntimeContentScope.Unknown
            && Owner.IsValid
            && AnchorScope != ContentAnchorScope.Unknown
            && AnchorKind != ContentAnchorKind.Unknown
            && AnchorId.IsValid;

        public bool IsRequired => Requiredness == ContentAnchorRequiredness.Required;

        public bool IsOptional => Requiredness == ContentAnchorRequiredness.Optional;

        public bool HasSource => !string.IsNullOrWhiteSpace(Source);

        public bool HasReason => !string.IsNullOrWhiteSpace(Reason);

        public bool Equals(PauseContentRequirement other)
        {
            return RequirementId.Equals(other.RequirementId)
                && Purpose == other.Purpose
                && PauseState == other.PauseState
                && RuntimeScope == other.RuntimeScope
                && Owner.Equals(other.Owner)
                && AnchorScope == other.AnchorScope
                && AnchorKind == other.AnchorKind
                && AnchorId.Equals(other.AnchorId)
                && Requiredness == other.Requiredness
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PauseContentRequirement other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = RequirementId.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Purpose;
                hashCode = hashCode * 397 ^ (int)PauseState;
                hashCode = hashCode * 397 ^ (int)RuntimeScope;
                hashCode = hashCode * 397 ^ Owner.GetHashCode();
                hashCode = hashCode * 397 ^ (int)AnchorScope;
                hashCode = hashCode * 397 ^ (int)AnchorKind;
                hashCode = hashCode * 397 ^ AnchorId.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Requiredness;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            string sourceText = HasSource ? Source : "<none>";
            string reasonText = HasReason ? Reason : "<none>";
            return $"requirement='{RequirementId.StableText}' purpose='{Purpose}' pauseState='{PauseState}' runtimeScope='{RuntimeScope}' owner='{Owner.StableText}' anchorScope='{AnchorScope}' anchorKind='{AnchorKind}' anchor='{AnchorId.StableText}' requiredness='{Requiredness}' source='{sourceText}' reason='{reasonText}'";
        }

        public static PauseContentRequirement RequiredPresentationRoot(
            PauseContentRequirementId requirementId,
            PauseState pauseState,
            RuntimeContentScope runtimeScope,
            RuntimeContentOwner owner,
            ContentAnchorScope anchorScope,
            ContentAnchorId anchorId,
            string source,
            string reason)
        {
            return new PauseContentRequirement(
                requirementId,
                PauseContentRequirementPurpose.PresentationRoot,
                pauseState,
                runtimeScope,
                owner,
                anchorScope,
                ContentAnchorKind.Root,
                anchorId,
                ContentAnchorRequiredness.Required,
                source,
                reason);
        }

        public static PauseContentRequirement OptionalMenuRoot(
            PauseContentRequirementId requirementId,
            PauseState pauseState,
            RuntimeContentScope runtimeScope,
            RuntimeContentOwner owner,
            ContentAnchorScope anchorScope,
            ContentAnchorId anchorId,
            string source,
            string reason)
        {
            return new PauseContentRequirement(
                requirementId,
                PauseContentRequirementPurpose.MenuRoot,
                pauseState,
                runtimeScope,
                owner,
                anchorScope,
                ContentAnchorKind.Root,
                anchorId,
                ContentAnchorRequiredness.Optional,
                source,
                reason);
        }

        public static bool operator ==(PauseContentRequirement left, PauseContentRequirement right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PauseContentRequirement left, PauseContentRequirement right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
