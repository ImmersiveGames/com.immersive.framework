using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Aggregate result for the reusable ContentAnchor materialization service.
    /// It preserves original subsystem results and reports the stage that failed instead of replacing every status vocabulary.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "MAT-4 aggregate ContentAnchor materialization service result with failed stage and subsystem results.")]
    internal readonly struct ContentAnchorMaterializationResult : IEquatable<ContentAnchorMaterializationResult>
    {
        public ContentAnchorMaterializationResult(
            ContentAnchorBindingRequest request,
            RuntimeMaterializationRequest materializationRequest,
            ContentAnchorMaterializationStage failedStage,
            RuntimeMaterializationResult materializationResult,
            RuntimeMaterializationResult appliedMaterializationResult,
            ContentAnchorBindingResult bindingResult,
            UnityContentAnchorPlacementResult placementResult,
            ContentAnchorMaterializationRollbackResult rollbackResult,
            string source,
            string reason,
            string message)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Content Anchor materialization result requires a valid binding request.", nameof(request));
            }

            if (!Enum.IsDefined(typeof(ContentAnchorMaterializationStage), failedStage))
            {
                throw new ArgumentOutOfRangeException(nameof(failedStage), failedStage, "Content Anchor materialization failed stage must be defined.");
            }

            if (failedStage == ContentAnchorMaterializationStage.None)
            {
                if (!materializationRequest.IsValid
                    || !materializationResult.Succeeded
                    || !appliedMaterializationResult.Succeeded
                    || !bindingResult.Succeeded
                    || !placementResult.Succeeded)
                {
                    throw new ArgumentException("Successful Content Anchor materialization result requires successful materialization, applied materialization, binding and placement results.", nameof(failedStage));
                }
            }

            Request = request;
            MaterializationRequest = materializationRequest;
            FailedStage = failedStage;
            MaterializationResult = materializationResult;
            AppliedMaterializationResult = appliedMaterializationResult;
            BindingResult = bindingResult;
            PlacementResult = placementResult;
            RollbackResult = rollbackResult;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
        }

        public ContentAnchorBindingRequest Request { get; }

        public RuntimeMaterializationRequest MaterializationRequest { get; }

        public ContentAnchorMaterializationStage FailedStage { get; }

        public RuntimeMaterializationResult MaterializationResult { get; }

        public RuntimeMaterializationResult AppliedMaterializationResult { get; }

        public ContentAnchorBindingResult BindingResult { get; }

        public UnityContentAnchorPlacementResult PlacementResult { get; }

        public ContentAnchorMaterializationRollbackResult RollbackResult { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool Succeeded => FailedStage == ContentAnchorMaterializationStage.None;

        public bool Failed => !Succeeded;

        public bool HasMaterializationRequest => MaterializationRequest.IsValid;

        public bool HasMaterializationResult => MaterializationResult.Request.IsValid;

        public bool HasAppliedMaterializationResult => AppliedMaterializationResult.Request.IsValid;

        public bool HasBindingResult => BindingResult.Request.IsValid;

        public bool HasPlacementResult => PlacementResult.BindingResult.Request.IsValid;

        public RuntimeContentIdentity RuntimeIdentity => Request.RuntimeIdentity;

        public bool PhysicalPlacementApplied => HasPlacementResult && PlacementResult.Succeeded;

        public bool Equals(ContentAnchorMaterializationResult other)
        {
            return Request.Equals(other.Request)
                && MaterializationRequest.Equals(other.MaterializationRequest)
                && FailedStage == other.FailedStage
                && MaterializationResult.Equals(other.MaterializationResult)
                && AppliedMaterializationResult.Equals(other.AppliedMaterializationResult)
                && BindingResult.Equals(other.BindingResult)
                && PlacementResult.Equals(other.PlacementResult)
                && RollbackResult.Equals(other.RollbackResult)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ContentAnchorMaterializationResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Request.GetHashCode();
                hashCode = hashCode * 397 ^ MaterializationRequest.GetHashCode();
                hashCode = hashCode * 397 ^ (int)FailedStage;
                hashCode = hashCode * 397 ^ MaterializationResult.GetHashCode();
                hashCode = hashCode * 397 ^ AppliedMaterializationResult.GetHashCode();
                hashCode = hashCode * 397 ^ BindingResult.GetHashCode();
                hashCode = hashCode * 397 ^ PlacementResult.GetHashCode();
                hashCode = hashCode * 397 ^ RollbackResult.GetHashCode();
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
            return $"succeeded='{Succeeded}' failedStage='{FailedStage}' runtimeIdentity='{RuntimeIdentity.StableText}' materialization='{materializationStatus}' appliedMaterialization='{appliedMaterializationStatus}' binding='{bindingStatus}' placement='{placementStatus}' physicalPlacementApplied='{PhysicalPlacementApplied}' rollback={RollbackResult.ToDiagnosticString()} source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }

        public static ContentAnchorMaterializationResult Success(
            ContentAnchorBindingRequest request,
            RuntimeMaterializationRequest materializationRequest,
            RuntimeMaterializationResult materializationResult,
            RuntimeMaterializationResult appliedMaterializationResult,
            ContentAnchorBindingResult bindingResult,
            UnityContentAnchorPlacementResult placementResult,
            string source,
            string reason,
            string message)
        {
            return new ContentAnchorMaterializationResult(
                request,
                materializationRequest,
                ContentAnchorMaterializationStage.None,
                materializationResult,
                appliedMaterializationResult,
                bindingResult,
                placementResult,
                ContentAnchorMaterializationRollbackResult.NotAttempted(source, reason, "Content Anchor materialization succeeded; rollback was not needed."),
                source,
                reason,
                string.IsNullOrWhiteSpace(message)
                    ? "Content Anchor materialization service succeeded."
                    : message);
        }

        public static ContentAnchorMaterializationResult Failure(
            ContentAnchorBindingRequest request,
            RuntimeMaterializationRequest materializationRequest,
            ContentAnchorMaterializationStage failedStage,
            RuntimeMaterializationResult materializationResult,
            RuntimeMaterializationResult appliedMaterializationResult,
            ContentAnchorBindingResult bindingResult,
            UnityContentAnchorPlacementResult placementResult,
            ContentAnchorMaterializationRollbackResult rollbackResult,
            string source,
            string reason,
            string message)
        {
            if (failedStage == ContentAnchorMaterializationStage.None)
            {
                throw new ArgumentException("Failed Content Anchor materialization result requires a failed stage.", nameof(failedStage));
            }

            return new ContentAnchorMaterializationResult(
                request,
                materializationRequest,
                failedStage,
                materializationResult,
                appliedMaterializationResult,
                bindingResult,
                placementResult,
                rollbackResult,
                source,
                reason,
                string.IsNullOrWhiteSpace(message)
                    ? "Content Anchor materialization service failed."
                    : message);
        }
    }
}
