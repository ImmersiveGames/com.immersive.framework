using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Experimental. Passive diagnostic issue produced while building an Activity Content Execution participant collection.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10E Activity Content Execution participant collection issue; no discovery runtime or Unity side effects.")]
    public readonly struct ActivityContentExecutionParticipantCollectionIssue : IEquatable<ActivityContentExecutionParticipantCollectionIssue>
    {
        public ActivityContentExecutionParticipantCollectionIssue(
            ActivityContentExecutionParticipantCollectionIssueKind kind,
            int index,
            string key,
            ActivityContentExecutionParticipantDescriptor descriptor,
            string message)
        {
            if (!Enum.IsDefined(typeof(ActivityContentExecutionParticipantCollectionIssueKind), kind)
                || kind == ActivityContentExecutionParticipantCollectionIssueKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Activity content execution participant collection issue kind must be explicit.");
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Activity content execution participant collection issue index cannot be negative.");
            }

            Kind = kind;
            Index = index;
            Key = key.NormalizeText();
            Descriptor = descriptor;
            Message = message.NormalizeText();
        }

        public ActivityContentExecutionParticipantCollectionIssueKind Kind { get; }

        private int Index { get; }

        private string Key { get; }

        private ActivityContentExecutionParticipantDescriptor Descriptor { get; }

        private string Message { get; }

        public bool Equals(ActivityContentExecutionParticipantCollectionIssue other)
        {
            return Kind == other.Kind
                && Index == other.Index
                && string.Equals(Key, other.Key, StringComparison.Ordinal)
                && Descriptor.Equals(other.Descriptor)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActivityContentExecutionParticipantCollectionIssue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Kind;
                hashCode = hashCode * 397 ^ Index;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Key ?? string.Empty);
                hashCode = hashCode * 397 ^ Descriptor.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            string keyText = Key.ToDiagnosticText();
            string messageText = Message.ToDiagnosticText();
            string descriptorText = Descriptor.IsValid ? Descriptor.ToDiagnosticString() : "<invalid>";
            return $"kind='{Kind}' index='{Index}' key='{keyText}' descriptor='{descriptorText}' message='{messageText}'";
        }
    }
}
