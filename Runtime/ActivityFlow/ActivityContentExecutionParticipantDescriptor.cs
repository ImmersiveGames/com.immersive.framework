using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Experimental. Passive descriptor for one Activity Content Execution participant.
    /// It defines identity, requiredness, phase support and diagnostic ordering only; it does not discover, execute or mutate Unity objects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10D Activity Content Execution participant descriptor; no discovery runtime or Unity side effects.")]
    public readonly struct ActivityContentExecutionParticipantDescriptor : IEquatable<ActivityContentExecutionParticipantDescriptor>
    {

        private ActivityContentExecutionParticipantDescriptor(
            RuntimeContentId contentId,
            ActivityContentExecutionRequiredness requiredness,
            bool supportsEnter,
            bool supportsExit,
            int order,
            string displayName,
            string source,
            string reason)
        {
            if (!contentId.IsValid)
            {
                throw new ArgumentException("Activity content execution participant descriptor requires a valid RuntimeContentId.", nameof(contentId));
            }

            if (!Enum.IsDefined(typeof(ActivityContentExecutionRequiredness), requiredness)
                || requiredness == ActivityContentExecutionRequiredness.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredness), requiredness, "Activity content execution participant requiredness must be explicit.");
            }

            if (!supportsEnter && !supportsExit)
            {
                throw new ArgumentException("Activity content execution participant must support Enter, Exit or both.", nameof(supportsEnter));
            }

            ContentId = contentId;
            Requiredness = requiredness;
            SupportsEnter = supportsEnter;
            SupportsExit = supportsExit;
            Order = order;
            DisplayName = displayName.NormalizeText();
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
        }

        public RuntimeContentId ContentId { get; }

        public ActivityContentExecutionRequiredness Requiredness { get; }

        public bool SupportsEnter { get; }

        public bool SupportsExit { get; }

        public int Order { get; }

        public string DisplayName { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool IsRequired => Requiredness == ActivityContentExecutionRequiredness.Required;

        public bool IsOptional => Requiredness == ActivityContentExecutionRequiredness.Optional;

        public bool IsValid => ContentId.IsValid
            && Requiredness != ActivityContentExecutionRequiredness.Unknown
            && (SupportsEnter || SupportsExit);

        public bool SupportsPhase(ActivityContentExecutionPhase phase)
        {
            return phase switch
            {
                ActivityContentExecutionPhase.Enter => SupportsEnter,
                ActivityContentExecutionPhase.Exit => SupportsExit,
                _ => false
            };
        }

        public bool Equals(ActivityContentExecutionParticipantDescriptor other)
        {
            return ContentId.Equals(other.ContentId)
                && Requiredness == other.Requiredness
                && SupportsEnter == other.SupportsEnter
                && SupportsExit == other.SupportsExit
                && Order == other.Order
                && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActivityContentExecutionParticipantDescriptor other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ContentId.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Requiredness;
                hashCode = hashCode * 397 ^ SupportsEnter.GetHashCode();
                hashCode = hashCode * 397 ^ SupportsExit.GetHashCode();
                hashCode = hashCode * 397 ^ Order;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(DisplayName ?? string.Empty);
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
            string displayNameText = DisplayName.ToDiagnosticText();
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            return $"contentId='{ContentId.StableText}' requiredness='{Requiredness}' supportsEnter='{SupportsEnter}' supportsExit='{SupportsExit}' order='{Order}' displayName='{displayNameText}' source='{sourceText}' reason='{reasonText}'";
        }

        public static ActivityContentExecutionParticipantDescriptor Required(
            RuntimeContentId contentId,
            bool supportsEnter,
            bool supportsExit,
            int order,
            string displayName,
            string source,
            string reason)
        {
            return new ActivityContentExecutionParticipantDescriptor(
                contentId,
                ActivityContentExecutionRequiredness.Required,
                supportsEnter,
                supportsExit,
                order,
                displayName,
                source,
                reason);
        }

        public static ActivityContentExecutionParticipantDescriptor Optional(
            RuntimeContentId contentId,
            bool supportsEnter,
            bool supportsExit,
            int order,
            string displayName,
            string source,
            string reason)
        {
            return new ActivityContentExecutionParticipantDescriptor(
                contentId,
                ActivityContentExecutionRequiredness.Optional,
                supportsEnter,
                supportsExit,
                order,
                displayName,
                source,
                reason);
        }

        public static bool operator ==(ActivityContentExecutionParticipantDescriptor left, ActivityContentExecutionParticipantDescriptor right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ActivityContentExecutionParticipantDescriptor left, ActivityContentExecutionParticipantDescriptor right)
        {
            return !left.Equals(right);
        }
    }
}
