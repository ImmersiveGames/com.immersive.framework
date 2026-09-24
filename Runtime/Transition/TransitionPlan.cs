using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;
using Immersive.Framework.Common;

namespace Immersive.Framework.Transition
{
    /// <summary>
    /// API status: Experimental. Passive immutable plan for a transition operation.
    /// The plan records expected orchestration phases; it does not execute lifecycle, load scenes or play effects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F18B passive Transition plan primitive; no runtime execution or visual effects.")]
    public readonly struct TransitionPlan : IEquatable<TransitionPlan>
    {
        private readonly TransitionStep[] _steps;

        public TransitionPlan(
            TransitionOperationId operationId,
            TransitionKind kind,
            FrameworkIdentityKey owner,
            string source,
            string reason,
            IReadOnlyList<TransitionStep> steps)
        {
            if (!operationId.IsValid)
            {
                throw new ArgumentException("Transition plan requires a valid operation id.", nameof(operationId));
            }

            if (!Enum.IsDefined(typeof(TransitionKind), kind) || kind == TransitionKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Transition plan kind must be explicit.");
            }

            OperationId = operationId;
            Kind = kind;
            Owner = owner;
            Source = Normalize(source);
            Reason = Normalize(reason);
            _steps = CopySteps(steps);
        }

        public TransitionOperationId OperationId { get; }

        public TransitionKind Kind { get; }

        public FrameworkIdentityKey Owner { get; }

        public string Source { get; }

        public string Reason { get; }

        public IReadOnlyList<TransitionStep> Steps => _steps ?? Array.Empty<TransitionStep>();

        public int StepCount => Steps.Count;

        public bool HasSteps => StepCount > 0;

        public bool HasOwner => Owner.IsValid;

        public bool IsValid => OperationId.IsValid && Kind != TransitionKind.Unknown;

        public bool Equals(TransitionPlan other)
        {
            return OperationId.Equals(other.OperationId)
                && Kind == other.Kind
                && Owner.Equals(other.Owner)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && SequenceEquals(Steps, other.Steps);
        }

        public override bool Equals(object obj)
        {
            return obj is TransitionPlan other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = OperationId.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Kind;
                hashCode = hashCode * 397 ^ Owner.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);

                IReadOnlyList<TransitionStep> items = Steps;
                for (int i = 0; i < items.Count; i++)
                {
                    hashCode = hashCode * 397 ^ items[i].GetHashCode();
                }

                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            var builder = new StringBuilder();
            string ownerText = HasOwner ? Owner.StableText : "<none>";
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            builder.Append($"operation='{OperationId.StableText}' kind='{Kind}' owner='{ownerText}' source='{sourceText}' reason='{reasonText}' steps='{StepCount}'");

            if (HasSteps)
            {
                builder.Append(" plannedSteps=[");
                IReadOnlyList<TransitionStep> items = Steps;
                for (int i = 0; i < items.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append("; ");
                    }

                    builder.Append(items[i].ToDiagnosticString());
                }

                builder.Append(']');
            }

            return builder.ToString();
        }

        public static TransitionPlan Create(
            TransitionOperationId operationId,
            TransitionKind kind,
            string source,
            string reason,
            IReadOnlyList<TransitionStep> steps)
        {
            return new TransitionPlan(operationId, kind, default, source, reason, steps);
        }

        public static TransitionPlan CreateForOwner(
            TransitionOperationId operationId,
            TransitionKind kind,
            FrameworkIdentityKey owner,
            string source,
            string reason,
            IReadOnlyList<TransitionStep> steps)
        {
            if (!owner.IsValid)
            {
                throw new ArgumentException("Owner-scoped Transition plan requires a valid owner identity.", nameof(owner));
            }

            return new TransitionPlan(operationId, kind, owner, source, reason, steps);
        }

        public static bool operator ==(TransitionPlan left, TransitionPlan right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TransitionPlan left, TransitionPlan right)
        {
            return !left.Equals(right);
        }

        private static TransitionStep[] CopySteps(IReadOnlyList<TransitionStep> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<TransitionStep>();
            }

            var copy = new TransitionStep[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                if (!source[i].IsValid)
                {
                    throw new ArgumentException("Transition plan cannot contain invalid steps.", nameof(source));
                }

                copy[i] = source[i];
            }

            return copy;
        }

        private static bool SequenceEquals(IReadOnlyList<TransitionStep> left, IReadOnlyList<TransitionStep> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            for (int i = 0; i < left.Count; i++)
            {
                if (!left[i].Equals(right[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
