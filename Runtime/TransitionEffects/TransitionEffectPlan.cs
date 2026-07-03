using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Transition;
using Immersive.Framework.Common;

namespace Immersive.Framework.TransitionEffects
{
    /// <summary>
    /// API status: Experimental. Passive collection of effect requests associated with one Transition operation.
    /// The plan does not discover adapters, execute visuals, load content or mutate Gate/Transition state.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F19B passive Transition Effect plan; no adapter execution or authoring policy.")]
    public readonly struct TransitionEffectPlan : IEquatable<TransitionEffectPlan>
    {
        private readonly TransitionEffectRequest[] _requests;

        public TransitionEffectPlan(
            TransitionOperationId operationId,
            TransitionKind transitionKind,
            string source,
            string reason,
            IReadOnlyList<TransitionEffectRequest> requests)
        {
            if (!operationId.IsValid)
            {
                throw new ArgumentException("Transition Effect plan requires a valid operation id.", nameof(operationId));
            }

            if (!Enum.IsDefined(typeof(TransitionKind), transitionKind) || transitionKind == TransitionKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(transitionKind), transitionKind, "Transition Effect plan transition kind must be explicit.");
            }

            OperationId = operationId;
            TransitionKind = transitionKind;
            Source = Normalize(source);
            Reason = Normalize(reason);
            _requests = CopyRequests(operationId, transitionKind, requests);
        }

        public TransitionOperationId OperationId { get; }

        public TransitionKind TransitionKind { get; }

        public string Source { get; }

        public string Reason { get; }

        public IReadOnlyList<TransitionEffectRequest> Requests => _requests ?? Array.Empty<TransitionEffectRequest>();

        public int RequestCount => Requests.Count;

        public bool HasRequests => RequestCount > 0;

        public int RequiredRequestCount => CountByRequiredness(TransitionEffectRequiredness.Required);

        public int OptionalRequestCount => CountByRequiredness(TransitionEffectRequiredness.Optional);

        public bool IsValid => OperationId.IsValid && TransitionKind != TransitionKind.Unknown;

        public int CountByKind(TransitionEffectKind kind)
        {
            if (kind == TransitionEffectKind.Unknown || !HasRequests)
            {
                return 0;
            }

            int count = 0;
            IReadOnlyList<TransitionEffectRequest> items = Requests;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].EffectKind == kind)
                {
                    count++;
                }
            }

            return count;
        }

        public int CountByRequiredness(TransitionEffectRequiredness requiredness)
        {
            if (requiredness == TransitionEffectRequiredness.Unknown || !HasRequests)
            {
                return 0;
            }

            int count = 0;
            IReadOnlyList<TransitionEffectRequest> items = Requests;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Requiredness == requiredness)
                {
                    count++;
                }
            }

            return count;
        }

        public bool Equals(TransitionEffectPlan other)
        {
            return OperationId.Equals(other.OperationId)
                && TransitionKind == other.TransitionKind
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && SequenceEquals(Requests, other.Requests);
        }

        public override bool Equals(object obj)
        {
            return obj is TransitionEffectPlan other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = OperationId.GetHashCode();
                hashCode = hashCode * 397 ^ (int)TransitionKind;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);

                IReadOnlyList<TransitionEffectRequest> items = Requests;
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
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            builder.Append($"operation='{OperationId.StableText}' transitionKind='{TransitionKind}' source='{sourceText}' reason='{reasonText}' requests='{RequestCount}' required='{RequiredRequestCount}' optional='{OptionalRequestCount}'");

            if (HasRequests)
            {
                builder.Append(" requests=[");
                IReadOnlyList<TransitionEffectRequest> items = Requests;
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

        public static TransitionEffectPlan Create(
            TransitionOperationId operationId,
            TransitionKind transitionKind,
            string source,
            string reason,
            IReadOnlyList<TransitionEffectRequest> requests)
        {
            return new TransitionEffectPlan(operationId, transitionKind, source, reason, requests);
        }

        public static bool operator ==(TransitionEffectPlan left, TransitionEffectPlan right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TransitionEffectPlan left, TransitionEffectPlan right)
        {
            return !left.Equals(right);
        }

        private static TransitionEffectRequest[] CopyRequests(
            TransitionOperationId operationId,
            TransitionKind transitionKind,
            IReadOnlyList<TransitionEffectRequest> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<TransitionEffectRequest>();
            }

            var copy = new TransitionEffectRequest[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                if (!source[i].IsValid)
                {
                    throw new ArgumentException("Transition Effect plan cannot contain invalid effect requests.", nameof(source));
                }

                if (source[i].OperationId != operationId)
                {
                    throw new ArgumentException("Transition Effect plan cannot contain requests for another Transition operation.", nameof(source));
                }

                if (source[i].TransitionKind != transitionKind)
                {
                    throw new ArgumentException("Transition Effect plan cannot contain requests for another Transition kind.", nameof(source));
                }

                copy[i] = source[i];
            }

            return copy;
        }

        private static bool SequenceEquals(IReadOnlyList<TransitionEffectRequest> left, IReadOnlyList<TransitionEffectRequest> right)
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
