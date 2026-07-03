using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Unity adapter-side result for physical placement of one materialized object under an explicit anchor Transform.
    /// This result is not a core ContentAnchor handle and must not be used as functional identity.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R-B Unity Content Anchor physical placement result proof; transform references remain adapter-side evidence.")]
    public readonly struct UnityContentAnchorPlacementResult : IEquatable<UnityContentAnchorPlacementResult>
    {
        public UnityContentAnchorPlacementResult(
            ContentAnchorBindingResult bindingResult,
            UnityContentAnchorPlacementStatus status,
            UnityRuntimeMaterializedObjectEvidence evidence,
            Transform anchorTransform,
            bool parentApplied,
            string source,
            string reason,
            string message)
        {
            if (!Enum.IsDefined(typeof(UnityContentAnchorPlacementStatus), status)
                || status == UnityContentAnchorPlacementStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Content Anchor placement status must be explicit.");
            }

            if (status is UnityContentAnchorPlacementStatus.Succeeded or UnityContentAnchorPlacementStatus.SucceededAlreadyPlaced)
            {
                if (!bindingResult.Succeeded || !bindingResult.HasHandle)
                {
                    throw new ArgumentException("Successful Content Anchor placement requires a successful logical binding result.", nameof(bindingResult));
                }

                if (evidence == null)
                {
                    throw new ArgumentNullException(nameof(evidence), "Successful Content Anchor placement requires physical materialization evidence.");
                }

                if (evidence.Identity != bindingResult.RuntimeIdentity)
                {
                    throw new ArgumentException("Successful Content Anchor placement evidence identity must match the logical binding runtime identity.", nameof(evidence));
                }

                if (anchorTransform == null)
                {
                    throw new ArgumentNullException(nameof(anchorTransform), "Successful Content Anchor placement requires an explicit anchor Transform.");
                }
            }

            BindingResult = bindingResult;
            Status = status;
            Evidence = evidence;
            AnchorTransform = anchorTransform;
            ParentApplied = parentApplied;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public ContentAnchorBindingResult BindingResult { get; }

        public UnityContentAnchorPlacementStatus Status { get; }

        public UnityRuntimeMaterializedObjectEvidence Evidence { get; }

        public Transform AnchorTransform { get; }

        public bool ParentApplied { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool Succeeded => Status is UnityContentAnchorPlacementStatus.Succeeded or UnityContentAnchorPlacementStatus.SucceededAlreadyPlaced;

        public bool Failed => !Succeeded;

        public bool HasEvidence => Evidence != null;

        public bool HasAnchorTransform => AnchorTransform != null;

        public RuntimeContentIdentity RuntimeIdentity => BindingResult.Request.IsValid
            ? BindingResult.RuntimeIdentity
            : Evidence?.Identity ?? default(RuntimeContentIdentity);

        public ContentAnchorDeclaration Anchor => BindingResult.Anchor;

        public bool Equals(UnityContentAnchorPlacementResult other)
        {
            return BindingResult.Equals(other.BindingResult)
                && Status == other.Status
                && ReferenceEquals(Evidence, other.Evidence)
                && ReferenceEquals(AnchorTransform, other.AnchorTransform)
                && ParentApplied == other.ParentApplied
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is UnityContentAnchorPlacementResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = BindingResult.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ (Evidence != null ? Evidence.GetHashCode() : 0);
                hashCode = hashCode * 397 ^ (AnchorTransform != null ? AnchorTransform.GetHashCode() : 0);
                hashCode = hashCode * 397 ^ ParentApplied.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
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
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            string messageText = Message.ToDiagnosticText();
            string anchorText = BindingResult.HasAnchor ? BindingResult.Anchor.ToDiagnosticString() : "<none>";
            string instanceName = Evidence?.Instance.ToDiagnosticText(value => value.name) ?? "<none>";
            string anchorName = AnchorTransform.ToDiagnosticText(value => value.name);
            string runtimeIdentityText = RuntimeIdentity.IsValid ? RuntimeIdentity.StableText : "<none>";
            return $"status='{Status}' succeeded='{Succeeded}' parentApplied='{ParentApplied}' runtimeIdentity='{runtimeIdentityText}' anchor={anchorText} hasEvidence='{HasEvidence}' hasAnchorTransform='{HasAnchorTransform}' instanceName='{instanceName}' anchorTransformName='{anchorName}' source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }

        public static UnityContentAnchorPlacementResult Success(
            ContentAnchorBindingResult bindingResult,
            UnityRuntimeMaterializedObjectEvidence evidence,
            Transform anchorTransform,
            bool parentApplied,
            string source,
            string reason,
            string message)
        {
            return new UnityContentAnchorPlacementResult(
                bindingResult,
                UnityContentAnchorPlacementStatus.Succeeded,
                evidence,
                anchorTransform,
                parentApplied,
                source,
                reason,
                string.IsNullOrWhiteSpace(message)
                    ? "Content Anchor physical placement succeeded."
                    : message);
        }

        public static UnityContentAnchorPlacementResult AlreadyPlaced(
            ContentAnchorBindingResult bindingResult,
            UnityRuntimeMaterializedObjectEvidence evidence,
            Transform anchorTransform,
            string source,
            string reason,
            string message)
        {
            return new UnityContentAnchorPlacementResult(
                bindingResult,
                UnityContentAnchorPlacementStatus.SucceededAlreadyPlaced,
                evidence,
                anchorTransform,
                false,
                source,
                reason,
                string.IsNullOrWhiteSpace(message)
                    ? "Content Anchor physical placement already existed."
                    : message);
        }

        public static UnityContentAnchorPlacementResult Failure(
            ContentAnchorBindingResult bindingResult,
            UnityContentAnchorPlacementStatus status,
            UnityRuntimeMaterializedObjectEvidence evidence,
            Transform anchorTransform,
            string source,
            string reason,
            string message)
        {
            if (status is UnityContentAnchorPlacementStatus.Succeeded or UnityContentAnchorPlacementStatus.SucceededAlreadyPlaced)
            {
                throw new ArgumentException("Use Success or AlreadyPlaced for successful Content Anchor placement results.", nameof(status));
            }

            return new UnityContentAnchorPlacementResult(
                bindingResult,
                status,
                evidence,
                anchorTransform,
                false,
                source,
                reason,
                string.IsNullOrWhiteSpace(message)
                    ? "Content Anchor physical placement failed."
                    : message);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
