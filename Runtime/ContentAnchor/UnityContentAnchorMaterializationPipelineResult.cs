using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Immutable result for the explicit Unity RuntimeContent materialization + logical ContentAnchor binding + physical placement proof.
    /// It keeps physical Unity evidence adapter-side and does not make ContentAnchor core a materializer or release owner.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R-C Unity RuntimeContent/ContentAnchor materialization pipeline proof result; no camera/audio/gameplay/save consumer.")]
    public readonly struct UnityContentAnchorMaterializationPipelineResult : IEquatable<UnityContentAnchorMaterializationPipelineResult>
    {
        public UnityContentAnchorMaterializationPipelineResult(
            ContentAnchorBindingRequest request,
            UnityContentAnchorMaterializationPipelineStatus status,
            RuntimeMaterializationResult materializationResult,
            RuntimeMaterializationResult appliedMaterializationResult,
            ContentAnchorBindingResult bindingResult,
            UnityContentAnchorPlacementResult placementResult,
            RuntimeReleaseResult rollbackPhysicalReleaseResult,
            RuntimeReleaseResult rollbackLogicalReleaseResult,
            bool rollbackAttempted,
            bool rollbackSucceeded,
            string source,
            string reason,
            string message)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Content Anchor materialization pipeline result requires a valid request.", nameof(request));
            }

            if (!Enum.IsDefined(typeof(UnityContentAnchorMaterializationPipelineStatus), status)
                || status == UnityContentAnchorMaterializationPipelineStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Content Anchor materialization pipeline status must be explicit.");
            }

            if (status == UnityContentAnchorMaterializationPipelineStatus.Succeeded)
            {
                if (!materializationResult.Succeeded || !appliedMaterializationResult.Succeeded || !bindingResult.Succeeded || !placementResult.Succeeded)
                {
                    throw new ArgumentException("Successful Content Anchor materialization pipeline result requires successful materialization, applied materialization, binding and placement results.", nameof(status));
                }
            }

            Request = request;
            Status = status;
            MaterializationResult = materializationResult;
            AppliedMaterializationResult = appliedMaterializationResult;
            BindingResult = bindingResult;
            PlacementResult = placementResult;
            RollbackPhysicalReleaseResult = rollbackPhysicalReleaseResult;
            RollbackLogicalReleaseResult = rollbackLogicalReleaseResult;
            RollbackAttempted = rollbackAttempted;
            RollbackSucceeded = rollbackSucceeded;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public ContentAnchorBindingRequest Request { get; }

        public UnityContentAnchorMaterializationPipelineStatus Status { get; }

        public RuntimeMaterializationResult MaterializationResult { get; }

        public RuntimeMaterializationResult AppliedMaterializationResult { get; }

        public ContentAnchorBindingResult BindingResult { get; }

        public UnityContentAnchorPlacementResult PlacementResult { get; }

        public RuntimeReleaseResult RollbackPhysicalReleaseResult { get; }

        public RuntimeReleaseResult RollbackLogicalReleaseResult { get; }

        public bool RollbackAttempted { get; }

        public bool RollbackSucceeded { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool Succeeded => Status == UnityContentAnchorMaterializationPipelineStatus.Succeeded;

        public bool Failed => !Succeeded;

        public bool HasMaterializationResult => MaterializationResult.Request.IsValid;

        public bool HasAppliedMaterializationResult => AppliedMaterializationResult.Request.IsValid;

        public bool HasBindingResult => BindingResult.Request.IsValid;

        public bool HasPlacementResult => PlacementResult.BindingResult.Request.IsValid;

        public RuntimeContentIdentity RuntimeIdentity => Request.RuntimeIdentity;

        public bool PhysicalPlacementApplied => HasPlacementResult && PlacementResult.Succeeded;

        public bool Equals(UnityContentAnchorMaterializationPipelineResult other)
        {
            return Request.Equals(other.Request)
                && Status == other.Status
                && MaterializationResult.Equals(other.MaterializationResult)
                && AppliedMaterializationResult.Equals(other.AppliedMaterializationResult)
                && BindingResult.Equals(other.BindingResult)
                && PlacementResult.Equals(other.PlacementResult)
                && RollbackPhysicalReleaseResult.Equals(other.RollbackPhysicalReleaseResult)
                && RollbackLogicalReleaseResult.Equals(other.RollbackLogicalReleaseResult)
                && RollbackAttempted == other.RollbackAttempted
                && RollbackSucceeded == other.RollbackSucceeded
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is UnityContentAnchorMaterializationPipelineResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Request.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ MaterializationResult.GetHashCode();
                hashCode = hashCode * 397 ^ AppliedMaterializationResult.GetHashCode();
                hashCode = hashCode * 397 ^ BindingResult.GetHashCode();
                hashCode = hashCode * 397 ^ PlacementResult.GetHashCode();
                hashCode = hashCode * 397 ^ RollbackPhysicalReleaseResult.GetHashCode();
                hashCode = hashCode * 397 ^ RollbackLogicalReleaseResult.GetHashCode();
                hashCode = hashCode * 397 ^ RollbackAttempted.GetHashCode();
                hashCode = hashCode * 397 ^ RollbackSucceeded.GetHashCode();
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
            string materializationStatus = HasMaterializationResult ? MaterializationResult.Status.ToString() : "None";
            string appliedMaterializationStatus = HasAppliedMaterializationResult ? AppliedMaterializationResult.Status.ToString() : "None";
            string bindingStatus = HasBindingResult ? BindingResult.Status.ToString() : "None";
            string placementStatus = HasPlacementResult ? PlacementResult.Status.ToString() : "None";
            string rollbackPhysicalStatus = RollbackPhysicalReleaseResult.Request.IsValid ? RollbackPhysicalReleaseResult.Status.ToString() : "None";
            string rollbackLogicalStatus = RollbackLogicalReleaseResult.Request.IsValid ? RollbackLogicalReleaseResult.Status.ToString() : "None";
            return $"status='{Status}' succeeded='{Succeeded}' runtimeIdentity='{RuntimeIdentity.StableText}' materialization='{materializationStatus}' appliedMaterialization='{appliedMaterializationStatus}' binding='{bindingStatus}' placement='{placementStatus}' physicalPlacementApplied='{PhysicalPlacementApplied}' rollbackAttempted='{RollbackAttempted}' rollbackSucceeded='{RollbackSucceeded}' rollbackPhysical='{rollbackPhysicalStatus}' rollbackLogical='{rollbackLogicalStatus}' source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }

        public static UnityContentAnchorMaterializationPipelineResult Success(
            ContentAnchorBindingRequest request,
            RuntimeMaterializationResult materializationResult,
            RuntimeMaterializationResult appliedMaterializationResult,
            ContentAnchorBindingResult bindingResult,
            UnityContentAnchorPlacementResult placementResult,
            string source,
            string reason,
            string message)
        {
            return new UnityContentAnchorMaterializationPipelineResult(
                request,
                UnityContentAnchorMaterializationPipelineStatus.Succeeded,
                materializationResult,
                appliedMaterializationResult,
                bindingResult,
                placementResult,
                default(RuntimeReleaseResult),
                default(RuntimeReleaseResult),
                false,
                false,
                source,
                reason,
                string.IsNullOrWhiteSpace(message)
                    ? "Content Anchor materialization pipeline succeeded."
                    : message);
        }

        public static UnityContentAnchorMaterializationPipelineResult Failure(
            ContentAnchorBindingRequest request,
            UnityContentAnchorMaterializationPipelineStatus status,
            RuntimeMaterializationResult materializationResult,
            RuntimeMaterializationResult appliedMaterializationResult,
            ContentAnchorBindingResult bindingResult,
            UnityContentAnchorPlacementResult placementResult,
            RuntimeReleaseResult rollbackPhysicalReleaseResult,
            RuntimeReleaseResult rollbackLogicalReleaseResult,
            bool rollbackAttempted,
            bool rollbackSucceeded,
            string source,
            string reason,
            string message)
        {
            if (status == UnityContentAnchorMaterializationPipelineStatus.Succeeded)
            {
                throw new ArgumentException("Use Success for successful Content Anchor materialization pipeline results.", nameof(status));
            }

            return new UnityContentAnchorMaterializationPipelineResult(
                request,
                status,
                materializationResult,
                appliedMaterializationResult,
                bindingResult,
                placementResult,
                rollbackPhysicalReleaseResult,
                rollbackLogicalReleaseResult,
                rollbackAttempted,
                rollbackSucceeded,
                source,
                reason,
                string.IsNullOrWhiteSpace(message)
                    ? "Content Anchor materialization pipeline failed."
                    : message);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
