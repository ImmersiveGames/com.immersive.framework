using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Transition
{
    /// <summary>
    /// API status: Experimental. Immutable diagnostics entry for one logical transition phase.
    /// A step observes or plans part of orchestration; it does not execute scene, UI, input or visual effects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F18B Transition step primitive; passive phase/status diagnostics.")]
    public readonly struct TransitionStep : IEquatable<TransitionStep>
    {
        public TransitionStep(
            int order,
            TransitionPhase phase,
            TransitionStatus status,
            string label,
            string message,
            bool blockingIssue)
        {
            if (order < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(order), order, "Transition step order cannot be negative.");
            }

            if (!Enum.IsDefined(typeof(TransitionPhase), phase) || phase == TransitionPhase.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(phase), phase, "Transition step phase must be explicit.");
            }

            if (!Enum.IsDefined(typeof(TransitionStatus), status) || status == TransitionStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Transition step status must be explicit.");
            }

            Order = order;
            Phase = phase;
            Status = status;
            Label = Normalize(label);
            Message = Normalize(message);
            BlockingIssue = blockingIssue;
        }

        public int Order { get; }

        public TransitionPhase Phase { get; }

        public TransitionStatus Status { get; }

        public string Label { get; }

        public string Message { get; }

        public bool BlockingIssue { get; }

        public bool IsValid => Phase != TransitionPhase.Unknown && Status != TransitionStatus.Unknown;

        public bool HasLabel => !string.IsNullOrWhiteSpace(Label);

        public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

        public bool IsFailure => Status == TransitionStatus.Failed || Status == TransitionStatus.Rejected || BlockingIssue;

        public bool Equals(TransitionStep other)
        {
            return Order == other.Order
                && Phase == other.Phase
                && Status == other.Status
                && string.Equals(Label, other.Label, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal)
                && BlockingIssue == other.BlockingIssue;
        }

        public override bool Equals(object obj)
        {
            return obj is TransitionStep other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Order;
                hashCode = hashCode * 397 ^ (int)Phase;
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Label ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                hashCode = hashCode * 397 ^ BlockingIssue.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            string labelText = HasLabel ? Label : "<none>";
            string messageText = HasMessage ? Message : "<none>";
            return $"order='{Order}' phase='{Phase}' status='{Status}' label='{labelText}' blockingIssue='{BlockingIssue}' message='{messageText}'";
        }

        public static TransitionStep Planned(int order, TransitionPhase phase, string label)
        {
            return new TransitionStep(order, phase, TransitionStatus.Planned, label, string.Empty, false);
        }

        public static TransitionStep Observed(int order, TransitionPhase phase, string label, string message)
        {
            return new TransitionStep(order, phase, TransitionStatus.Observed, label, message, false);
        }

        public static TransitionStep Succeeded(int order, TransitionPhase phase, string label, string message)
        {
            return new TransitionStep(order, phase, TransitionStatus.Succeeded, label, message, false);
        }

        public static TransitionStep Skipped(int order, TransitionPhase phase, string label, string message)
        {
            return new TransitionStep(order, phase, TransitionStatus.Skipped, label, message, false);
        }

        public static TransitionStep Failed(int order, TransitionPhase phase, string label, string message)
        {
            return new TransitionStep(order, phase, TransitionStatus.Failed, label, message, true);
        }

        public static bool operator ==(TransitionStep left, TransitionStep right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TransitionStep left, TransitionStep right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
