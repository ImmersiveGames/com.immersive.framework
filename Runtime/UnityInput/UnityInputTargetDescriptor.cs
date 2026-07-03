using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.UnityInput
{
    /// <summary>
    /// API status: Experimental. Passive description of one declared Unity Input integration target.
    /// It records evidence around official Unity Input components only; it does not enable input, switch action maps, bind gameplay commands or replace PlayerInput/PlayerInputManager.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F29A passive Unity Input target descriptor.")]
    public readonly struct UnityInputTargetDescriptor : IEquatable<UnityInputTargetDescriptor>
    {
        public UnityInputTargetDescriptor(
            UnityInputTargetId targetId,
            UnityInputTargetRole role,
            bool hasPlayerInputReference,
            string displayName,
            string sceneName,
            string objectName,
            string source,
            string reason)
            : this(
                targetId,
                role,
                hasPlayerInputReference,
                false,
                displayName,
                sceneName,
                objectName,
                source,
                reason)
        {
        }

        public UnityInputTargetDescriptor(
            UnityInputTargetId targetId,
            UnityInputTargetRole role,
            bool hasPlayerInputReference,
            bool requiresPlayerInputEvidence,
            string displayName,
            string sceneName,
            string objectName,
            string source,
            string reason)
        {
            if (!targetId.IsValid)
            {
                throw new ArgumentException("Unity Input target descriptor requires a valid target id.", nameof(targetId));
            }

            FrameworkEnumValidation.ThrowIfUndefinedOr(role, UnityInputTargetRole.Unknown, nameof(role), "Unity Input target descriptor role must be explicit.");

            TargetId = targetId;
            Role = role;
            HasPlayerInputReference = hasPlayerInputReference;
            RequiresPlayerInputEvidence = requiresPlayerInputEvidence;
            DisplayName = displayName.NormalizeTextOrFallback(targetId.StableText);
            SceneName = sceneName.NormalizeText();
            ObjectName = objectName.NormalizeText();
            Source = source.NormalizeTextOrFallback(nameof(UnityInputTargetDescriptor));
            Reason = reason.NormalizeText();
        }

        public UnityInputTargetId TargetId { get; }

        public UnityInputTargetRole Role { get; }

        public bool HasPlayerInputReference { get; }

        public bool RequiresPlayerInputEvidence { get; }

        public string DisplayName { get; }

        public string SceneName { get; }

        public string ObjectName { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool IsValid => TargetId.IsValid && FrameworkEnumValidation.IsDefinedAndNot(Role, UnityInputTargetRole.Unknown);

        public bool Equals(UnityInputTargetDescriptor other)
        {
            return TargetId.Equals(other.TargetId)
                && Role == other.Role
                && HasPlayerInputReference == other.HasPlayerInputReference
                && RequiresPlayerInputEvidence == other.RequiresPlayerInputEvidence
                && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
                && string.Equals(SceneName, other.SceneName, StringComparison.Ordinal)
                && string.Equals(ObjectName, other.ObjectName, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is UnityInputTargetDescriptor other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = TargetId.GetHashCode();
                hash = (hash * 397) ^ (int)Role;
                hash = (hash * 397) ^ HasPlayerInputReference.GetHashCode();
                hash = (hash * 397) ^ RequiresPlayerInputEvidence.GetHashCode();
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(DisplayName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(SceneName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(ObjectName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"{Role}:{TargetId.StableText}";
        }
    }
}
